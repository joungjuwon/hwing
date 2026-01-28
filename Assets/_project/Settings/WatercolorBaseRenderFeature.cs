using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WatercolorBaseRenderFeature : ScriptableRendererFeature
{
    static readonly int NoiseMapId = Shader.PropertyToID("_NoiseMap");
    static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    static readonly int PaperMapId = Shader.PropertyToID("_PaperMap");
    static readonly int PaperStrengthId = Shader.PropertyToID("_PaperStrength");

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader;

        [Header("Watercolor Settings")]
        public Texture2D noiseTexture;
        public float distortionScale = 1.0f;
        public float distortionStrength = 0.02f;
        public float colorLevels = 8.0f;
        public float edgeStrength = 1.0f;
        public Color edgeColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);

        [Header("Paper & Pigment Settings")]
        [Range(0f, 1f)]
        public float granulationStrength = 0.5f;
        [Range(0f, 2f)]
        public float wetEdgeStrength = 1.0f;
        [Range(0.5f, 3f)]
        public float wetEdgeSize = 1.0f;
        public bool useCustomEdgeColor = false;
        public Color wetEdgeColor = new Color(0.3f, 0.2f, 0.1f, 1.0f);
    }

    public Settings settings = new Settings();

    class WatercolorBasePass : ScriptableRenderPass
    {
        public Settings settings;
        private Material material;
        private RTHandle source;
        private RTHandle tempTexture;

        public WatercolorBasePass(Settings settings)
        {
            this.settings = settings;
            if (settings.shader != null)
            {
                material = new Material(settings.shader);
            }
        }

        public void UpdateMaterial()
        {
            if (material == null && settings.shader != null)
            {
                material = new Material(settings.shader);
            }
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;

            #pragma warning disable CS0618
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            #pragma warning restore CS0618
            if (source == null || source.rt == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Watercolor Base");

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateHandleIfNeeded(
                ref tempTexture,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_TempWatercolorBase"
            );

            if (settings.noiseTexture != null) material.SetTexture("_NoiseTex", settings.noiseTexture);
            material.SetFloat("_DistortionScale", settings.distortionScale);
            material.SetFloat("_DistortionStrength", settings.distortionStrength);
            material.SetFloat("_ColorLevels", settings.colorLevels);
            material.SetFloat("_EdgeStrength", settings.edgeStrength);
            material.SetColor("_EdgeColor", settings.edgeColor);

            material.SetFloat("_GranulationStrength", settings.granulationStrength);
            material.SetFloat("_WetEdgeStrength", settings.wetEdgeStrength);
            material.SetFloat("_WetEdgeSize", settings.wetEdgeSize);
            material.SetColor("_WetEdgeColor", settings.wetEdgeColor);
            material.SetFloat("_UseCustomEdgeColor", settings.useCustomEdgeColor ? 1.0f : 0.0f);

            if (settings.noiseTexture != null && material.HasProperty(NoiseMapId))
            {
                material.SetTexture(NoiseMapId, settings.noiseTexture);
                material.SetTextureScale(NoiseMapId, Vector2.one * settings.distortionScale);
            }
            if (material.HasProperty(NoiseStrengthId))
            {
                material.SetFloat(NoiseStrengthId, settings.distortionStrength);
            }
            if (material.HasProperty(PaperMapId) && settings.noiseTexture != null)
            {
                material.SetTexture(PaperMapId, settings.noiseTexture);
            }
            if (material.HasProperty(PaperStrengthId))
            {
                material.SetFloat(PaperStrengthId, settings.granulationStrength);
            }

            cmd.Blit(source, tempTexture, material, 0);
            cmd.Blit(tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTexture?.Release();
        }
    }

    WatercolorBasePass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new WatercolorBasePass(settings);
        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.shader != null)
        {
            m_ScriptablePass.UpdateMaterial();
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
