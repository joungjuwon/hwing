Shader "Custom/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _Scale ("Scale", Float) = 1
        _DepthThreshold ("Depth Threshold", Float) = 1.5
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        [Toggle] _Debug ("Debug View", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "Outline"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
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
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float4 _OutlineColor;
            float _Scale;
            float _DepthThreshold;
            float _NormalThreshold;
            float _Debug;

            float GetDepth(float2 uv)
            {
                return SampleSceneDepth(uv);
            }

            float3 GetNormal(float2 uv)
            {
                return SampleSceneNormals(uv);
            }

            half4 Frag (Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                
                // Roberts Cross Algorithm
                // _ScreenParams.x is width, .y is height
                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * _Scale;
                
                float2 uv1 = uv + float2(-1, -1) * texelSize; // Top Left
                float2 uv2 = uv + float2( 1,  1) * texelSize; // Bottom Right
                float2 uv3 = uv + float2( 1, -1) * texelSize; // Top Right
                float2 uv4 = uv + float2(-1,  1) * texelSize; // Bottom Left
                
                // Sample Depth
                float d1 = LinearEyeDepth(GetDepth(uv1), _ZBufferParams);
                float d2 = LinearEyeDepth(GetDepth(uv2), _ZBufferParams);
                float d3 = LinearEyeDepth(GetDepth(uv3), _ZBufferParams);
                float d4 = LinearEyeDepth(GetDepth(uv4), _ZBufferParams);
                
                // Sample Normals
                float3 n1 = GetNormal(uv1);
                float3 n2 = GetNormal(uv2);
                float3 n3 = GetNormal(uv3);
                float3 n4 = GetNormal(uv4);
                
                // Calculate Differences
                float depthFiniteDiff0 = abs(d1 - d2);
                float depthFiniteDiff1 = abs(d3 - d4);
                float edgeDepth = sqrt(pow(depthFiniteDiff0, 2) + pow(depthFiniteDiff1, 2)); // Removed * 100
                
                // Normals
                float3 normalFiniteDiff0 = n1 - n2;
                float3 normalFiniteDiff1 = n3 - n4;
                float edgeNormal = sqrt(dot(normalFiniteDiff0, normalFiniteDiff0) + dot(normalFiniteDiff1, normalFiniteDiff1));
                
                // Skybox/Far Plane Mask
                // If depth is too far, ignore it.
                // LinearEyeDepth returns distance in units.
                // We can check raw depth or linear depth.
                // Let's use a safe large distance check.
                if (d1 > _ProjectionParams.z * 0.99) edgeDepth = 0;

                // Debug View: Show Differences
                // Red = Depth Diff, Green = Normal Diff
                if (_Debug > 0.5)
                {
                    return float4(edgeDepth, edgeNormal, 0, 1);
                }
                
                // Thresholding
                edgeDepth = edgeDepth > _DepthThreshold ? 1 : 0;
                edgeNormal = edgeNormal > _NormalThreshold ? 1 : 0;
                
                // Combine
                float edge = max(edgeDepth, edgeNormal);
                
                // Sample Source Color
                half4 sourceColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Apply Outline
                return lerp(sourceColor, _OutlineColor, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
