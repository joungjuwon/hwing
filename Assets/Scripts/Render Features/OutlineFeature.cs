using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;
        public Material outlineMaterial = null;
        [Range(0, 5)] public float scale = 1;
        public Color color = Color.black;
        [Range(0, 10)] public float depthThreshold = 1.5f;
        [Range(0, 1)] public float normalThreshold = 0.4f;
        public bool debug = false;
    }

    public OutlineSettings settings = new OutlineSettings();
    OutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlinePass(settings);
        outlinePass.renderPassEvent = settings.Event;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null)
        {
            Debug.LogWarning("Outline Material is missing.");
            return;
        }
        
        outlinePass.Setup(renderer);
        renderer.EnqueuePass(outlinePass);
    }

    class OutlinePass : ScriptableRenderPass
    {
        private OutlineSettings settings;
        private Material material;
        private ScriptableRenderer renderer;
        private int tempTextureId;

        public OutlinePass(OutlineSettings settings)
        {
            this.settings = settings;
            this.material = settings.outlineMaterial;
            this.tempTextureId = Shader.PropertyToID("_OutlineTempTexture");
        }

        public void Setup(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // CRITICAL: Request Depth and Normals
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            
            // Update Material Properties
            if (material != null)
            {
                material.SetColor("_OutlineColor", settings.color);
                material.SetFloat("_Scale", settings.scale);
                material.SetFloat("_DepthThreshold", settings.depthThreshold);
                material.SetFloat("_NormalThreshold", settings.normalThreshold);
                material.SetFloat("_Debug", settings.debug ? 1 : 0);
            }
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Outline Feature");
            
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            cmd.GetTemporaryRT(tempTextureId, opaqueDesc, FilterMode.Bilinear);

            // Blit Screen -> Temp (Apply Outline)
            // We use CameraTarget as source to ensure we catch everything rendered so far
            cmd.Blit(BuiltinRenderTextureType.CameraTarget, tempTextureId, material, 0);
            
            // Blit Temp -> Screen
            cmd.Blit(tempTextureId, BuiltinRenderTextureType.CameraTarget);

            cmd.ReleaseTemporaryRT(tempTextureId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
