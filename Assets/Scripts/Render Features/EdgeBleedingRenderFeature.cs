using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// Edge Bleeding Render Feature - Creates a subtle color bleeding effect at object edges
/// for a watercolor-style appearance.
/// </summary>
public class EdgeBleedingRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material bleedingMaterial;
        
        [Header("Bleeding Settings")]
        [Range(0f, 1f)]
        public float bleedingIntensity = 0.3f;
        
        [Range(1, 8)]
        public int blurIterations = 2;
        
        [Range(0.5f, 4f)]
        public float blurSpread = 1.5f;
        
        [Header("Edge Detection")]
        public bool useEdgeDetection = true;
        
        [Range(0f, 1f)]
        public float depthThreshold = 0.1f;
        
        [Range(0f, 1f)]
        public float normalThreshold = 0.3f;
    }

    public Settings settings = new Settings();
    private EdgeBleedingPass m_RenderPass;

    public override void Create()
    {
        m_RenderPass = new EdgeBleedingPass(settings);
        m_RenderPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.bleedingMaterial == null)
            return;

        if (renderingData.cameraData.isPreviewCamera)
            return;

        m_RenderPass.Setup(settings);
        renderer.EnqueuePass(m_RenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_RenderPass?.Dispose();
    }

    class EdgeBleedingPass : ScriptableRenderPass
    {
        private Settings m_Settings;
        private RTHandle m_TempTexture;
        private RTHandle m_BlurTexture1;
        private RTHandle m_BlurTexture2;
        
        private static readonly int s_BleedingIntensity = Shader.PropertyToID("_BleedingIntensity");
        private static readonly int s_BlurSpread = Shader.PropertyToID("_BlurSpread");
        private static readonly int s_DepthThreshold = Shader.PropertyToID("_DepthThreshold");
        private static readonly int s_NormalThreshold = Shader.PropertyToID("_NormalThreshold");
        private static readonly int s_BlurDirection = Shader.PropertyToID("_BlurDirection");
        private static readonly int s_UseEdgeDetection = Shader.PropertyToID("_UseEdgeDetection");

        public EdgeBleedingPass(Settings settings)
        {
            m_Settings = settings;
            profilingSampler = new ProfilingSampler("Edge Bleeding");
            requiresIntermediateTexture = true;
        }

        public void Setup(Settings settings)
        {
            m_Settings = settings;
        }

        // Legacy path - called when Render Graph is disabled
        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TempTexture, desc, FilterMode.Bilinear, 
                TextureWrapMode.Clamp, name: "_EdgeBleedingTemp");
            
            desc.width = Mathf.Max(1, desc.width / 2);
            desc.height = Mathf.Max(1, desc.height / 2);
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_BlurTexture1, desc, FilterMode.Bilinear, 
                TextureWrapMode.Clamp, name: "_EdgeBleedingBlur1");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_BlurTexture2, desc, FilterMode.Bilinear, 
                TextureWrapMode.Clamp, name: "_EdgeBleedingBlur2");
        }

        // Legacy path - called when Render Graph is disabled
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Settings.bleedingMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                #pragma warning disable CS0618
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                #pragma warning restore CS0618
                var mat = m_Settings.bleedingMaterial;
                
                SetupMaterialProperties(mat);
                
                // Pass 0: Initial Horizontal Blur
                mat.SetVector(s_BlurDirection, new Vector4(1, 0, 0, 0));
                Blitter.BlitCameraTexture(cmd, source, m_BlurTexture1, mat, 0);
                
                // Blur Iterations
                for (int i = 0; i < m_Settings.blurIterations; i++)
                {
                    // Vertical Blur
                    mat.SetVector(s_BlurDirection, new Vector4(0, 1, 0, 0));
                    Blitter.BlitCameraTexture(cmd, m_BlurTexture1, m_BlurTexture2, mat, 0);
                    
                    // Horizontal Blur
                    mat.SetVector(s_BlurDirection, new Vector4(1, 0, 0, 0));
                    Blitter.BlitCameraTexture(cmd, m_BlurTexture2, m_BlurTexture1, mat, 0);
                }
                
                // Pass 1: Edge-aware Composite
                cmd.SetGlobalTexture("_BlurredTex", m_BlurTexture1);
                Blitter.BlitCameraTexture(cmd, source, m_TempTexture, mat, 1);
                Blitter.BlitCameraTexture(cmd, m_TempTexture, source);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void SetupMaterialProperties(Material mat)
        {
            mat.SetFloat(s_BleedingIntensity, m_Settings.bleedingIntensity);
            mat.SetFloat(s_BlurSpread, m_Settings.blurSpread);
            mat.SetFloat(s_DepthThreshold, m_Settings.depthThreshold);
            mat.SetFloat(s_NormalThreshold, m_Settings.normalThreshold);
            mat.SetFloat(s_UseEdgeDetection, m_Settings.useEdgeDetection ? 1f : 0f);
        }

        // Render Graph path - called when Render Graph is enabled
        private class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle blurTex1;
            public TextureHandle blurTex2;
            public TextureHandle temp;
            public int blurIterations;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Settings.bleedingMaterial == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            
            if (resourceData.isActiveTargetBackBuffer)
                return;

            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            
            var blurDesc = desc;
            blurDesc.width = Mathf.Max(1, blurDesc.width / 2);
            blurDesc.height = Mathf.Max(1, blurDesc.height / 2);
            
            TextureHandle blurTex1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurDesc, "_EdgeBleedingBlur1", false);
            TextureHandle blurTex2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurDesc, "_EdgeBleedingBlur2", false);
            TextureHandle tempTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_EdgeBleedingTemp", false);
            
            var mat = m_Settings.bleedingMaterial;
            SetupMaterialProperties(mat);

            TextureHandle source = resourceData.activeColorTexture;
            
            using (var builder = renderGraph.AddUnsafePass<PassData>("Edge Bleeding", out var passData))
            {
                passData.material = mat;
                passData.source = source;
                passData.blurTex1 = blurTex1;
                passData.blurTex2 = blurTex2;
                passData.temp = tempTex;
                passData.blurIterations = m_Settings.blurIterations;
                
                builder.UseTexture(source, AccessFlags.ReadWrite);
                builder.UseTexture(blurTex1, AccessFlags.ReadWrite);
                builder.UseTexture(blurTex2, AccessFlags.ReadWrite);
                builder.UseTexture(tempTex, AccessFlags.ReadWrite);
                
                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    
                    data.material.SetVector(s_BlurDirection, new Vector4(1, 0, 0, 0));
                    Blitter.BlitCameraTexture(cmd, data.source, data.blurTex1, data.material, 0);
                    
                    for (int i = 0; i < data.blurIterations; i++)
                    {
                        data.material.SetVector(s_BlurDirection, new Vector4(0, 1, 0, 0));
                        Blitter.BlitCameraTexture(cmd, data.blurTex1, data.blurTex2, data.material, 0);
                        
                        data.material.SetVector(s_BlurDirection, new Vector4(1, 0, 0, 0));
                        Blitter.BlitCameraTexture(cmd, data.blurTex2, data.blurTex1, data.material, 0);
                    }
                    
                    cmd.SetGlobalTexture("_BlurredTex", data.blurTex1);
                    Blitter.BlitCameraTexture(cmd, data.source, data.temp, data.material, 1);
                    Blitter.BlitCameraTexture(cmd, data.temp, data.source);
                });
            }
        }

        public void Dispose()
        {
            m_TempTexture?.Release();
            m_BlurTexture1?.Release();
            m_BlurTexture2?.Release();
        }
    }
}
