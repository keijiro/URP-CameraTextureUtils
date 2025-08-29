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

    public CameraTextureRouterPass(Material material)
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Motion);

        _material = material;
    }

    public void Cleanup()
    {
        _material = null;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_material == null) return;

        // Pull targets from the controller attached to the current camera
        var cam = frameData.Get<UniversalCameraData>().camera;
        var controller = cam != null ? cam.GetComponent<CameraTextureRouter>() : null;
        if (controller == null || !controller.enabled || !controller.IsReady) return;

        var resrc = frameData.Get<UniversalResourceData>();

        var (depthHandle, depthInfo) = controller.GetDepthTarget();
        var (motionHandle, motionInfo) = controller.GetMotionTarget();
        if (depthHandle == null || motionHandle == null) return;

        var out0 = renderGraph.ImportTexture(depthHandle, depthInfo);
        var out1 = renderGraph.ImportTexture(motionHandle, motionInfo);

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

public sealed class CameraTextureRouterFeature : ScriptableRendererFeature
{
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
        _pass = new CameraTextureRouterPass(_material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
      => renderer.EnqueuePass(_pass);
}

} // namespace CameraTextureUtils
