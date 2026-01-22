Shader "Hidden/WatercolorBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _DistortionScale ("Distortion Scale", Float) = 1.0
        _DistortionStrength ("Distortion Strength", Float) = 0.02
        _ColorLevels ("Color Levels", Float) = 8.0
        _EdgeStrength ("Edge Strength", Float) = 1.0
        _EdgeColor ("Edge Color", Color) = (0.2, 0.2, 0.2, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Watercolor"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            float _DistortionScale;
            float _DistortionStrength;
            float _ColorLevels;
            float _EdgeStrength;
            float4 _EdgeColor;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Distortion
                float2 noiseUV = input.uv * _DistortionScale;
                float4 noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV);
                float2 distortion = (noise.rg - 0.5) * _DistortionStrength;
                float2 distortedUV = input.uv + distortion;

                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // 2. Quantization (Posterization)
                color.rgb = floor(color.rgb * _ColorLevels) / _ColorLevels;

                // 3. Simple Edge Detection (Sobel-ish)
                // Sample neighbors for edge detection
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float4 c0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(-1, -1) * texelSize);
                float4 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2( 1,  1) * texelSize);
                float4 c2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2( 1, -1) * texelSize);
                float4 c3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV + float2(-1,  1) * texelSize);
                
                float edge = length(c0.rgb - c1.rgb) + length(c2.rgb - c3.rgb);
                
                // Apply edge
                color.rgb = lerp(color.rgb, _EdgeColor.rgb, saturate(edge * _EdgeStrength));

                return color;
            }
            ENDHLSL
        }
    }
}
