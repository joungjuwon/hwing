using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

// Screen-space inner edge pass (depth/normal) masked to watercolor objects only.
// Workflow:
// 1) Render watercolor objects into stencil via shader pass tag "WCStencil" (ColorMask 0).
// 2) Fullscreen edge detect, with Stencil Comp Equal (inside only), blended over camera color.
public class InnerEdgeRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        // Stencil is written after opaques; edge composite works best after transparents.
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

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
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

            // Create RendererList for Stencil pass
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            var filterSettings = _filtering;
            var drawSettings = CreateDrawingSettings(_shaderTagId, ref cameraData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;

            var param = new RendererListParams(cameraData.cullingResults, drawSettings, filterSettings);
            var rendererListHandle = renderGraph.CreateRendererList(param);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("InnerEdge Stencil Pass", out var passData))
            {
                passData.rendererListHandle = rendererListHandle;
                
                builder.UseRendererList(rendererListHandle);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachment(resourceData.activeDepthTexture, AccessFlags.Write); // Depth/Stencil write

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }

        class PassData { public RendererListHandle rendererListHandle; }
    }

    class EdgePass : ScriptableRenderPass
    {
        readonly Settings _settings;
        RTHandle _temp;

        public EdgePass(Settings s)
        {
            _settings = s;
            // No intermediate tax needed for RG usually as we make handles
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
           // Legacy execution... (kept for compatibility mode if needed, but warning suggests migration)
           if (_settings.edgeMaterial == null) return;
           var renderer = renderingData.cameraData.renderer;
           var src = renderer.cameraColorTargetHandle;
           if (src == null || src.rt == null) return;
           if (renderingData.cameraData.isSceneViewCamera && !Application.isPlaying) { }

           var cmd = CommandBufferPool.Get("InnerEdge");
           var desc = renderingData.cameraData.cameraTargetDescriptor;
           desc.depthBufferBits = 0;
           RenderingUtils.ReAllocateIfNeeded(ref _temp, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_InnerEdgeTemp");

           Blitter.BlitCameraTexture(cmd, src, _temp, _settings.edgeMaterial, _settings.edgeMaterialPass);
           Blitter.BlitCameraTexture(cmd, _temp, src);

           context.ExecuteCommandBuffer(cmd);
           CommandBufferPool.Release(cmd);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_settings.edgeMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // 1. Create Temp Texture
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1; // Edge detect usually doesn't need MSAA resolution if blitting
            TextureHandle tempHandle = renderGraph.CreateTexture(desc);

            TextureHandle sourceHandle = resourceData.activeColorTexture;

            // Pass 1: Edge Detect (Source -> Temp)
            using (var builder = renderGraph.AddRasterRenderPass<EdgePassData>("InnerEdge Detect", out var passData))
            {
                passData.material = _settings.edgeMaterial;
                passData.passIndex = _settings.edgeMaterialPass;
                passData.source = sourceHandle;
                
                builder.UseTexture(sourceHandle);
                builder.SetRenderAttachment(tempHandle, 0);

                builder.SetRenderFunc((EdgePassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1,1,0,0), data.material, data.passIndex);
                });
            }

            // Pass 2: Copy Back (Temp -> Source)
            using (var builder = renderGraph.AddRasterRenderPass<EdgePassData>("InnerEdge Composite", out var passData))
            {
                passData.source = tempHandle; // Now temp is source
                
                builder.UseTexture(tempHandle);
                builder.SetRenderAttachment(sourceHandle, 0);

                builder.SetRenderFunc((EdgePassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1,1,0,0), 0, false);
                });
            }
        }
        
        class EdgePassData
        {
            public Material material;
            public int passIndex;
            public TextureHandle source;
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
        // 1) Write stencil right after opaques.
        _stencilPass = new StencilPass(settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };

        // 2) Composite edges after transparents so the final look matches what you see.
        _edgePass = new EdgePass(settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
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
