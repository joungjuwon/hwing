Shader "WC/InnerEdge"
{
    Properties
    {
        _EdgeColor("Edge Color", Color) = (0.2,0.0,0.4,1)
        _Scale("Scale", Float) = 1
        _DepthThreshold("Depth Threshold", Float) = 0.6
        _NormalThreshold("Normal Threshold", Float) = 0.35
        _EdgeAlpha("Edge Alpha", Range(0,1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 100

        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "InnerEdge"

            // Only inside watercolor objects
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            float4 _EdgeColor;
            float _Scale;
            float _DepthThreshold;
            float _NormalThreshold;
            float _EdgeAlpha;

            float DepthAt(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float3 NormalAt(float2 uv)
            {
                // Scene normals are in world space for URP normals texture.
                return SampleSceneNormals(uv);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float2 texel = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * _Scale;

                float2 uv1 = uv + float2(-1, -1) * texel;
                float2 uv2 = uv + float2( 1,  1) * texel;
                float2 uv3 = uv + float2( 1, -1) * texel;
                float2 uv4 = uv + float2(-1,  1) * texel;

                float d1 = DepthAt(uv1);
                float d2 = DepthAt(uv2);
                float d3 = DepthAt(uv3);
                float d4 = DepthAt(uv4);

                float3 n1 = NormalAt(uv1);
                float3 n2 = NormalAt(uv2);
                float3 n3 = NormalAt(uv3);
                float3 n4 = NormalAt(uv4);

                float edgeDepth = sqrt(pow(abs(d1 - d2), 2) + pow(abs(d3 - d4), 2));
                float edgeNormal = sqrt(dot(n1 - n2, n1 - n2) + dot(n3 - n4, n3 - n4));

                float eD = edgeDepth > _DepthThreshold ? 1.0 : 0.0;
                float eN = edgeNormal > _NormalThreshold ? 1.0 : 0.0;
                float edge = max(eD, eN);

                float a = edge * _EdgeAlpha * _EdgeColor.a;
                return float4(_EdgeColor.rgb, a);
            }
            ENDHLSL
        }
    }
}
