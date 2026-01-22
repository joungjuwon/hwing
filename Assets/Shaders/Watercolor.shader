Shader "Custom/Watercolor"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        [Header(Mapping)]
        [Toggle(_TRIPLANAR)] _UseTriplanar("Use Triplanar Mapping", Float) = 0
        _TriplanarScale("Triplanar Scale", Float) = 0.1
        
        [Header(Watercolor Settings)]
        _WatercolorNoise("Watercolor Noise", 2D) = "white" {}
        _DistortionScale("Distortion Scale", Float) = 1.0
        _DistortionStrength("Distortion Strength", Range(0, 1)) = 0.1
        _RampSteps("Ramp Steps", Range(1, 10)) = 3
        
        [Header(Lighting)]
        _ShadowColor("Shadow Color", Color) = (0.7, 0.7, 0.8, 1)
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.0
        
        [Header(Pigment)]
        _PigmentEdgeSize("Pigment Edge Size", Range(0, 0.2)) = 0.05
        _PigmentEdgeColor("Pigment Edge Color", Color) = (0.4, 0.4, 0.5, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature_local _TRIPLANAR
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _UseTriplanar;
                float _TriplanarScale;
                float4 _WatercolorNoise_ST;
                float _DistortionScale;
                float _DistortionStrength;
                float _RampSteps;
                float4 _ShadowColor;
                float4 _RimColor;
                float _RimPower;
                float _PigmentEdgeSize;
                float4 _PigmentEdgeColor;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_WatercolorNoise);
            SAMPLER(sampler_WatercolorNoise);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float3 SampleWatercolorNoise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_WatercolorNoise, sampler_WatercolorNoise, uv).rgb;
            }

            float4 SampleTriplanar(float3 positionWS, float3 normalWS, float scale)
            {
                float2 uvX = positionWS.zy * scale;
                float2 uvY = positionWS.xz * scale;
                float2 uvZ = positionWS.xy * scale;
                
                float3 blend = abs(normalWS);
                blend /= dot(blend, 1.0);
                
                float4 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX);
                float4 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY);
                float4 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ);
                
                return colX * blend.x + colY * blend.y + colZ * blend.z;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Sample Noise for distortion
                float2 noiseUV = input.positionWS.xz * _DistortionScale; // World space noise
                float3 noise = SampleWatercolorNoise(noiseUV);
                float3 distortion = (noise - 0.5) * _DistortionStrength;

                // Distort World Position for Shadow Sampling
                float3 distortedPosWS = input.positionWS + distortion;
                
                // Main Light
                float4 shadowCoord = TransformWorldToShadowCoord(distortedPosWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                
                // NdotL
                float NdotL = dot(normalWS, lightDir);
                
                // Apply Banding (Toon Ramp)
                float halfLambert = NdotL * 0.5 + 0.5;
                float noisyLambert = halfLambert + (noise.r - 0.5) * 0.1; 
                float banded = floor(noisyLambert * _RampSteps) / _RampSteps;
                
                float shadowAtten = mainLight.shadowAttenuation;
                
                // Pigment Build-up
                float pigmentEdge = 0;
                float rampFrac = frac(noisyLambert * _RampSteps);
                if (rampFrac < _PigmentEdgeSize && banded < 1.0)
                {
                    pigmentEdge = 1.0;
                }

                // Mix Base Color
                float4 baseMap;
                #if _TRIPLANAR
                    baseMap = SampleTriplanar(input.positionWS, normalWS, _TriplanarScale);
                #else
                    baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                #endif
                
                float3 albedo = baseMap.rgb * _BaseColor.rgb;

                // Combine Lighting
                float3 litColor = albedo * mainLight.color;
                float3 shadowColor = albedo * _ShadowColor.rgb;
                
                float lightIntensity = banded * shadowAtten;
                float3 finalColor = lerp(shadowColor, litColor, lightIntensity);
                
                // Apply Pigment Edge
                finalColor = lerp(finalColor, _PigmentEdgeColor.rgb * finalColor, pigmentEdge);

                // Rim Light
                float NdotV = dot(normalWS, viewDirectionWS);
                float rim = 1.0 - saturate(NdotV);
                rim = pow(rim, _RimPower);
                finalColor += _RimColor.rgb * rim * 0.5;

                return float4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
