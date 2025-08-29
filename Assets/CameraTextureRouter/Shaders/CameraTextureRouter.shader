Shader "Hidden/URP/CameraTextureRouter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        // Single-target: Depth only
        Pass
        {
            Name "CameraTextureRouter_DepthOnly"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDepthOnly
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_MotionVectorTexture);

            CBUFFER_START(UnityPerMaterial)
            int _DepthEncoding;   // 0: RawDepth, 1: Linear01, 2: LinearEye
            int _MotionEncoding;  // unused here
            CBUFFER_END

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_Position; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
                return o;
            }

            float4 FragDepthOnly(Varyings i) : SV_Target
            {
                float rawDepth = SampleSceneDepth(i.uv);
                float depthOut = rawDepth;
                if (_DepthEncoding == 1) depthOut = Linear01Depth(rawDepth, _ZBufferParams);
                else if (_DepthEncoding == 2) depthOut = LinearEyeDepth(rawDepth, _ZBufferParams);
                return float4(depthOut, depthOut, depthOut, 1);
            }

            ENDHLSL
        }

        // Single-target: Motion only
        Pass
        {
            Name "CameraTextureRouter_MotionOnly"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragMotionOnly
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_MotionVectorTexture);

            CBUFFER_START(UnityPerMaterial)
            int _DepthEncoding;   // unused here
            int _MotionEncoding;  // 0: Signed, 1: Centered01
            CBUFFER_END

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_Position; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(v.vertexID);
                return o;
            }

            float4 FragMotionOnly(Varyings i) : SV_Target
            {
                float2 motion = SAMPLE_TEXTURE2D_X_LOD(_MotionVectorTexture, sampler_LinearClamp, i.uv, 0).xy;
                if (_MotionEncoding == 1) motion = saturate(motion * 0.5 + 0.5);
                return float4(motion, 0, 1);
            }

            ENDHLSL
        }

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

            CBUFFER_START(UnityPerMaterial)
            int _DepthEncoding;   // 0: RawBuffer, 1: Linear01, 2: LinearEye
            int _MotionEncoding;  // 0: Signed, 1: Centered01
            CBUFFER_END

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

                // Depth encoding selection
                float depthOut = rawDepth;
                if (_DepthEncoding == 1)
                {
                    depthOut = Linear01Depth(rawDepth, _ZBufferParams);
                }
                else if (_DepthEncoding == 2)
                {
                    depthOut = LinearEyeDepth(rawDepth, _ZBufferParams);
                }

                // Motion encoding selection
                float2 motion = SAMPLE_TEXTURE2D_X_LOD(_MotionVectorTexture, sampler_LinearClamp, i.uv, 0).xy;
                float2 motionOut = motion;
                if (_MotionEncoding == 1)
                {
                    motionOut = saturate(motion * 0.5 + 0.5);
                }

                o.depth = float4(depthOut, depthOut, depthOut, 1);
                o.motion = float4(motionOut, 0, 1);
                return o;
            }

            ENDHLSL
        }
    }
}
