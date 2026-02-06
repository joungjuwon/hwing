Shader "Watercolor/URP/Water"
{
    Properties
    {
        [Header(Base Color)]
        _BaseColor("Shallow Color", Color) = (0.3, 0.8, 0.9, 0.5)
        _DeepColor("Deep Color", Color) = (0.0, 0.2, 0.5, 1.0)
        _DepthFactor("Depth Factor", Range(0.01, 5.0)) = 1.0
        
        [Header(Foam)]
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamThreshold("Foam Threshold", Range(0, 2)) = 0.5
        _FoamSoftness("Foam Softness", Range(0, 1)) = 0.1
        
        [Header(Waves)]
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveHeight("Wave Height", Range(0, 1)) = 0.1
        _WaveFrequency("Wave Frequency", Float) = 1.0
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 1)) = 0.5
        _NormalTiling("Normal Tiling", Float) = 1.0
        
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
        
        // Internal
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5 
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            #include "Assets/_project/Shaders/Watercolor/Core/WatercolorCore.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
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
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DeepColor;
                float _DepthFactor;
                float4 _FoamColor;
                float _FoamThreshold;
                float _FoamSoftness;
                
                float _WaveSpeed;
                float _WaveHeight;
                float _WaveFrequency;
                float _NormalScale;
                float _NormalTiling;
                
                float _LayerBlend;
                float _PaperTiling;
                float _PaperStrength;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);
            
            TEXTURE2D(_RampLightingA); SAMPLER(sampler_RampLightingA);
            TEXTURE2D(_RampLightingB); SAMPLER(sampler_RampLightingB);
            TEXTURE2D(_RampEdgeA); SAMPLER(sampler_RampEdgeA);
            TEXTURE2D(_RampEdgeB); SAMPLER(sampler_RampEdgeB);
            TEXTURE2D(_RampEdgeCol); SAMPLER(sampler_RampEdgeCol);

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float wave = sin(_Time.y * _WaveSpeed + worldPos.x * _WaveFrequency) 
                           + cos(_Time.y * _WaveSpeed * 0.8 + worldPos.z * _WaveFrequency);
                input.positionOS.y += wave * _WaveHeight;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(input.screenPos.z / input.screenPos.w, _ZBufferParams);
                float depthDiff = sceneDepth - surfaceDepth;
                
                float depthAlpha = saturate(depthDiff * _DepthFactor);
                float3 waterBase = lerp(_BaseColor.rgb, _DeepColor.rgb, depthAlpha);
                float alpha = lerp(_BaseColor.a, _DeepColor.a, depthAlpha);
                
                float2 uv1 = input.uv * _NormalTiling + _Time.y * _WaveSpeed * 0.05;
                float2 uv2 = input.uv * _NormalTiling * 0.7 - _Time.y * _WaveSpeed * 0.03;
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1), _NormalScale);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2), _NormalScale);
                float3 blendNormal = normalize(float3(n1.xy + n2.xy, n1.z)); 
                float3 normalWS = normalize(input.normalWS + blendNormal.xzy * _NormalScale);

                float foamFactor = 1.0 - saturate(depthDiff / _FoamThreshold);
                foamFactor = smoothstep(0.0, _FoamSoftness, foamFactor);
                
                // Watercolor Lighting (Complex)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 wcColor;
                float3 viewDirWS = SafeNormalize(input.viewDirWS);
                
                // 물은 왜곡(Distortion)을 노멀맵으로 이미 했으므로, 추가 Vertex 왜곡은 생략하거나 약하게 적용
                WatercolorLightingComplex_float(
                    waterBase,
                    normalWS,
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
                
                wcColor = lerp(wcColor, _FoamColor.rgb, foamFactor);
                
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
                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
