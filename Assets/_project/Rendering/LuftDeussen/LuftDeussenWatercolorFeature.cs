using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LuftDeussenWatercolorFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader;
        
        [Header("Textures (Assign Required Textures)")]
        public Texture2D paperTexture;
        public Texture2D turbulenceTexture;
        public Texture2D dispersal1Texture;
        public Texture2D dispersal2Texture;
        
        [Header("Performance")]
        public bool halfResolution = false;
    }
    
    public Settings settings = new Settings();
    LuftDeussenPass pass;
    
    class LuftDeussenPass : ScriptableRenderPass
    {
        public Settings settings;
        private Material material;
        private LuftDeussenWatercolorVolume volume;
        
        private RTHandle densityTexture;
        private RTHandle modifiedColorTexture;
        private RTHandle blurTempTexture;
        private RTHandle resultTexture;
        
        public LuftDeussenPass(Settings settings)
        {
            this.settings = settings;
            if (settings.shader != null)
                material = new Material(settings.shader);
        }
        
        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            
            var blurDesc = desc;
            if (settings.halfResolution)
            {
                blurDesc.width = Mathf.Max(1, blurDesc.width / 2);
                blurDesc.height = Mathf.Max(1, blurDesc.height / 2);
            }
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref densityTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_DensityTex");
            RenderingUtils.ReAllocateHandleIfNeeded(ref modifiedColorTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ModifiedColorTex");
            RenderingUtils.ReAllocateHandleIfNeeded(ref blurTempTexture, blurDesc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_BlurTemp");
            RenderingUtils.ReAllocateHandleIfNeeded(ref resultTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_WatercolorResult");
        }
        
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;
            
            var stack = VolumeManager.instance.stack;
            volume = stack.GetComponent<LuftDeussenWatercolorVolume>();
            if (volume == null || !volume.IsActive()) return;
            
            #pragma warning disable CS0618
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            #pragma warning restore CS0618
            if (source == null || source.rt == null) return;
            
            CommandBuffer cmd = CommandBufferPool.Get("Luft-Deussen Watercolor");
            
            // Set Textures from Settings (with safe defaults)
            material.SetTexture("_PaperTex", settings.paperTexture ? settings.paperTexture : Texture2D.whiteTexture);
            material.SetTexture("_TurbulenceTex", settings.turbulenceTexture ? settings.turbulenceTexture : Texture2D.grayTexture);
            material.SetTexture("_Dispersal1Tex", settings.dispersal1Texture ? settings.dispersal1Texture : Texture2D.grayTexture);
            material.SetTexture("_Dispersal2Tex", settings.dispersal2Texture ? settings.dispersal2Texture : Texture2D.grayTexture);
            
            // Set Parameters from Volume
            material.SetFloat("_EdgeThreshold", volume.edgeThreshold.value);
            material.SetFloat("_EdgeDarkening", volume.edgeDarkening.value);
            material.SetFloat("_DensityBase", volume.densityBase.value);
            material.SetFloat("_DensityContrast", volume.densityContrast.value);
            material.SetFloat("_PaperStrength", volume.paperStrength.value);
            material.SetFloat("_TurbulenceStrength", volume.turbulenceStrength.value);
            material.SetFloat("_Dispersal1Strength", volume.dispersal1Strength.value);
            material.SetFloat("_Dispersal2Strength", volume.dispersal2Strength.value);
            material.SetFloat("_TextureScale", volume.textureScale.value);
            material.SetFloat("_BlurSize", volume.blurSize.value);
            
            // Wobble and Quantization parameters
            material.SetFloat("_WobbleStrength", volume.wobbleStrength.value);
            material.SetFloat("_ColorSteps", (float)volume.colorSteps.value);
            material.SetFloat("_EnableQuantization", volume.enableQuantization.value ? 1.0f : 0.0f);
            
            // Pass 0: Edge Detection & Density Map
            if (densityTexture != null)
                Blitter.BlitCameraTexture(cmd, source, densityTexture, material, 0);
            
            // Pass 1: Color Modification
            if (densityTexture != null && modifiedColorTexture != null)
            {
                cmd.SetGlobalTexture("_DensityTex", densityTexture);
                Blitter.BlitCameraTexture(cmd, source, modifiedColorTexture, material, 1);
            }
            
            // Pass 2 & 3: Blur Loop
            RTHandle currentSource = modifiedColorTexture;
            RTHandle currentDest = blurTempTexture;
            RTHandle finalBlurResult = modifiedColorTexture; // Default if 0 iterations
            
            int iterations = volume.blurIterations.value;
            if (blurTempTexture != null && resultTexture != null)
            {
                for (int i = 0; i < iterations; i++)
                {
                    // Horizontal
                    Blitter.BlitCameraTexture(cmd, currentSource, currentDest, material, 2);
                    
                    // Vertical
                    Blitter.BlitCameraTexture(cmd, currentDest, resultTexture, material, 3);
                    
                    // Prepare for next iteration
                    currentSource = resultTexture;
                    finalBlurResult = resultTexture;
                }
            }
            
            // Pass 4: Composite
            // We need to blit back to the camera target. 
            // Ensure _DensityTex is set again just in case.
            if (densityTexture != null)
            {
                cmd.SetGlobalTexture("_DensityTex", densityTexture);
                Blitter.BlitCameraTexture(cmd, finalBlurResult, source, material, 4);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public void Dispose()
        {
            densityTexture?.Release();
            modifiedColorTexture?.Release();
            blurTempTexture?.Release();
            resultTexture?.Release();
        }
    }
    
    public override void Create()
    {
        pass = new LuftDeussenPass(settings);
        pass.renderPassEvent = settings.renderPassEvent;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.shader == null) return;
        renderer.EnqueuePass(pass);
    }
    
    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }
}
