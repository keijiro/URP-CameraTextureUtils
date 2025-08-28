using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CameraTextureUtils {

sealed class CameraTextureRouterPass : ScriptableRenderPass
{
    class PassData
    {
        public TextureHandle cameraDepth;
        public TextureHandle motionVector;
        public Material material;
    }

    Material _material;
    (RTHandle handle, RenderTargetInfo info) _depth;
    (RTHandle handle, RenderTargetInfo info) _motion;

    public CameraTextureRouterPass(Material material,
                                   RenderTexture depthTarget,
                                   RenderTexture motionTarget)
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);

        _material = material;
        _depth = UpdateOutput(depthTarget, "DepthOutput");
        _motion = UpdateOutput(motionTarget, "MotionOutput");
    }

    public void Cleanup()
    {
        _material = null;

        _depth.handle?.Release();
        _depth = (null, default);

        _motion.handle?.Release();
        _motion = (null, default);
    }

    (RTHandle, RenderTargetInfo) UpdateOutput(RenderTexture src, string name)
    {
        var handle = RTHandles.Alloc(src, name);
        var info = new RenderTargetInfo
        {
            format = src.graphicsFormat,
            width = src.width,
            height = src.height,
            volumeDepth = src.volumeDepth,
            msaaSamples = 1,
            bindMS = src.bindTextureMS
        };
        return (handle, info);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_material == null) return;
        if (_depth.handle == null || _motion.handle == null) return;

        var resrc = frameData.Get<UniversalResourceData>();

        var out0 = renderGraph.ImportTexture(_depth.handle, _depth.info);
        var out1 = renderGraph.ImportTexture(_motion.handle, _motion.info);

        using var builder =
          renderGraph.AddRasterRenderPass<PassData>
            ("Camera Texture Router", out var passData);

        builder.UseAllGlobalTextures(true);
        builder.AllowPassCulling(false);

        passData.material = _material;
        passData.cameraDepth = resrc.cameraDepthTexture;
        passData.motionVector = resrc.motionVectorColor;

        builder.UseTexture(passData.cameraDepth, AccessFlags.Read);
        builder.UseTexture(passData.motionVector, AccessFlags.Read);

        builder.SetRenderAttachment(out0, 0, AccessFlags.Write);
        builder.SetRenderAttachment(out1, 1, AccessFlags.Write);

        builder.SetRenderFunc<PassData>(ExecutePass);
    }

    static void ExecutePass(PassData data, RasterGraphContext ctx)
    {
        data.material.SetTexture("_CameraDepthTexture", data.cameraDepth);
        data.material.SetTexture("_MotionVectorTexture", data.motionVector);
        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
    }
}

public sealed class CameraTextureRouter : ScriptableRendererFeature
{
    [SerializeField] RenderTexture _depthTarget = null;
    [SerializeField] RenderTexture _motionTarget = null;

    [SerializeField, HideInInspector] Shader _shader = null;

    Material _material;
    CameraTextureRouterPass _pass;

    void OnValidate()
    {
        OnDestroy();
        Create();
    }

    void OnDestroy()
    {
        _pass?.Cleanup();
        if (_material != null) CoreUtils.Destroy(_material);
        (_material, _pass) = (null, null);
    }

    public override void Create()
    {
        _material = CoreUtils.CreateEngineMaterial(_shader);
        _pass = new CameraTextureRouterPass(_material, _depthTarget, _motionTarget);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
      => renderer.EnqueuePass(_pass);
}

} // namespace CameraTextureUtils
