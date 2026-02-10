using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KuwaharaTestRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material;
        
        [Header("Single Kuwahara")]
        [Range(2, 20)]
        public int kernelSize1 = 3;
        [Range(0.1f, 5.0f)]
        public float tensorSpread1 = 1.0f;
        [Range(0f, 2f)]
        public float weightH1 = 1.0f;
        [Range(0f, 2f)]
        public float weightV1 = 1.0f;
        [Range(0f, 2f)]
        public float anisotropy1 = 1.0f;
        [Range(1f, 20f)]
        public float sharpness1 = 12.0f;
        
        public enum DebugPass
        {
            None,
            SourceCopy,
            Tensor1,
            Temp1
        }
        public DebugPass debugPass = DebugPass.None;
    }

    public Settings settings = new Settings();
    
    private RTHandle m_SourceCopy;
    private RTHandle m_Tensor1;
    private RTHandle m_Temp1;
    
    class KuwaharaPass : ScriptableRenderPass
    {
        private Settings m_Settings;
        private Material m_Mat1;
        private RTHandle m_SourceCopy;
        private RTHandle m_Tensor1;
        private RTHandle m_Temp1;

        public void Setup(Settings settings, Material mat1, RTHandle sourceCopy, RTHandle tensor1, RTHandle temp1)
        {
            m_Settings = settings;
            m_Mat1 = mat1;
            m_SourceCopy = sourceCopy;
            m_Tensor1 = tensor1;
            m_Temp1 = temp1;
        }

#pragma warning disable CS0672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
#pragma warning restore CS0672

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Mat1 == null || m_Settings == null) return;

            var cmd = CommandBufferPool.Get("Kuwahara");
            var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Safety: URP can run passes for cameras that don't have a valid color target (or RTHandles may be null if allocation failed).
            if (cameraColorTarget == null || cameraColorTarget.rt == null ||
                m_SourceCopy == null || m_Tensor1 == null || m_Temp1 == null)
            {
                CommandBufferPool.Release(cmd);
                return;
            }
            
            // Copy camera color to source copy
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, m_SourceCopy);

            // Single Kuwahara settings
            m_Mat1.SetFloat("_KuwaharaSize", m_Settings.kernelSize1);
            m_Mat1.SetFloat("_TensorSpread", m_Settings.tensorSpread1);
            m_Mat1.SetFloat("_WeightH", m_Settings.weightH1);
            m_Mat1.SetFloat("_WeightV", m_Settings.weightV1);
            m_Mat1.SetFloat("_Anisotropy", m_Settings.anisotropy1);
            m_Mat1.SetFloat("_Sharpness", m_Settings.sharpness1);

            // Structure tensor + blur
            Blitter.BlitCameraTexture(cmd, m_SourceCopy, m_Tensor1, m_Mat1, 0);
            Blitter.BlitCameraTexture(cmd, m_Tensor1, m_Temp1, m_Mat1, 1);
            Blitter.BlitCameraTexture(cmd, m_Temp1, m_Tensor1, m_Mat1, 2);
            
            // Single Kuwahara
            cmd.SetGlobalTexture("_StructureTensorTex", m_Tensor1);

            if (m_Settings.debugPass == Settings.DebugPass.None)
            {
                Blitter.BlitCameraTexture(cmd, m_SourceCopy, cameraColorTarget, m_Mat1, 3);
            }
            else
            {
                RTHandle debugTarget = null;
                switch (m_Settings.debugPass)
                {
                    case Settings.DebugPass.SourceCopy: debugTarget = m_SourceCopy; break;
                    case Settings.DebugPass.Tensor1: debugTarget = m_Tensor1; break;
                    case Settings.DebugPass.Temp1: debugTarget = m_Temp1; break;
                }
                
                if (debugTarget != null)
                {
                    Blitter.BlitCameraTexture(cmd, debugTarget, cameraColorTarget);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private KuwaharaPass m_Pass;
    private Material m_Mat1;

    public override void Create()
    {
        m_Pass = new KuwaharaPass();
        m_Pass.renderPassEvent = settings.renderPassEvent;
        
        if (settings.material != null)
        {
            m_Mat1 = new Material(settings.material);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
        {
            return;
        }

        if (m_Mat1 == null || m_Mat1.shader != settings.material.shader)
        {
            if (m_Mat1 != null) DestroyImmediate(m_Mat1);
            m_Mat1 = new Material(settings.material);
        }
        
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        
        // Enforce Clamp to prevent edge bleeding artifacts
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_SourceCopy, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SourceCopy");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Tensor1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Tensor1");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Temp1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Temp1");
        
        m_Pass.Setup(settings, m_Mat1, m_SourceCopy, m_Tensor1, m_Temp1);
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        m_SourceCopy?.Release();
        m_Tensor1?.Release();
        m_Temp1?.Release();
        
        if (m_Mat1 != null) DestroyImmediate(m_Mat1);
    }
}
