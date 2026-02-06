using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Skip preview / reflection / etc if needed
            if (renderingData.cameraData.isPreviewCamera)
                return;

            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawingSettings = CreateDrawingSettings(_shaderTagId, ref renderingData, sortFlags);
            drawingSettings.perObjectData = PerObjectData.None;

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filtering);
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
