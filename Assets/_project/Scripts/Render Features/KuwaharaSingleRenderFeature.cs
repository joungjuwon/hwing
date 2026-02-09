using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KuwaharaSingleRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material material;

        [Range(2, 20)] public int kernelSize = 4;
        [Range(0.1f, 5.0f)] public float tensorSpread = 1.0f;
        [Range(0f, 2f)] public float weightH = 1.0f;
        [Range(0f, 2f)] public float weightV = 1.0f;
        [Range(0f, 2f)] public float anisotropy = 1.0f;
        [Range(1f, 20f)] public float sharpness = 12.0f;
    }

    public Settings settings = new Settings();

    private RTHandle m_SourceCopy;
    private RTHandle m_Tensor;
    private RTHandle m_Temp;

    private Material m_Material;
    private KuwaharaSinglePass m_Pass;

    class KuwaharaSinglePass : ScriptableRenderPass
    {
        private Settings m_Settings;
        private Material m_Mat;
        private RTHandle m_SourceCopy;
        private RTHandle m_Tensor;
        private RTHandle m_Temp;

        public void Setup(Settings settings, Material mat, RTHandle sourceCopy, RTHandle tensor, RTHandle temp)
        {
            m_Settings = settings;
            m_Mat = mat;
            m_SourceCopy = sourceCopy;
            m_Tensor = tensor;
            m_Temp = temp;
        }

#pragma warning disable CS0672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
#pragma warning restore CS0672

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Mat == null || m_Settings == null) return;

            var cmd = CommandBufferPool.Get("KuwaharaSingle");
            var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            if (cameraColorTarget == null || cameraColorTarget.rt == null ||
                m_SourceCopy == null || m_Tensor == null || m_Temp == null)
            {
                CommandBufferPool.Release(cmd);
                return;
            }

            m_Mat.SetFloat("_KuwaharaSize", m_Settings.kernelSize);
            m_Mat.SetFloat("_TensorSpread", m_Settings.tensorSpread);
            m_Mat.SetFloat("_WeightH", m_Settings.weightH);
            m_Mat.SetFloat("_WeightV", m_Settings.weightV);
            m_Mat.SetFloat("_Anisotropy", m_Settings.anisotropy);
            m_Mat.SetFloat("_Sharpness", m_Settings.sharpness);

            // Source copy
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, m_SourceCopy);

            // Pass 0~2: structure tensor + blur H/V
            Blitter.BlitCameraTexture(cmd, m_SourceCopy, m_Tensor, m_Mat, 0);
            Blitter.BlitCameraTexture(cmd, m_Tensor, m_Temp, m_Mat, 1);
            Blitter.BlitCameraTexture(cmd, m_Temp, m_Tensor, m_Mat, 2);

            // Pass 3: single anisotropic Kuwahara
            cmd.SetGlobalTexture("_StructureTensorTex", m_Tensor);
            Blitter.BlitCameraTexture(cmd, m_SourceCopy, cameraColorTarget, m_Mat, 3);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public override void Create()
    {
        m_Pass = new KuwaharaSinglePass();
        m_Pass.renderPassEvent = settings.renderPassEvent;

        if (settings.material != null)
        {
            m_Material = new Material(settings.material);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
        {
            return;
        }

        if (m_Material == null || m_Material.shader != settings.material.shader)
        {
            if (m_Material != null) DestroyImmediate(m_Material);
            m_Material = new Material(settings.material);
        }

        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;

        RenderingUtils.ReAllocateHandleIfNeeded(ref m_SourceCopy, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaSingleSource");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Tensor, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaSingleTensor");
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_Temp, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaSingleTemp");

        m_Pass.Setup(settings, m_Material, m_SourceCopy, m_Tensor, m_Temp);
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        m_SourceCopy?.Release();
        m_Tensor?.Release();
        m_Temp?.Release();

        if (m_Material != null)
        {
            DestroyImmediate(m_Material);
        }
    }
}
