using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Blit : ScriptableRendererFeature
{
    [System.Serializable]
    public class BlitSettings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingPostProcessing;
        public Material blitMaterial = null;
        public int blitMaterialPassIndex = 0;
    }

    public BlitSettings settings = new BlitSettings();
    BlitPass blitPass;

    public override void Create()
    {
        blitPass = new BlitPass(settings.Event, settings, name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blitMaterial == null)
        {
            Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute.", GetType().Name);
            return;
        }

        blitPass.Setup(renderer);
        renderer.EnqueuePass(blitPass);
    }

    protected override void Dispose(bool disposing)
    {
        blitPass?.Dispose();
    }

    class BlitPass : ScriptableRenderPass
    {
        public Material blitMaterial = null;
        public int blitMaterialPassIndex = 0;
        private ScriptableRenderer renderer;
        private string profilerTag;
        private int tempTextureId;

        public BlitPass(RenderPassEvent renderPassEvent, BlitSettings settings, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = settings.blitMaterial;
            this.blitMaterialPassIndex = settings.blitMaterialPassIndex;
            this.profilerTag = tag;
            this.tempTextureId = Shader.PropertyToID("_BlitTempTexture");
        }

        public void Setup(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Request Depth and Normal textures
            // This is required for the shader to sample _CameraDepthTexture and _CameraNormalsTexture
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            cmd.GetTemporaryRT(tempTextureId, opaqueDesc, FilterMode.Bilinear);

            if (blitMaterial == null)
            {
                CommandBufferPool.Release(cmd);
                return;
            }

            // Blit CameraTarget (Screen) -> Temp
            cmd.Blit(BuiltinRenderTextureType.CameraTarget, tempTextureId, blitMaterial, blitMaterialPassIndex);
            
            // Blit Temp -> CameraTarget (Write back to Screen)
            cmd.Blit(tempTextureId, BuiltinRenderTextureType.CameraTarget);

            cmd.ReleaseTemporaryRT(tempTextureId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose() {}
    }
}