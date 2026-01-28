using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class WatercolorRenderFeature : ScriptableRendererFeature
{
    static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
    static readonly int NoiseMapId = Shader.PropertyToID("_NoiseMap");
    static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    static readonly int PaperMapId = Shader.PropertyToID("_PaperMap");
    static readonly int PaperStrengthId = Shader.PropertyToID("_PaperStrength");
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
        
        [Header("Edge Bleeding Pipeline")]
        public bool enableEdgeBleeding = true;
        public Shader blurShader;
        public BlendStage blendStage = BlendStage.Blur2;
        
        [Header("Stage 1: Pre-Blur")]
        [Range(1, 4)]
        [FormerlySerializedAs("blurIterations")]
        public int blur1Iterations = 2;
        [Range(0.5f, 4f)]
        [FormerlySerializedAs("blurSize")]
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

    class WatercolorPass : ScriptableRenderPass
    {
        public Settings settings;
        private Material material;
        private Material blurMaterial;
        private RTHandle source;
        private RTHandle tempTexture;
        private RTHandle blurTexture1;
        private RTHandle blurTexture2;
        private RTHandle kuwaharaTexture;
        
        // Dual-layer Kuwahara textures
        private RTHandle layer1Texture;
        private RTHandle layer2Texture;
        private RTHandle tensor1;
        private RTHandle tensor2;
        private RTHandle tensorTemp;

        public WatercolorPass(Settings settings)
        {
            this.settings = settings;
            if (settings.shader != null)
            {
                material = new Material(settings.shader);
            }
            if (settings.blurShader != null)
            {
                blurMaterial = new Material(settings.blurShader);
            }
        }

        public void UpdateMaterials()
        {
            if (blurMaterial == null && settings.blurShader != null)
            {
                blurMaterial = new Material(settings.blurShader);
            }

            if (material == null && settings.shader != null)
            {
                material = new Material(settings.shader);
            }
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;
            
            // Early exit if camera target is not valid
            #pragma warning disable CS0618
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            #pragma warning restore CS0618
            if (source == null || source.rt == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Watercolor Effect");

            // Setup Temp texture
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateHandleIfNeeded(ref tempTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempWatercolorTexture");

            // Set properties
            if (settings.noiseTexture != null) material.SetTexture("_NoiseTex", settings.noiseTexture);
            material.SetFloat("_DistortionScale", settings.distortionScale);
            material.SetFloat("_DistortionStrength", settings.distortionStrength);
            material.SetFloat("_ColorLevels", settings.colorLevels);
            material.SetFloat("_EdgeStrength", settings.edgeStrength);
            material.SetColor("_EdgeColor", settings.edgeColor);
            
            // New Paper & Pigment properties
            material.SetFloat("_GranulationStrength", settings.granulationStrength);
            material.SetFloat("_WetEdgeStrength", settings.wetEdgeStrength);
            material.SetFloat("_WetEdgeSize", settings.wetEdgeSize);
            material.SetColor("_WetEdgeColor", settings.wetEdgeColor);
            material.SetFloat("_UseCustomEdgeColor", settings.useCustomEdgeColor ? 1.0f : 0.0f);

            // Compatibility with WatercolourBlit.shader property names
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

            // Blit watercolor effect
            cmd.Blit(source, tempTexture, material, 0);
            
            // Edge Bleeding (if enabled and blur material exists)
            if (settings.enableEdgeBleeding && blurMaterial != null)
            {
                // Allocate textures
                // Blur textures are half resolution for performance
                var blurDesc = desc;
                blurDesc.width = Mathf.Max(1, blurDesc.width / 2);
                blurDesc.height = Mathf.Max(1, blurDesc.height / 2);
                
                // Kuwahara needs full resolution
                var fullDesc = desc;
                
                RenderingUtils.ReAllocateHandleIfNeeded(ref blurTexture1, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_WatercolorBlur1");
                RenderingUtils.ReAllocateHandleIfNeeded(ref blurTexture2, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_WatercolorBlur2");
                RenderingUtils.ReAllocateHandleIfNeeded(ref kuwaharaTexture, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_WatercolorKuwahara");
                
                // Dual-layer textures
                RenderingUtils.ReAllocateHandleIfNeeded(ref layer1Texture, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaLayer1");
                RenderingUtils.ReAllocateHandleIfNeeded(ref layer2Texture, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KuwaharaLayer2");
                RenderingUtils.ReAllocateHandleIfNeeded(ref tensor1, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Tensor1");
                RenderingUtils.ReAllocateHandleIfNeeded(ref tensor2, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_Tensor2");
                RenderingUtils.ReAllocateHandleIfNeeded(ref tensorTemp, fullDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TensorTemp");
                
                RTHandle resultTexture = null;

                // --- Stage 1: Pre-Blur ---
                ApplyBlur(cmd, tempTexture, blurTexture1, blurTexture2, settings.blur1Size, settings.blur1Iterations);
                resultTexture = blurTexture1;

                if (settings.blendStage >= BlendStage.Kuwahara)
                {
                    // === SINGLE KUWAHARA + OVERLAY EFFECT ===
                    // Compute Kuwahara ONCE, then blend with offset copy (like Photoshop layer duplicate)
                    
                    // 1. Structure Tensor
                    blurMaterial.SetFloat("_WeightH", settings.layer1WeightH);
                    blurMaterial.SetFloat("_WeightV", settings.layer1WeightV);
                    cmd.Blit(blurTexture1, tensor1, blurMaterial, 3); // Pass 3: Structure Tensor
                    
                    // 2. Blur Tensor
                    cmd.Blit(tensor1, tensorTemp, blurMaterial, 0); // Horizontal
                    cmd.Blit(tensorTemp, tensor1, blurMaterial, 1); // Vertical
                    
                    // 3. Kuwahara (SINGLE computation)
                    cmd.SetGlobalTexture("_StructureTensorTex", tensor1);
                    blurMaterial.SetFloat("_KuwaharaSize", settings.layer1KernelSize);
                    blurMaterial.SetFloat("_AnisotropyStrength", settings.layer1Anisotropy);
                    blurMaterial.SetFloat("_Sharpness", settings.layer1Sharpness);
                    cmd.Blit(blurTexture1, layer1Texture, blurMaterial, 4); // Pass 4: Anisotropic Kuwahara
                    
                    // 4. Copy layer1 to layer2 for safe blending (avoid same-texture read/write)
                    cmd.Blit(layer1Texture, layer2Texture);
                    
                    // 5. Blend layer1 with offset copy (like Photoshop layer duplicate)
                    // Layer 1 = base (normal UV), Layer 2 = copy with offset
                    blurMaterial.SetFloat("_BlendFactor", settings.layerBlendFactor);
                    blurMaterial.SetVector("_Layer2Offset", settings.layer2Offset);
                    cmd.SetGlobalTexture("_Layer2Tex", layer2Texture); // Use COPY for overlay (safe)
                    cmd.Blit(layer1Texture, kuwaharaTexture, blurMaterial, 5); // Pass 5: Blend
                    
                    resultTexture = kuwaharaTexture;
                }

                if (settings.blendStage >= BlendStage.Blur2)
                {
                    // --- Stage 3: Post-Blur ---
                    ApplyBlur(cmd, kuwaharaTexture, blurTexture1, blurTexture2, settings.blur2Size, settings.blur2Iterations);
                    resultTexture = blurTexture1;
                }
                
                // --- Composite ---
                blurMaterial.SetFloat("_BleedingIntensity", settings.bleedingIntensity);
                blurMaterial.SetFloat("_EdgeSensitivity", settings.edgeSensitivity);
                blurMaterial.SetFloat("_UseEdgeDetection", settings.useEdgeDetection ? 1f : 0f);
                
                cmd.SetGlobalTexture("_BlurredTex", resultTexture);
                cmd.Blit(tempTexture, source, blurMaterial, 2);
            }
            else
            {
                // No bleeding, just copy result
                cmd.Blit(tempTexture, source);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        private void ApplyBlur(CommandBuffer cmd, RTHandle input, RTHandle blur1, RTHandle blur2, float size, int iterations)
        {
            blurMaterial.SetFloat("_BlurSize", size);
            
            // Initial blit (can be downsample if input is larger than blur1)
            cmd.Blit(input, blur1, blurMaterial, 0); // Horizontal pass 1
            
            // Iterations
            for (int i = 0; i < iterations; i++)
            {
                // Vertical blur
                cmd.Blit(blur1, blur2, blurMaterial, 1);
                
                // Horizontal blur (back to blur1)
                // If this is the last iteration, we end up in blur1
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

    WatercolorPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new WatercolorPass(settings);
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
            m_ScriptablePass.UpdateMaterials();
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}
