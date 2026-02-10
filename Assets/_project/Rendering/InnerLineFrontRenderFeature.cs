using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;


// Renders the "InnerLineFront" shader pass as an extra draw after opaques.
// This is required because URP's forward opaque pass selects only ONE pass per object
// (UniversalForward/UniversalForwardOnly/SRPDefaultUnlit) and will not automatically execute
// additional custom passes on the same material.
public class InnerLineFrontRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
        public LayerMask layerMask = ~0;
        public RenderQueueType renderQueue = RenderQueueType.Opaque;
        public string shaderTag = "InnerLineFront";
    }

    public Settings settings = new Settings();

    class Pass : ScriptableRenderPass
    {
        readonly Settings _settings;
        readonly ShaderTagId _shaderTagId;
        FilteringSettings _filtering;

        public Pass(Settings settings)
        {
            _settings = settings;
            _shaderTagId = new ShaderTagId(string.IsNullOrEmpty(settings.shaderTag) ? "InnerLineFront" : settings.shaderTag);
            renderPassEvent = settings.passEvent;

            var range = settings.renderQueue == RenderQueueType.Transparent ? RenderQueueRange.transparent : RenderQueueRange.opaque;
            _filtering = new FilteringSettings(range, settings.layerMask);
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Compatibility mode fallback
            if (renderingData.cameraData.isPreviewCamera)
                return;

            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawingSettings = CreateDrawingSettings(_shaderTagId, ref renderingData, sortFlags);
            drawingSettings.perObjectData = PerObjectData.None;

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filtering);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.isPreviewCamera) return;

            // 1. Create RendererList
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            var renderQueueRange = (_settings.renderQueue == RenderQueueType.Transparent) ? RenderQueueRange.transparent : RenderQueueRange.opaque;
            var filterSettings = new FilteringSettings(renderQueueRange, _settings.layerMask);
            
            // CreateDrawingSettings logic inside RG:
            // We need to construct parameters similar to CreateDrawingSettings
            var drawSettings = CreateDrawingSettings(_shaderTagId, ref cameraData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None; // Match old logic
            
            var param = new RendererListParams(cameraData.cullingResults, drawSettings, filterSettings);
            var rendererListHandle = renderGraph.CreateRendererList(param);

            // 2. Schedule Pass
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("InnerLineFront Pass", out var passData))
            {
                passData.rendererListHandle = rendererListHandle;
                
                // Set extraction target (Color), assume Depth is inherited or needed?
                // We are drawing ON TOP of opaques, so we need the current color/depth.
                builder.UseRendererList(rendererListHandle);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachment(resourceData.activeDepthTexture, AccessFlags.Write); // Depth test?

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }

        private class PassData
        {
            public RendererListHandle rendererListHandle;
        }
    }

    Pass _pass;

    public override void Create()
    {
        _pass = new Pass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null) Create();
        renderer.EnqueuePass(_pass);
    }
}
