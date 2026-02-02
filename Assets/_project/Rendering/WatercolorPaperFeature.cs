#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class WatercolorPaperFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Shader paperBlitShader;
        public Texture2D paperTexture;

        [Header("Paper Look")]
        public Vector4 paperST = new Vector4(1, 1, 0, 0); // tiling.xy, offset.zw
        [Range(0, 1)] public float saturation = 0.75f;
        [Range(0.5f, 2.0f)] public float contrast = 1.2f;
        [Range(-0.5f, 0.5f)] public float brightness = 0.0f;

        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingOpaques;
        public bool forceIntermediateTexture = true;
    }

    public Settings settings = new Settings();

    Material _mat;
    PaperPass _pass;

    public override void Create()
    {
        if (settings.paperBlitShader == null)
            settings.paperBlitShader = Shader.Find("Hidden/Watercolor/PaperBlit");

        if (settings.paperBlitShader != null)
            _mat = CoreUtils.CreateEngineMaterial(settings.paperBlitShader);

        _pass = new PaperPass(_mat, settings)
        {
            renderPassEvent = settings.passEvent
        };

        // Sampling/Blit often needs an intermediate texture
        _pass.requiresIntermediateTexture = settings.forceIntermediateTexture;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_mat == null || settings.paperTexture == null) return;
        renderer.EnqueuePass(_pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        _pass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_mat);
    }

    class PaperPass : ScriptableRenderPass
    {
        const string k_PassName = "WatercolorPaperPass";
        readonly Material _mat;
        readonly Settings _settings;
        RTHandle _colorTarget;

        static readonly int PaperTexID = Shader.PropertyToID("_PaperTex");
        static readonly int PaperSTID = Shader.PropertyToID("_PaperST");
        static readonly int SatID = Shader.PropertyToID("_PaperSaturation");
        static readonly int ContrastID = Shader.PropertyToID("_PaperContrast");
        static readonly int BrightID = Shader.PropertyToID("_PaperBrightness");

        public PaperPass(Material mat, Settings settings)
        {
            _mat = mat;
            _settings = settings;
            profilingSampler = new ProfilingSampler(k_PassName);
        }

        public void Setup(RTHandle colorTarget) => _colorTarget = colorTarget;

        void SetupMaterial()
        {
            _mat.SetTexture(PaperTexID, _settings.paperTexture);
            _mat.SetVector(PaperSTID, _settings.paperST);
            _mat.SetFloat(SatID, _settings.saturation);
            _mat.SetFloat(ContrastID, _settings.contrast);
            _mat.SetFloat(BrightID, _settings.brightness);
        }

        // Compatibility Mode (Execute) path
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_mat == null) return;

            // In URP 17 compatibility mode, we need to handle the RT explicitly if we want to read/write same target
            var cameraData = renderingData.cameraData;
            var source = cameraData.renderer.cameraColorTargetHandle;

            SetupMaterial();
            
            var cmd = CommandBufferPool.Get(k_PassName);
            using (new ProfilingScope(cmd, profilingSampler))
            {
                // Allocate temp texture
                var desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0; // No depth needed
                
                // Using a unique name for the temp texture
                var tempHandle = RTHandles.Alloc(desc, name: "_WatercolorPaperTemp");

                // Blit Source -> Temp (Apply Effect)
                Blitter.BlitCameraTexture(cmd, source, tempHandle, _mat, 0);

                // Blit Temp -> Source (Copy Back)
                Blitter.BlitCameraTexture(cmd, tempHandle, source);

                // Cleanup relies on RTHandles cache, but we should release the reference locally
                // Note: Standard RTHandles.Release isn't manual like old GetTemporaryRT
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // RenderGraph path
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_mat == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError("WatercolorPaperPass: Cannot use BackBuffer as input. Intermediate ColorTexture required.");
                return;
            }

            SetupMaterial();

            var source = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "CameraColor-WatercolorPaper";
            desc.clearBuffer = false;
            desc.depthBufferBits = 0;

            var destination = renderGraph.CreateTexture(desc);

            var para = new RenderGraphUtils.BlitMaterialParameters(source, destination, _mat, 0);
            renderGraph.AddBlitPass(para, passName: k_PassName);

            // Swap camera color for subsequent passes
            resourceData.cameraColor = destination;
        }
    }
}
