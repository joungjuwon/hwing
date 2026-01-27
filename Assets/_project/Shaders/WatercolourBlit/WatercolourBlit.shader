Shader "Custom/WatercolourBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseMap ("Noise Texture", 2D) = "gray" {}
        _PaperMap ("Paper Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Float) = 0.02
        _PaperStrength ("Paper Strength", Float) = 0.5
        
        [Header(Granulation)]
        _GranulationStrength ("Granulation Strength", Range(0, 1)) = 0.5
        
        [Header(Wet Edge)]
        _WetEdgeStrength ("Wet Edge Strength", Range(0, 2)) = 1.0
        _WetEdgeSize ("Wet Edge Size", Range(0.5, 3.0)) = 1.0
        _WetEdgeColor ("Wet Edge Color", Color) = (0.3, 0.2, 0.1, 1)
        [Toggle] _UseCustomEdgeColor ("Use Custom Edge Color", Float) = 0
        
        [Header(Edge Detection)]
        _EdgeColor("Edge Color", Color) = (0, 0, 0, 1)
        _EdgeThreshold("Edge Threshold", Range(0.01, 1)) = 0.1
        _EdgeThickness("Edge Thickness", Range(0.1, 5)) = 1.0
        [Toggle] _DebugEdges("Debug Edges", Float) = 0
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
            Name "WatercolourBlit"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.uv;
                return output;
            }
            
            // Declare _MainTex (provided by cmd.Blit)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_PaperMap);
            SAMPLER(sampler_PaperMap);

            float _NoiseStrength;
            float _PaperStrength;
            float4 _NoiseMap_ST;
            float4 _PaperMap_ST;
            
            float _GranulationStrength;
            float _WetEdgeStrength;
            float _WetEdgeSize;
            float4 _WetEdgeColor;
            float _UseCustomEdgeColor;

            // Simple edge detection for wet edges
            float DetectColorEdge(float2 uv)
            {
                float2 texelSize = _MainTex_TexelSize.xy * _WetEdgeSize;
                
                half3 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                half3 l = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize.x, 0)).rgb;
                half3 r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, 0)).rgb;
                half3 u = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texelSize.y)).rgb;
                half3 d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, texelSize.y)).rgb;
                
                float edge = length(c - l) + length(c - r) + length(c - u) + length(c - d);
                return saturate(edge);
            }

            half4 Frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Calculate UVs
                float2 uv = input.texcoord;
                
                // Sample Noise
                float2 noiseUV = TRANSFORM_TEX(uv, _NoiseMap);
                half4 noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV);
                
                // Distort UVs with noise
                float2 distortedUV = uv + (noise.rg - 0.5) * _NoiseStrength;
                
                // Sample Main Texture (Screen)
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                
                // Sample Paper Texture overlay
                float2 paperUV = TRANSFORM_TEX(uv, _PaperMap);
                half4 paper = SAMPLE_TEXTURE2D(_PaperMap, sampler_PaperMap, paperUV);
                
                // --- Granulation Effect ---
                // Darken color where paper texture is dark (valleys), simulating pigment settling
                // Paper texture is usually white, so we invert it or use 1-paper for "depth"
                float paperDepth = 1.0 - paper.r; 
                // Only darken, don't lighten
                half3 granulation = color.rgb * (1.0 - paperDepth * _GranulationStrength);
                color.rgb = lerp(color.rgb, granulation, 0.5); // Blend a bit
                
                // --- Wet Edge Effect (Coffee Ring) ---
                float edge = DetectColorEdge(distortedUV);
                
                half3 wetEdgeResult;
                if (_UseCustomEdgeColor > 0.5)
                {
                    // Use custom color
                    wetEdgeResult = _WetEdgeColor.rgb;
                    // Blend based on edge strength
                    color.rgb = lerp(color.rgb, wetEdgeResult, edge * _WetEdgeStrength);
                }
                else
                {
                    // Darken existing color (original behavior)
                    wetEdgeResult = color.rgb * (1.0 - _WetEdgeStrength * 0.5);
                    color.rgb = lerp(color.rgb, wetEdgeResult, edge);
                }

                // Apply Paper Blend (Overlay)
                color.rgb = lerp(color.rgb, color.rgb * paper.rgb, _PaperStrength);
                
                return color;
            }
            ENDHLSL
        }
    }
}
