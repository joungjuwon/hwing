using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WatercolorEdgeBleedingRenderFeature : ScriptableRendererFeature
{
    public enum BlendStage
    {
        Blur1,
        Kuwahara,
        Blur2
    }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public bool enableEdgeBleeding = true;
        public Shader blurShader;
        public BlendStage blendStage = BlendStage.Blur2;

        [Header("Stage 1: Pre-Blur")]
        [Range(1, 4)]
        public int blur1Iterations = 2;
        [Range(0.5f, 4f)]
        public float blur1Size = 1.5f;

        [Header("Stage 2: Kuwahara")]
        [Range(2, 20)]
        public int layer1KernelSize = 5;
        [Range(0f, 2f)]
        public float layer1WeightH = 1.0f;
        [Range(0f, 2f)]
        public float layer1WeightV = 1.0f;
        [Range(0f, 2f)]
        public float layer1Anisotropy = 1.0f;
        [Range(1f, 20f)]
        public float layer1Sharpness = 10.0f;

        [Header("Stage 2: Overlay Effect (Offset Blending)")]
        [Range(0f, 1f)]
        public float layerBlendFactor = 0.3f;
        public Vector2 layer2Offset = new Vector2(2f, 2f);

        [Header("Stage 3: Post-Blur")]
        [Range(1, 4)]
        public int blur2Iterations = 2;
        [Range(0.5f, 4f)]
        public float blur2Size = 1.5f;

        [Header("Composite Settings")]
        [Range(0f, 1f)]
        public float bleedingIntensity = 0.3f;
        [Range(0.01f, 1f)]
        public float edgeSensitivity = 0.1f;
        public bool useEdgeDetection = true;
    }

    public Settings settings = new Settings();

    class WatercolorEdgeBleedingPass : ScriptableRenderPass
    {
        public Settings settings;
        private Material blurMaterial;
        private RTHandle source;
        private RTHandle tempTexture;
        private RTHandle blurTexture1;
        private RTHandle blurTexture2;
        private RTHandle kuwaharaTexture;
        private RTHandle layer1Texture;
        private RTHandle layer2Texture;
        private RTHandle tensor1;
        private RTHandle tensor2;
        private RTHandle tensorTemp;

        public WatercolorEdgeBleedingPass(Settings settings)
        {
            this.settings = settings;
            if (settings.blurShader != null)
            {
                blurMaterial = new Material(settings.blurShader);
            }
        }

        public void UpdateMaterial()
        {
            if (blurMaterial == null && settings.blurShader != null)
            {
                blurMaterial = new Material(settings.blurShader);
            }
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!settings.enableEdgeBleeding || blurMaterial == null) return;

            #pragma warning disable CS0618
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            #pragma warning restore CS0618
            if (source == null || source.rt == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Watercolor Edge Bleeding");

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateHandleIfNeeded(
                ref tempTexture,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_TempWatercolorEdge"
            );

            cmd.Blit(source, tempTexture);

            var blurDesc = desc;
            blurDesc.width = Mathf.Max(1, blurDesc.width / 2);
            blurDesc.height = Mathf.Max(1, blurDesc.height / 2);

            var fullDesc = desc;

            RenderingUtils.ReAllocateHandleIfNeeded(ref blurTexture1, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_WatercolorBlur1");
            RenderingUtils.ReAllocateHandleIfNeeded(ref blurTexture2, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_WatercolorBlur2");
            RenderingUtils.ReAllocateHandleIfNeeded(ref kuwaharaTexture, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_WatercolorKuwahara");

            RenderingUtils.ReAllocateHandleIfNeeded(ref layer1Texture, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaLayer1");
            RenderingUtils.ReAllocateHandleIfNeeded(ref layer2Texture, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaLayer2");
            RenderingUtils.ReAllocateHandleIfNeeded(ref tensor1, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Tensor1");
            RenderingUtils.ReAllocateHandleIfNeeded(ref tensor2, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Tensor2");
            RenderingUtils.ReAllocateHandleIfNeeded(ref tensorTemp, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TensorTemp");

            RTHandle resultTexture = null;

            ApplyBlur(cmd, tempTexture, blurTexture1, blurTexture2, settings.blur1Size, settings.blur1Iterations);
            resultTexture = blurTexture1;

            if (settings.blendStage >= BlendStage.Kuwahara)
            {
                blurMaterial.SetFloat("_WeightH", settings.layer1WeightH);
                blurMaterial.SetFloat("_WeightV", settings.layer1WeightV);
                cmd.Blit(blurTexture1, tensor1, blurMaterial, 3);

                cmd.Blit(tensor1, tensorTemp, blurMaterial, 0);
                cmd.Blit(tensorTemp, tensor1, blurMaterial, 1);

                cmd.SetGlobalTexture("_StructureTensorTex", tensor1);
                blurMaterial.SetFloat("_KuwaharaSize", settings.layer1KernelSize);
                blurMaterial.SetFloat("_AnisotropyStrength", settings.layer1Anisotropy);
                blurMaterial.SetFloat("_Sharpness", settings.layer1Sharpness);
                cmd.Blit(blurTexture1, layer1Texture, blurMaterial, 4);

                cmd.Blit(layer1Texture, layer2Texture);

                blurMaterial.SetFloat("_BlendFactor", settings.layerBlendFactor);
                blurMaterial.SetVector("_Layer2Offset", settings.layer2Offset);
                cmd.SetGlobalTexture("_Layer2Tex", layer2Texture);
                cmd.Blit(layer1Texture, kuwaharaTexture, blurMaterial, 5);

                resultTexture = kuwaharaTexture;
            }

            if (settings.blendStage >= BlendStage.Blur2)
            {
                ApplyBlur(cmd, kuwaharaTexture, blurTexture1, blurTexture2, settings.blur2Size, settings.blur2Iterations);
                resultTexture = blurTexture1;
            }

            blurMaterial.SetFloat("_BleedingIntensity", settings.bleedingIntensity);
            blurMaterial.SetFloat("_EdgeSensitivity", settings.edgeSensitivity);
            blurMaterial.SetFloat("_UseEdgeDetection", settings.useEdgeDetection ? 1f : 0f);

            cmd.SetGlobalTexture("_BlurredTex", resultTexture);
            cmd.Blit(tempTexture, source, blurMaterial, 2);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ApplyBlur(CommandBuffer cmd, RTHandle input, RTHandle blur1, RTHandle blur2, float size, int iterations)
        {
            blurMaterial.SetFloat("_BlurSize", size);
            cmd.Blit(input, blur1, blurMaterial, 0);

            for (int i = 0; i < iterations; i++)
            {
                cmd.Blit(blur1, blur2, blurMaterial, 1);
                cmd.Blit(blur2, blur1, blurMaterial, 0);
            }
        }

        public void Dispose()
        {
            tempTexture?.Release();
            blurTexture1?.Release();
            blurTexture2?.Release();
            kuwaharaTexture?.Release();
            layer1Texture?.Release();
            layer2Texture?.Release();
            tensor1?.Release();
            tensor2?.Release();
            tensorTemp?.Release();
        }
    }

    WatercolorEdgeBleedingPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new WatercolorEdgeBleedingPass(settings);
        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blurShader != null)
        {
            m_ScriptablePass.UpdateMaterial();
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
