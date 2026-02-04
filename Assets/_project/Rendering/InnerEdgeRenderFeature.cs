using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Screen-space inner edge pass (depth/normal) masked to watercolor objects only.
// Workflow:
// 1) Render watercolor objects into stencil via shader pass tag "WCStencil" (ColorMask 0).
// 2) Fullscreen edge detect, with Stencil Comp Equal (inside only), blended over camera color.
public class InnerEdgeRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
        public LayerMask layerMask = ~0;

        [Tooltip("Shader pass tag used to write stencil on watercolor objects.")]
        public string stencilShaderTag = "WCStencil";

        [Tooltip("Material that performs fullscreen edge detect.")]
        public Material edgeMaterial;

        [Tooltip("Edge material pass index.")]
        public int edgeMaterialPass = 0;
    }

    public Settings settings = new Settings();

    class StencilPass : ScriptableRenderPass
    {
        readonly ShaderTagId _shaderTagId;
        FilteringSettings _filtering;

        public StencilPass(Settings s)
        {
            _shaderTagId = new ShaderTagId(string.IsNullOrEmpty(s.stencilShaderTag) ? "WCStencil" : s.stencilShaderTag);
            _filtering = new FilteringSettings(RenderQueueRange.opaque, s.layerMask);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isPreviewCamera)
                return;

            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawingSettings = CreateDrawingSettings(_shaderTagId, ref renderingData, sortFlags);
            drawingSettings.perObjectData = PerObjectData.None;

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filtering);
        }
    }

    class EdgePass : ScriptableRenderPass
    {
        readonly Settings _settings;
        RTHandle _temp;

        public EdgePass(Settings s)
        {
            _settings = s;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_settings.edgeMaterial == null)
                return;

            var renderer = renderingData.cameraData.renderer;
            var src = renderer.cameraColorTargetHandle;
            if (src == null || src.rt == null)
                return;

            var cmd = CommandBufferPool.Get("InnerEdge");

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _temp, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_InnerEdgeTemp");

            // src -> temp (edge)
            Blitter.BlitCameraTexture(cmd, src, _temp, _settings.edgeMaterial, _settings.edgeMaterialPass);
            // temp -> src
            Blitter.BlitCameraTexture(cmd, _temp, src);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // RTHandle is reused; no release here.
        }
    }

    StencilPass _stencilPass;
    EdgePass _edgePass;

    public override void Create()
    {
        _stencilPass = new StencilPass(settings)
        {
            renderPassEvent = settings.passEvent
        };

        _edgePass = new EdgePass(settings)
        {
            renderPassEvent = settings.passEvent + 1 // right after stencil
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_stencilPass == null || _edgePass == null)
            Create();

        renderer.EnqueuePass(_stencilPass);
        renderer.EnqueuePass(_edgePass);
    }
}
