Shader "Watercolor/URP/Grass"
{
    Properties
    {
        [Header(Base Settings)]
        [MainColor] _BaseColor("Colour Tint", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Base Map", 2D) = "white" {}
        _Cutoff("Alpha Cutout", Range(0, 1)) = 0.5

        [Header(Base Texture vs Palette)]
        _WC_PaletteStrength("Palette Strength (0=keep texture color)", Range(0,1)) = 0.35
        _WC_BaseDetailStrength("Base Detail Strength (luma)", Range(0,1)) = 0.35
        
        [Header(Grass Gradient)]
        _TopColor("Top Color", Color) = (0.4, 0.8, 0.2, 1)
        _BottomColor("Bottom Color", Color) = (0.1, 0.3, 0.1, 1)
        
        [Header(Wind Animation)]
        _WindSpeed("Wind Speed", Float) = 1.0
        _WindStrength("Wind Strength", Range(0, 1)) = 0.2
        _WindFrequency("Wind Frequency", Float) = 2.0
        _WindGust("Wind Gust", Float) = 0.5
        
        [Header(Complex Watercolor)]
        _RampLightingA("Ramp Lighting A", 2D) = "white" {}
        _RampLightingB("Ramp Lighting B", 2D) = "white" {}
        _RampEdgeA("Ramp Edge A", 2D) = "white" {}
        _RampEdgeB("Ramp Edge B", 2D) = "white" {}
        _RampEdgeCol("Ramp Edge Color", 2D) = "white" {}
        _LayerBlend("Edge Blend", Range(0, 1)) = 0.2
        
        [Header(Paper Texture)]
        _PaperTex("Paper Texture", 2D) = "white" {}
        _PaperTiling("Paper Tiling", Float) = 1.0
        _PaperStrength("Paper Strength", Range(0, 1)) = 0.5
        
        [Header(Distortion)]
        _NoiseStrength("Distortion Strength", Range(0, 1)) = 0.1
        _NoiseScale("Noise Scale", Float) = 3.6
        _NoiseDetail("Noise Detail", Float) = 2.0
        _NoiseRoughness("Noise Roughness", Float) = 0.5
        _NoiseDistortion("Noise Distortion", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Off
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
            #include "Assets/_project/Shaders/Watercolor/Core/WatercolorCore.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float _Cutoff;
                
                float4 _TopColor;
                float4 _BottomColor;
                
                float _WindSpeed;
                float _WindStrength;
                float _WindFrequency;
                float _WindGust;
                
                float _LayerBlend;
                float _WC_PaletteStrength;
                float _WC_BaseDetailStrength;
                float _PaperTiling;
                float _PaperStrength;
                
                float _NoiseStrength;
                float _NoiseScale;
                float _NoiseDetail;
                float _NoiseRoughness;
                float _NoiseDistortion;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_RampLightingA); SAMPLER(sampler_RampLightingA);
            TEXTURE2D(_RampLightingB); SAMPLER(sampler_RampLightingB);
            TEXTURE2D(_RampEdgeA); SAMPLER(sampler_RampEdgeA);
            TEXTURE2D(_RampEdgeB); SAMPLER(sampler_RampEdgeB);
            TEXTURE2D(_RampEdgeCol); SAMPLER(sampler_RampEdgeCol);
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float windMask = input.uv.y;
                float windTime = _Time.y * _WindSpeed;
                float3 windOffset = float3(0,0,0);
                
                float gust = sin(worldPos.x * 0.1 + windTime * 0.5) * 0.5 + 0.5;
                float strength = _WindStrength * (1.0 + gust * _WindGust);
                
                windOffset.x = sin(windTime + worldPos.x * _WindFrequency) * strength;
                windOffset.z = cos(windTime * 0.8 + worldPos.z * _WindFrequency) * strength;
                windOffset *= windMask * windMask;
                
                input.positionOS.xyz += TransformWorldToObjectDir(windOffset);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                output.color = lerp(_BottomColor, _TopColor, input.uv.y);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(baseTex.a - _Cutoff);

                // Base texture vs palette control
                float3 albedo;
                float detailMul;
                {
                    float3 baseForLighting;
                    WatercolorComputeBase_float(baseTex.rgb, _BaseColor.rgb * input.color.rgb, _WC_PaletteStrength, _WC_BaseDetailStrength, baseForLighting, detailMul);
                    albedo = baseForLighting;
                }
                
                // Normal Distortion (Updated Parameters)
                float3 normalWS = SafeNormalize(input.normalWS);
                float3 positionOS = TransformWorldToObject(input.positionWS);
                float3 normalOS = TransformWorldToObjectNormal(normalWS);
                
                float3 distortedNormalOS;
                WatercolorDistortNormal_float(
                    positionOS, normalOS, 
                    _NoiseScale, _NoiseDetail, _NoiseRoughness, _NoiseDistortion, 
                    _NoiseStrength, distortedNormalOS
                );
                float3 distortedNormalWS = TransformObjectToWorldNormal(distortedNormalOS);
                distortedNormalWS = SafeNormalize(distortedNormalWS);

                // Lighting (Complex)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 wcColor;
                float3 viewDirWS = SafeNormalize(input.viewDirWS);
                
                WatercolorLightingComplex_float(
                    albedo,
                    distortedNormalWS,
                    viewDirWS,
                    mainLight.direction,
                    mainLight.color,
                    mainLight.shadowAttenuation,
                    _RampLightingA, sampler_RampLightingA,
                    _RampLightingB, sampler_RampLightingB,
                    _RampEdgeA, sampler_RampEdgeA,
                    _RampEdgeB, sampler_RampEdgeB,
                    _RampEdgeCol, sampler_RampEdgeCol,
                    _LayerBlend,
                    wcColor
                );

                wcColor *= detailMul;
                
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                screenUV.x *= _ScreenParams.x / _ScreenParams.y;
                
                float3 finalColor;
                WatercolorPaper_float(
                    wcColor,
                    screenUV,
                    _PaperTex, sampler_PaperTex,
                    _PaperTiling,
                    _PaperStrength,
                    finalColor
                );
                
                finalColor = MixFog(finalColor, input.fogFactor);
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
