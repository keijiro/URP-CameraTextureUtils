using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CameraTextureUtils {

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public sealed class CameraTextureRouter : MonoBehaviour
{
    #region Serialized Fields

    [SerializeField] RenderTexture _depthTarget = null;
    [SerializeField] RenderTexture _motionTarget = null;

    #endregion

    #region Runtime State

    (RTHandle handle, RenderTargetInfo info) _depth;
    (RTHandle handle, RenderTargetInfo info) _motion;

    #endregion

    #region Public API

    public RenderTexture DepthTarget => _depthTarget;
    public RenderTexture MotionTarget => _motionTarget;
    public (RTHandle, RenderTargetInfo) GetDepthTarget() => _depth;
    public (RTHandle, RenderTargetInfo) GetMotionTarget() => _motion;
    public bool IsReady => DepthTarget != null && MotionTarget != null;

    #endregion

    #region Public Setters

    public void SetDepthTarget(RenderTexture target)
    {
        if (_depthTarget == target) return;
        _depthTarget = target;
        UpdateDepthOutput();
    }

    public void SetMotionTarget(RenderTexture target)
    {
        if (_motionTarget == target) return;
        _motionTarget = target;
        UpdateMotionOutput();
    }

    #endregion

    #region Unity Lifecycle

    void OnEnable() => UpdateOutputs();
    void OnDisable() => ReleaseOutputs();
    void OnDestroy() => ReleaseOutputs();
    void OnValidate() { ReleaseOutputs(); UpdateOutputs(); }

    #endregion

    #region Internal Helpers

    void UpdateOutputs()
    {
        UpdateDepthOutput();
        UpdateMotionOutput();
    }

    static (RTHandle handle, RenderTargetInfo info) CreateOutput(RenderTexture target, string name)
    {
        if (target == null) return (null, default);
        var handle = RTHandles.Alloc(target, name);
        var info = new RenderTargetInfo
        {
            format = target.graphicsFormat,
            width = target.width,
            height = target.height,
            volumeDepth = target.volumeDepth,
            msaaSamples = 1,
            bindMS = target.bindTextureMS
        };
        return (handle, info);
    }

    void UpdateDepthOutput()
    {
        _depth.handle?.Release();
        _depth = CreateOutput(_depthTarget, "DepthOutput");
    }

    void UpdateMotionOutput()
    {
        _motion.handle?.Release();
        _motion = CreateOutput(_motionTarget, "MotionOutput");
    }

    void ReleaseOutputs()
    {
        _depth.handle?.Release();
        _depth = (null, default);
        _motion.handle?.Release();
        _motion = (null, default);
    }

    #endregion
}

} // namespace CameraTextureUtils
