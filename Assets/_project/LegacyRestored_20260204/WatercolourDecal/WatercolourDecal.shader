Shader "Custom/WatercolourDecal"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _NoiseMap ("Noise Texture", 2D) = "gray" {}
        _NoiseStrength ("Noise Strength", Float) = 0.1
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"="True"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Decal"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NoiseMap_ST;
                float4 _BaseColor;
                float _NoiseStrength;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample Noise
                float2 noiseUV = TRANSFORM_TEX(input.uv, _NoiseMap);
                half4 noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV);
                
                // Distort UVs
                float2 distortedUV = input.uv + (noise.rg - 0.5) * _NoiseStrength;
                
                // Sample Base Map
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, distortedUV) * _BaseColor;
                
                // Alpha Clip
                clip(color.a - _Cutoff);
                
                // Apply Fog
                MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
    }
}
