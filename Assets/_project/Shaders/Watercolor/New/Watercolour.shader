Shader "Watercolor/URP/Watercolour"
{
    Properties
    {
        [MainColor] _BaseColor("Colour Tint", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Base Map (Texture)", 2D) = "white" {}
        _Cutoff("Alpha Cutout", Range(0, 1)) = 0.5

        _ShadowColor("Shadow Tint", Color) = (0.3, 0.59, 0.61, 1)
        _DeepShadowColor("Deep Shadow Colour", Color) = (0.1, 0.3, 0.4, 1)

        [Header(Noise & Texture)]
        _NoiseMap("Noise Texture", 2D) = "gray" {}
        _ShadowMap("Shadow Texture", 2D) = "white" {}
        _DeepShadowMap("Deep Shadow Texture", 2D) = "white" {}
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.2
        _NoiseBrighten("Noise Brighten", Range(0, 1)) = 0.1

        [Header(Shadow Controls)]
        _ShadowThreshold("Shadow Threshold", Range(-1, 1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0, 0.5)) = 0.05
        _DeepShadowSpread("Deep Shadow Spread", Range(-1, 1)) = 0.0
        _DeepShadowFalloff("Deep Shadow Falloff", Range(0.1, 5)) = 1.0

        [Header(Fresnel Rim)]
        _FresnelAmount("Fresnel Amount", Range(0, 1)) = 0.5
        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 3.0
        _FresnelThreshold("Fresnel Threshold", Range(0, 1)) = 0.5
        _FresnelSmoothness("Fresnel Smoothness", Range(0.01, 1)) = 0.1

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Glossiness("Glossiness", Range(0, 1)) = 0.5
        _SpecularNoiseStrength("Specular Noise Strength", Range(0, 1)) = 0.3

        [Header(Inner Outline)]
        [Toggle] _UseInnerOutline("Use Inner Outline", Float) = 0
        _InnerOutlineAlpha("Inner Outline Alpha", Range(0, 1)) = 0.5

        [Header(Outer Outline)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/_project/Shaders/Watercolor/New/WatercolorCore.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_ShadowMap);
            SAMPLER(sampler_ShadowMap);
            TEXTURE2D(_DeepShadowMap);
            SAMPLER(sampler_DeepShadowMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = SafeNormalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));

                float2 mainUV = TRANSFORM_TEX(input.uv, _MainTex);
                float4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);
                float4 texColor = texSample * _BaseColor;
                clip(texColor.a - _Cutoff);

                float2 noiseUV = TRANSFORM_TEX(input.uv, _NoiseMap);
                float noiseVal = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV).r;

                float2 shadowUV = TRANSFORM_TEX(input.uv, _ShadowMap);
                float shadowPattern = SAMPLE_TEXTURE2D(_ShadowMap, sampler_ShadowMap, shadowUV).r;

                float2 deepShadowUV = TRANSFORM_TEX(input.uv, _DeepShadowMap);
                float deepShadowPattern = SAMPLE_TEXTURE2D(_DeepShadowMap, sampler_DeepShadowMap, deepShadowUV).r;

                float4 shadowCoord;
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
                    shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightColor = mainLight.color * mainLight.distanceAttenuation;

                float3 baseColor = texColor.rgb;
                float noiseBright = (noiseVal - 0.5) * 2.0;
                baseColor *= (1.0 + noiseBright * _NoiseBrighten);

                float3 shadowColor = _ShadowColor.rgb * shadowPattern;
                float3 deepShadowColor = _DeepShadowColor.rgb * deepShadowPattern;

                float3 coreColor;
                float shadowFactor;
                float deepShadowFactor;
                WatercolorCoreEx_float(
                    baseColor,
                    normalWS,
                    mainLight.direction,
                    lightColor,
                    mainLight.shadowAttenuation,
                    shadowColor,
                    deepShadowColor,
                    noiseVal,
                    _NoiseStrength,
                    _ShadowThreshold,
                    _ShadowSmoothness,
                    _DeepShadowSpread,
                    _DeepShadowFalloff,
                    coreColor,
                    shadowFactor,
                    deepShadowFactor
                );

                float3 finalColor = coreColor;

                float3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
                float ndh = saturate(dot(normalWS, halfDir));
                float noisyNdh = saturate(ndh + (noiseVal - 0.5) * _SpecularNoiseStrength);
                float specPower = lerp(8.0, 128.0, _Glossiness);
                float specular = pow(noisyNdh, specPower);
                specular *= shadowFactor;
                finalColor += specular * _SpecularColor.rgb * lightColor;

                if (_UseInnerOutline > 0.5)
                {
                    float edge = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 2.0);
                    finalColor *= (1.0 - edge * _InnerOutlineAlpha);
                }

                float rimBase = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                float rim = smoothstep(_FresnelThreshold, _FresnelThreshold + _FresnelSmoothness, rimBase);
                finalColor += rim * _FresnelAmount * baseColor * lightColor;

                MixFog(finalColor, input.fogFactor);

                return float4(saturate(finalColor), texColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Offset 1, 1

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings OutlineVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                positionWS += normalWS * _OutlineWidth;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            float4 OutlineFrag(Varyings input) : SV_Target
            {
                clip(_OutlineWidth - 1e-5);
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                float3 normalVS = TransformWorldToViewNormal(input.normalWS);
                return float4(normalVS * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);

                output.positionCS = TransformWorldToHClip(biasedPositionWS);
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 ShadowPassFragment(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
