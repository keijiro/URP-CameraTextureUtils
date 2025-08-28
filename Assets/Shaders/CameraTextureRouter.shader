Shader "Hidden/URP/CameraTextureRouter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "CameraTextureRouter"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_MotionVectorTexture);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float2 uv         : TEXCOORD0;
                uint2  pixelXY    : TEXCOORD1;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
                o.pixelXY = (uint2)o.positionCS.xy;
                return o;
            }

            // Second color target (SV_Target1) is defined via MRT semantics in URP; we output using a separate function signature.
            struct MRT
            {
                float4 depth  : SV_Target0;
                float4 motion : SV_Target1;
            };

            MRT Frag(Varyings i)
            {
                MRT o;
                float rawDepth = SampleSceneDepth(i.uv);
                float2 motion = SAMPLE_TEXTURE2D_X_LOD(_MotionVectorTexture, sampler_LinearClamp, i.uv, 0).xy;
                o.depth = float4(rawDepth, rawDepth, rawDepth, 1);
                o.motion = float4(motion, 0, 1);
                return o;
            }

            ENDHLSL
        }
    }
}
