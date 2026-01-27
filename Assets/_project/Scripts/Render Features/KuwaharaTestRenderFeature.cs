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
        
        [Header("Layer 1 (Fine Details)")]
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
        
        [Header("Layer 2 (Coarse Shapes)")]
        [Range(2, 20)]
        public int kernelSize2 = 8;
        [Range(0.1f, 5.0f)]
        public float tensorSpread2 = 2.0f;
        [Range(0f, 2f)]
        public float weightH2 = 1.0f;
        [Range(0f, 2f)]
        public float weightV2 = 1.0f;
        [Range(0f, 2f)]
        public float anisotropy2 = 1.0f;
        [Range(1f, 20f)]
        public float sharpness2 = 8.0f;
        
        [Header("Blending")]
        [Range(0f, 1f)]
        public float blendFactor = 0.5f;
        public Vector2 layer2Offset = Vector2.zero; // Offset in pixels or UV units
        
        public enum DebugPass
        {
            None,
            SourceCopy,
            Tensor1,
            Temp1,
            Layer1,
            Tensor2,
            Temp2,
            Layer2
        }
        public DebugPass debugPass = DebugPass.None;
    }

    public Settings settings = new Settings();
    
    // Separate RTHandles for each layer
    private RTHandle m_SourceCopy;
    private RTHandle m_Tensor1;
    private RTHandle m_Temp1;
    private RTHandle m_Tensor2;
    private RTHandle m_Temp2;
    private RTHandle m_Layer1;
    private RTHandle m_Layer2;
    
    class KuwaharaPass : ScriptableRenderPass
    {
        private Settings m_Settings;
        private Material m_Mat1;
        private Material m_Mat2;
        private RTHandle m_SourceCopy;
        private RTHandle m_Tensor1;
        private RTHandle m_Temp1;
        private RTHandle m_Tensor2;
        private RTHandle m_Temp2;
        private RTHandle m_Layer1;
        private RTHandle m_Layer2;

        public void Setup(Settings settings, Material mat1, Material mat2,
            RTHandle sourceCopy, RTHandle tensor1, RTHandle temp1, RTHandle tensor2, RTHandle temp2, RTHandle layer1, RTHandle layer2)
        {
            m_Settings = settings;
            m_Mat1 = mat1;
            m_Mat2 = mat2;
            m_SourceCopy = sourceCopy;
            m_Tensor1 = tensor1;
            m_Temp1 = temp1;
            m_Tensor2 = tensor2;
            m_Temp2 = temp2;
            m_Layer1 = layer1;
            m_Layer2 = layer2;
        }

#pragma warning disable CS0672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
#pragma warning restore CS0672

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Mat1 == null || m_Mat2 == null || m_Settings == null) return;

            var cmd = CommandBufferPool.Get("Kuwahara");
            var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            
            // Copy camera color to source copy
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, m_SourceCopy);

            // --- Layer 1 (uses Tensor1, Temp1) ---
            m_Mat1.SetFloat("_KuwaharaSize", m_Settings.kernelSize1);
            m_Mat1.SetFloat("_TensorSpread", m_Settings.tensorSpread1);
            m_Mat1.SetFloat("_WeightH", m_Settings.weightH1);
            m_Mat1.SetFloat("_WeightV", m_Settings.weightV1);
            m_Mat1.SetFloat("_Anisotropy", m_Settings.anisotropy1);
            m_Mat1.SetFloat("_Sharpness", m_Settings.sharpness1);

            // Structure Tensor for Layer 1
            Blitter.BlitCameraTexture(cmd, m_SourceCopy, m_Tensor1, m_Mat1, 0);
            Blitter.BlitCameraTexture(cmd, m_Tensor1, m_Temp1, m_Mat1, 1);
            Blitter.BlitCameraTexture(cmd, m_Temp1, m_Tensor1, m_Mat1, 2);
            
            // Kuwahara for Layer 1 (bind tensor globally)
            cmd.SetGlobalTexture("_StructureTensorTex", m_Tensor1);
            Blitter.BlitCameraTexture(cmd, m_SourceCopy, m_Layer1, m_Mat1, 3);

            // --- Layer 2 (uses Tensor2, Temp2) ---
            m_Mat2.SetFloat("_KuwaharaSize", m_Settings.kernelSize2);
            m_Mat2.SetFloat("_TensorSpread", m_Settings.tensorSpread2);
            m_Mat2.SetFloat("_WeightH", m_Settings.weightH2);
            m_Mat2.SetFloat("_WeightV", m_Settings.weightV2);
            m_Mat2.SetFloat("_Anisotropy", m_Settings.anisotropy2);
            m_Mat2.SetFloat("_Sharpness", m_Settings.sharpness2);

            // Structure Tensor for Layer 2
            Blitter.BlitCameraTexture(cmd, m_SourceCopy, m_Tensor2, m_Mat2, 0);
            Blitter.BlitCameraTexture(cmd, m_Tensor2, m_Temp2, m_Mat2, 1);
            Blitter.BlitCameraTexture(cmd, m_Temp2, m_Tensor2, m_Mat2, 2);
            
            // Kuwahara for Layer 2 (bind tensor globally)
            cmd.SetGlobalTexture("_StructureTensorTex", m_Tensor2);
            Blitter.BlitCameraTexture(cmd, m_SourceCopy, m_Layer2, m_Mat2, 3);

            // --- Debug or Blend ---
            if (m_Settings.debugPass == Settings.DebugPass.None)
            {
                m_Mat1.SetFloat("_BlendFactor", m_Settings.blendFactor);
                m_Mat1.SetVector("_Layer2Offset", m_Settings.layer2Offset);
                cmd.SetGlobalTexture("_BlendTex", m_Layer2); // Use Global Texture for Blend
                Blitter.BlitCameraTexture(cmd, m_Layer1, cameraColorTarget, m_Mat1, 4);
            }
            else
            {
                RTHandle debugTarget = null;
                switch (m_Settings.debugPass)
                {
                    case Settings.DebugPass.SourceCopy: debugTarget = m_SourceCopy; break;
                    case Settings.DebugPass.Tensor1: debugTarget = m_Tensor1; break;
                    case Settings.DebugPass.Temp1: debugTarget = m_Temp1; break;
                    case Settings.DebugPass.Layer1: debugTarget = m_Layer1; break;
                    case Settings.DebugPass.Tensor2: debugTarget = m_Tensor2; break;
                    case Settings.DebugPass.Temp2: debugTarget = m_Temp2; break;
                    case Settings.DebugPass.Layer2: debugTarget = m_Layer2; break;
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
    private Material m_Mat2;

    public override void Create()
    {
        m_Pass = new KuwaharaPass();
        m_Pass.renderPassEvent = settings.renderPassEvent;
        
        if (settings.material != null)
        {
            m_Mat1 = new Material(settings.material);
            m_Mat2 = new Material(settings.material);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null || m_Mat1 == null || m_Mat2 == null) return;
        
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        
        // Enforce Clamp to prevent edge bleeding artifacts
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_SourceCopy, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_SourceCopy");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Tensor1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Tensor1");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Temp1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Temp1");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Tensor2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Tensor2");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Temp2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Temp2");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Layer1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Layer1");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Layer2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Layer2");
        
        m_Pass.Setup(settings, m_Mat1, m_Mat2, m_SourceCopy, m_Tensor1, m_Temp1, m_Tensor2, m_Temp2, m_Layer1, m_Layer2);
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        m_SourceCopy?.Release();
        m_Tensor1?.Release();
        m_Temp1?.Release();
        m_Tensor2?.Release();
        m_Temp2?.Release();
        m_Layer1?.Release();
        m_Layer2?.Release();
        
        if (m_Mat1 != null) DestroyImmediate(m_Mat1);
        if (m_Mat2 != null) DestroyImmediate(m_Mat2);
    }
}
