#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Kino.Aqua.Universal {

sealed class AquaEffectPass : ScriptableRenderPass
{
    private Material _material;
    private RTHandle _tempTexture;

    public AquaEffectPass()
    {
        // Set the render pass event to run before post-processing to apply the effect nicely
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(Material material)
    {
        _material = material;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Allocate a temporary RTHandle that matches the camera resolution
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0; // We don't need depth for the temp texture
        
        // Fix for obsolete warning: Use ReAllocateHandleIfNeeded
        RenderingUtils.ReAllocateHandleIfNeeded(ref _tempTexture, desc, name: "_AquaEffectTemp");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_material == null) return;

        var cmd = CommandBufferPool.Get("AquaEffect");
        var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

        // Use Blitter for better compatibility with URP 17+ / Unity 6
        // Blit Source -> Temp (with Effect)
        Blitter.BlitCameraTexture(cmd, source, _tempTexture, _material, 0);
        // Blit Temp -> Source (Copy back)
        Blitter.BlitCameraTexture(cmd, _tempTexture, source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // RTHandles are strictly managed by the system in recent URP versions.
    }
    
    // Unity 6 Fallback: Rely on Execute (Compatibility Mode).
    // We do not override RecordRenderGraph to ensure the legacy Execute is called.
}

public sealed class AquaEffectFeature : ScriptableRendererFeature
{
    AquaEffectPass _pass;

    public override void Create()
    {
        _pass = new AquaEffectPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        var fx = camera.GetComponent<AquaEffect>();
        
        if (fx != null && fx.enabled && fx.BlitMaterial != null)
        {
            _pass.Setup(fx.BlitMaterial);
            renderer.EnqueuePass(_pass);
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        // Cleanup if necessary
    }
}

} // namespace Kino.Aqua.Universal
