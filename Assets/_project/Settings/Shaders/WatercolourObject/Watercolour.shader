Shader "Custom/Watercolour_Erosion"
{
    Properties
    {
        [Header(Base Colors)]
        [MainColor] _BaseColor("Grass Colour", Color) = (0.5, 0.95, 0.6, 1)
        // [수정사항 1] 흙 색상 속성 추가
        _GroundColor("Ground Colour", Color) = (0.35, 0.25, 0.2, 1) 

        _ShadowColor("Shadow Colour", Color) = (0.3, 0.59, 0.61, 1)
        _DeepShadowColor("Deep Shadow Colour", Color) = (0.1, 0.3, 0.4, 1)
        
        [Header(Noise Settings)]
        _NoiseMap("Noise Texture", 2D) = "gray" {}
        _ShadowMap("Shadow Texture", 2D) = "white" {}
        _DeepShadowMap("Deep Shadow Texture", 2D) = "white" {}
        _NoiseStrength("Noise Strength", Float) = 0.2
        _NoiseBrighten("Noise Brighten", Float) = 0.1
        
        [Header(Shadow Settings)]
        _ShadowThreshold("Shadow Threshold", Range(-1, 1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0.0, 0.5)) = 0.05
        
        [Header(Deep Shadow Settings)]
        _DeepShadowThreshold("Deep Shadow Threshold", Range(-1, 1)) = 0.0
        _DeepShadowSmoothness("Deep Shadow Smoothness", Range(0.0, 0.5)) = 0.05
        _DeepShadowSpread("Deep Shadow Spread", Range(-1, 1)) = 0.0
        _DeepShadowFalloff("Deep Shadow Falloff", Range(0.1, 5)) = 1.0
        
        [Header(Texture Strength Settings)]
        _ShadowMapStrength("Shadow Map Strength", Range(0, 1)) = 1.0
        _DeepShadowMapStrength("Deep Shadow Map Strength", Range(0, 1)) = 1.0
        
        [Header(Fresnel Settings)]
        _FresnelAmount("Fresnel Amount", Float) = 0.5
        _FresnelPower("Fresnel Power", Float) = 3.0
        _FresnelThreshold("Fresnel Threshold", Range(0, 1)) = 0.5
        _FresnelSmoothness("Fresnel Smoothness", Range(0.001, 1)) = 0.1

        [Header(Outline Settings)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
        [Toggle(_USE_SMOOTHED_NORMALS_ENABLED)] _USE_SMOOTHED_NORMALS_ENABLED("Use Smoothed Normals (UV3)", Float) = 0
        
        [Header(Outline Noise)]
        _OutlineTexture ("Outline Texture (Alpha)", 2D) = "white" {}
        [Toggle(_INNER_OUTLINE)] _UseInnerOutline("Enable Inner Outline (Front)", Float) = 0
        _InnerOutlineAlpha("Inner Outline Alpha", Range(0, 1)) = 0.5
        _InnerOutlineThreshold("Inner Outline Threshold", Range(0, 1)) = 0.5
        _InnerOutlineSmoothness("Inner Outline Smoothness", Range(0, 1)) = 0.1
        _InnerOutlineNoiseStrength("Inner Outline Noise Strength", Range(0, 2)) = 0.5
        _InnerOutlineEdgePower("Inner Outline Edge Power", Range(1, 10)) = 1.0
        [Toggle(_TEXTURE_AS_MASK)] _UseTextureAsMask("Use Texture as Mask (Black=Transparent)", Float) = 0
        
        _OutlineNoiseTexture ("Noise Texture", 2D) = "white" {} 
        _OutlineNoiseFrequency ("Noise Frequency", Float) = 5.0
        _OutlineNoiseFramerate ("Noise Framerate", Float) = 12.0
        [Toggle(_RANDOM_OFFSETS_ENABLED)] _RANDOM_OFFSETS_ENABLED("Randomly offset the sample position", Float) = 0

        [Header(Highlight Settings)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Glossiness("Glossiness", Range(0.01, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite On 

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                // [수정사항 2] 스크립트에서 보내주는 Vertex Color(Mask) 수신
                float4 color : COLOR; 
                float3 normalSmooth : TEXCOORD3; 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
                // [수정사항 3] Fragment Shader로 색상 데이터 전달
                float4 color : TEXCOORD5; 
                float fogFactor : TEXCOORD3;
                float3 normalSmoothWS : TEXCOORD4; 
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GroundColor; // [수정사항 4] 변수 선언 추가
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowThreshold;
                float _DeepShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _ShadowMapStrength;
                float _DeepShadowMapStrength;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _SpecularColor;
                float _Glossiness;
                
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float _InnerOutlineThreshold;
                float _InnerOutlineSmoothness;
                float _InnerOutlineNoiseStrength;
                float _InnerOutlineEdgePower;
                float _UseTextureAsMask;
                float4 _OutlineTexture_ST;
                
                float _USE_SMOOTHED_NORMALS_ENABLED;
            CBUFFER_END

            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_ShadowMap); SAMPLER(sampler_ShadowMap);
            TEXTURE2D(_DeepShadowMap); SAMPLER(sampler_DeepShadowMap);
            TEXTURE2D(_OutlineTexture); SAMPLER(sampler_OutlineTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                if (_USE_SMOOTHED_NORMALS_ENABLED > 0.5)
                    output.normalSmoothWS = TransformObjectToWorldNormal(input.normalSmooth);
                else
                    output.normalSmoothWS = output.normalWS;

                output.uv = input.uv;
                
                // [수정사항 5] Vertex Color 데이터 전달
                output.color = input.color; 
                
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = SafeNormalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));

                // --- [수정사항 6] 블렌딩 로직 구현 (핵심) ---
                // Vertex Color의 Red 채널을 마스크로 사용 (0:잔디, 1:흙)
                float mask = input.color.r;
                
                // 잔디색(_BaseColor)과 흙색(_GroundColor)을 마스크 비율로 섞음
                float3 baseTarget = lerp(_BaseColor.rgb, _GroundColor.rgb, mask);

                // --- Lighting Calculation ---
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                float NdotL = dot(normalWS, mainLight.direction);
                float shadowAtten = mainLight.shadowAttenuation;

                float2 noiseUV = TRANSFORM_TEX(input.uv, _NoiseMap);
                float2 shadowUV = TRANSFORM_TEX(input.uv, _ShadowMap);
                float2 deepShadowUV = TRANSFORM_TEX(input.uv, _DeepShadowMap);

                half4 noiseSample = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV);
                float noiseVal = noiseSample.r;
                
                half4 shadowTexSample = SAMPLE_TEXTURE2D(_ShadowMap, sampler_ShadowMap, shadowUV);
                float3 shadowPattern = shadowTexSample.rgb;

                half4 deepShadowTexSample = SAMPLE_TEXTURE2D(_DeepShadowMap, sampler_DeepShadowMap, deepShadowUV);
                float3 deepShadowPattern = deepShadowTexSample.rgb;

                float3 shadowTexMixed = lerp(float3(1,1,1), shadowPattern, _ShadowMapStrength);
                float3 effectiveShadow = _ShadowColor.rgb * shadowTexMixed;
                
                float3 deepShadowTexMixed = lerp(float3(1,1,1), deepShadowPattern, _DeepShadowMapStrength);
                float3 effectiveDeepShadow = _DeepShadowColor.rgb * deepShadowTexMixed;

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, _Glossiness * 128.0);
                float3 specularColor = specular * _SpecularColor.rgb;

                float noisyNdotL = NdotL + (noiseVal - 0.5) * _NoiseStrength;

                float gradientInput = noisyNdotL + _DeepShadowSpread;
                float remappedGradient = saturate(gradientInput * 0.5 + 0.5);
                float globalShading = pow(remappedGradient, _DeepShadowFalloff);

                float shadowEdge = _ShadowThreshold;
                float shadowSmooth = _ShadowSmoothness;
                float shadowFactor = smoothstep(shadowEdge - shadowSmooth, shadowEdge + shadowSmooth, noisyNdotL);
                
                shadowFactor = min(shadowFactor, shadowAtten);
                globalShading = min(globalShading, shadowAtten);
                
                // [수정사항 7] 원본 _BaseColor 대신 블렌딩된 baseTarget 사용
                float3 celColor = lerp(effectiveShadow, baseTarget, shadowFactor);
                float3 mixedBase = lerp(effectiveDeepShadow, celColor, globalShading);
                
                float3 finalColor = mixedBase + (noiseVal - 0.5) * _NoiseBrighten * 0.5;
                finalColor += specularColor;

                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnelBase = pow(1.0 - NdotV, _FresnelPower);
                float fresnel = smoothstep(_FresnelThreshold, _FresnelThreshold + _FresnelSmoothness, fresnelBase);
                
                // [수정사항 8] 프레넬 색상도 블렌딩된 색상 기준으로 적용
                finalColor += fresnel * _FresnelAmount * baseTarget; 

                MixFog(finalColor, input.fogFactor);

                if (_UseInnerOutline > 0.5)
                {
                    float2 outlineUV = TRANSFORM_TEX(input.uv, _OutlineTexture);
                    half4 outlineTexColor = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, outlineUV);
                    
                    float3 viewDir = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                    float3 smoothNormal = SafeNormalize(input.normalSmoothWS);
                    float NdotV_inner = saturate(dot(smoothNormal, viewDir));
                    
                    float fresnelBaseInner = 1.0 - NdotV_inner;
                    fresnelBaseInner += (outlineTexColor.r - 0.5) * _InnerOutlineNoiseStrength;
                    float innerFresnel = smoothstep(_InnerOutlineThreshold, _InnerOutlineThreshold + max(_InnerOutlineSmoothness, 0.001), fresnelBaseInner);
                    float edgePower = max(_InnerOutlineEdgePower, 0.1);
                    innerFresnel = pow(max(innerFresnel, 0.0), edgePower);
                    
                    float overlayAlpha = 0;
                    float3 overlayColor = _OutlineColor.rgb;
                    
                    if (_UseTextureAsMask > 0.5)
                    {
                        overlayAlpha = outlineTexColor.r * _InnerOutlineAlpha * innerFresnel;
                        overlayColor = _OutlineColor.rgb;
                    }
                    else
                    {
                        overlayAlpha = outlineTexColor.a * _InnerOutlineAlpha * innerFresnel;
                        overlayColor = _OutlineColor.rgb * outlineTexColor.rgb;
                    }
                    finalColor = lerp(finalColor, overlayColor, overlayAlpha);
                }

                return float4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
        
        // --- Outline Pass (변동 없음) ---
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float nrand(float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453); }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR; // Outline pass에서도 일단 받아둠 (사용은 안함)
                float3 normalSmooth : TEXCOORD3; 
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _USE_SMOOTHED_NORMALS_ENABLED;
                float4 _OutlineNoiseTexture_ST;
                float _OutlineNoiseFrequency;
                float _OutlineNoiseFramerate;
                float _RANDOM_OFFSETS_ENABLED;
                float4 _OutlineTexture_ST;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
            CBUFFER_END
            
            TEXTURE2D(_OutlineNoiseTexture); SAMPLER(sampler_OutlineNoiseTexture);
            TEXTURE2D(_OutlineTexture); SAMPLER(sampler_OutlineTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                if(_USE_SMOOTHED_NORMALS_ENABLED > 0.5) normalWS = TransformObjectToWorldNormal(input.normalSmooth);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float thickness = max(_OutlineWidth, 0);
                float dist = length(input.positionOS.xyz);
                float r = _Time.y * _OutlineNoiseFramerate;
                if(_RANDOM_OFFSETS_ENABLED > 0.5) r = nrand(floor(r));
                
                float noiseVal = SAMPLE_TEXTURE2D_LOD(_OutlineNoiseTexture, sampler_OutlineNoiseTexture, float2(r + (dist * _OutlineNoiseFrequency), 0), 0).r;
                thickness *= noiseVal;
                positionWS += normalWS * thickness;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.texcoord, _OutlineTexture);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, input.uv);
                return float4(_OutlineColor.rgb * texColor.rgb, _OutlineColor.a);
            }
            ENDHLSL
        }

        // --- DepthNormals Pass (변동 없음) ---
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 normalSmooth : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _USE_SMOOTHED_NORMALS_ENABLED;
            CBUFFER_END

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                if(_USE_SMOOTHED_NORMALS_ENABLED > 0.5) normalWS = TransformObjectToWorldNormal(input.normalSmooth);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = NormalizeNormalPerVertex(normalWS);
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 normalVS = TransformWorldToViewNormal(normalWS);
                return float4(normalVS * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }

        // --- ShadowCaster Pass (변동 없음) ---
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.uv = input.texcoord;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);
                output.positionCS = TransformWorldToHClip(biasedPositionWS);
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}