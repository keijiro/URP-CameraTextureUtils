# URP Camera Texture Utilities

Custom renderer features for Universal Render Pipeline (URP) to handle camera
textures (depth and motion vectors).

Currently provided:

- CameraTextureRouter — copies the camera depth and motion vector textures to
  user‑provided RenderTextures.

Support for additional textures (normal vectors, G‑buffers, etc.) may be added
in the future.

## Requirements

- Unity 6 (6000.0) or later
- Universal Render Pipeline 17.x

## Installation

Copy the folder `Packages/jp.keijiro.urp-cameratextureutils` into your project.

Note: Not published to a registry; the package is experimental.

## Setup

- Add `CameraTextureRouterFeature` to the Renderer Features list in your URP
  Renderer asset. See the [Unity documentation][1] for steps.
- Add the `CameraTextureRouter` component to each camera that should output the
  textures. The pass runs only on cameras with this component.

[1]: https://docs.unity3d.com/6000.0/Documentation/Manual/urp/urp-renderer-feature.html
