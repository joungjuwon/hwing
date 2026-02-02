Shader "Watercolor/URP/WatercolorLit"
{
    Properties
    {
        [Header(Paper)]
        _PaperTex("Paper Texture", 2D) = "white" {}
        _PaperST("Paper ST (xy tiling, zw offset)", Vector) = (2,2,0,0)
        _PaperStrength("Paper Visibility", Range(0,1)) = 0.3

        [Header(Noise and Distortion)]
        _NoiseTex("Noise Texture", 2D) = "gray" {}
        _NoiseTiling("Noise Tiling", Float) = 1.0
        _DistortStrength("Distort Strength", Range(0,0.5)) = 0.08

        [Header(Shading)]
        _BaseColor("Base Color", Color) = (0.9, 0.7, 0.7, 1)
        _ShadowColor("Shadow Color", Color) = (0.6, 0.3, 0.4, 1)
        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSoftness("Shadow Softness", Range(0.01,0.5)) = 0.15
        _Ambient("Ambient", Range(0,1)) = 0.15

        [Header(Edge Darkening)]
        _EdgeColor("Edge Tint", Color) = (0.4, 0.2, 0.3, 1)
        _EdgePower("Edge Power", Range(0.1,5)) = 2.0
        _EdgeStrength("Edge Strength", Range(0,1)) = 0.3

        [Header(Pigment Effect)]
        _PigmentSpread("Pigment Spread", Range(0,1)) = 0.5
        _PigmentDarkening("Pigment Edge Darkening", Range(0,0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _PaperST;
                float _PaperStrength;

                float _NoiseTiling, _DistortStrength;

                float4 _BaseColor, _ShadowColor, _EdgeColor;
                float _ShadowThreshold, _ShadowSoftness, _Ambient;
                float _EdgePower, _EdgeStrength;
                float _PigmentSpread, _PigmentDarkening;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                #ifdef _MAIN_LIGHT_SHADOWS
                float4 shadowCoord : TEXCOORD4;
                #endif
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrmInputs = GetVertexNormalInputs(input.normalOS);

                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS   = normalize(nrmInputs.normalWS);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.uv = input.uv;

                #ifdef _MAIN_LIGHT_SHADOWS
                o.shadowCoord = TransformWorldToShadowCoord(o.positionWS);
                #endif
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // ===== Paper Texture (Screen-Fixed) =====
                float2 paperUV = screenUV * _PaperST.xy + _PaperST.zw;
                float3 paperTex = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, paperUV).rgb;
                // Subtle paper: mostly white with slight texture variation
                float paperValue = lerp(1.0, dot(paperTex, float3(0.299, 0.587, 0.114)), _PaperStrength);

                // ===== Noise for Organic Edges =====
                float2 noiseUV = i.uv * _NoiseTiling;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                float noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV * 1.7 + 0.3).g;
                
                // Gentle normal perturbation
                float3 normalWS = normalize(i.normalWS + float3(noise - 0.5, noise2 - 0.5, 0) * _DistortStrength * 0.5);
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);

                // ===== Lighting =====
                #ifdef _MAIN_LIGHT_SHADOWS
                    Light L = GetMainLight(i.shadowCoord);
                #else
                    Light L = GetMainLight();
                #endif

                float ndl = dot(normalWS, L.direction) * 0.5 + 0.5; // Remap to 0-1
                float shadowAtten = L.shadowAttenuation;
                float lightIntensity = ndl * shadowAtten;
                
                // Add noise to lighting for organic watercolor feel
                float distortedLight = lightIntensity + (noise - 0.5) * _DistortStrength;
                
                // Soft shadow transition (no harsh bands)
                float shadowMask = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, distortedLight);
                shadowMask = saturate(shadowMask + _Ambient);

                // ===== Base Color Blend =====
                float3 baseColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, shadowMask);

                // ===== Edge Darkening (Soft Fresnel) =====
                float fresnel = 1.0 - saturate(dot(i.normalWS, viewDirWS));
                fresnel = pow(fresnel, _EdgePower);
                // Add noise to edge for painterly effect
                fresnel = saturate(fresnel + (noise2 - 0.5) * _DistortStrength);
                
                float3 edgeTint = lerp(float3(1,1,1), _EdgeColor.rgb, fresnel * _EdgeStrength);
                baseColor *= edgeTint;

                // ===== Pigment Effect (Darker at edges of color regions) =====
                // Simulates watercolor pigment pooling at edges
                float pigmentNoise = abs(noise - 0.5) * 2.0; // 0 at center, 1 at edges
                float pigmentDark = 1.0 - pigmentNoise * _PigmentDarkening * (1.0 - shadowMask);
                baseColor *= pigmentDark;

                // ===== Combine with Paper =====
                // Watercolor effect: paint sits on paper, paper shows through in highlights
                float3 finalColor = baseColor * paperValue;
                
                // Slight paper texture bleed-through effect
                finalColor = lerp(finalColor, finalColor * paperTex, _PaperStrength * 0.3);

                return float4(saturate(finalColor), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
