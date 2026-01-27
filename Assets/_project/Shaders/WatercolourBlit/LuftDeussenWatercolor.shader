Shader "Custom/PostProcess/LuftDeussenWatercolor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        [Header(Textures)]
        _PaperTex ("Paper Texture (Granulation)", 2D) = "gray" {}
        _TurbulenceTex ("Turbulence (Low Freq)", 2D) = "gray" {}
        _Dispersal1Tex ("Dispersal 1 (Mid Freq)", 2D) = "gray" {}
        _Dispersal2Tex ("Dispersal 2 (High Freq)", 2D) = "gray" {}
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        
        TEXTURE2D(_PaperTex);
        SAMPLER(sampler_PaperTex);
        
        TEXTURE2D(_TurbulenceTex);
        SAMPLER(sampler_TurbulenceTex);
        
        TEXTURE2D(_Dispersal1Tex);
        SAMPLER(sampler_Dispersal1Tex);
        
        TEXTURE2D(_Dispersal2Tex);
        SAMPLER(sampler_Dispersal2Tex);
        
        TEXTURE2D(_DensityTex);
        SAMPLER(sampler_DensityTex);
        
        float _EdgeThreshold;
        float _EdgeDarkening;
        float _DensityBase;
        float _DensityContrast;
        float _PaperStrength;
        float _TurbulenceStrength;
        float _Dispersal1Strength;
        float _Dispersal2Strength;
        float _TextureScale;
        float _BlurSize;
        
        // Wobble and Quantization
        float _WobbleStrength;
        float _ColorSteps;
        float _EnableQuantization;
        
        float Luminance(half3 color)
        {
            return dot(color, half3(0.299, 0.587, 0.114));
        }
        
        half3 ApplyPigmentDensity(half3 color, float density)
        {
            color = saturate(color);
            if (density > 1.0)
            {
                half3 oneMinusC = 1.0 - color;
                half3 factor = pow(oneMinusC, density - 1.0);
                return color * (1.0 - factor);
            }
            else
            {
                return pow(color, density);
            }
        }
        
        // Custom HSV functions (renamed to avoid conflict with Blit.hlsl)
        float3 WatercolorRgbToHsv(float3 c)
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
            float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        float3 WatercolorHsvToRgb(float3 c)
        {
            float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
            return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
        }
        
        static const float gaussWeights7[4] = { 0.2270, 0.1945, 0.1216, 0.0540 };
        
        ENDHLSL
        
        // Pass 0: Edge Detection & Density Map
        Pass
        {
            Name "EdgeDetection"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragEdge
            
            half4 FragEdge(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.xy;
                
                half3 left   = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(texel.x, 0)).rgb;
                half3 right  = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(texel.x, 0)).rgb;
                half3 top    = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(0, texel.y)).rgb;
                half3 bottom = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(0, texel.y)).rgb;
                
                float gradX = Luminance(right) - Luminance(left);
                float gradY = Luminance(top) - Luminance(bottom);
                float gradient = sqrt(gradX * gradX + gradY * gradY);
                
                float edgeFactor = saturate(gradient * _EdgeThreshold);
                
                float2 noiseUV = uv * _TextureScale;
                float turbulence = SAMPLE_TEXTURE2D(_TurbulenceTex, sampler_TurbulenceTex, noiseUV).r - 0.5;
                float dispersal1 = SAMPLE_TEXTURE2D(_Dispersal1Tex, sampler_Dispersal1Tex, noiseUV * 2.0).r - 0.5;
                float dispersal2 = SAMPLE_TEXTURE2D(_Dispersal2Tex, sampler_Dispersal2Tex, noiseUV * 4.0).r - 0.5;
                
                float fBm = turbulence * _TurbulenceStrength * 1.0
                          + dispersal1 * _Dispersal1Strength * 0.5
                          + dispersal2 * _Dispersal2Strength * 0.25;
                
                float density = _DensityBase 
                              + edgeFactor * _EdgeDarkening 
                              + fBm * _DensityContrast;
                
                return half4(density, edgeFactor, 0, 1);
            }
            ENDHLSL
        }
        
        // Pass 1: Color Modification (with Wobble & Quantization)
        Pass
        {
            Name "ColorModification"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragColor
            
            half4 FragColor(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // 1. UV Wobble (Hand-drawn distortion)
                // Use Turbulence texture to distort UVs
                float2 noiseUV = uv * _TextureScale;
                float2 wobble = SAMPLE_TEXTURE2D(_TurbulenceTex, sampler_TurbulenceTex, noiseUV).rg - 0.5;
                float2 distortedUV = uv + wobble * _WobbleStrength * 0.01; // Scale down effect
                
                // Clamp UVs to prevent bleeding
                distortedUV = clamp(distortedUV, 0.001, 0.999);
                
                // 2. Sample Color & Density with Distorted UV
                half4 originalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, distortedUV);
                half2 densityData = SAMPLE_TEXTURE2D(_DensityTex, sampler_DensityTex, distortedUV).rg;
                float density = densityData.r;
                
                // 3. Color Quantization (Toon effect) - Controlled by parameters
                if (_EnableQuantization > 0.5)
                {
                    float3 hsv = WatercolorRgbToHsv(originalColor.rgb);
                    hsv.z = floor(hsv.z * _ColorSteps) / _ColorSteps;
                    originalColor.rgb = WatercolorHsvToRgb(hsv);
                }
                
                // 4. Apply Pigment Density
                half3 modifiedColor = ApplyPigmentDensity(originalColor.rgb, density);
                
                // 5. Paper Blend
                float2 paperUV = uv * _TextureScale * 2.0;
                float paper = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, paperUV).r;
                paper = lerp(1.0, paper, _PaperStrength);
                
                // Multiply paper, but keep highlights bright
                modifiedColor *= paper;
                
                return half4(modifiedColor, 1.0);
            }
            ENDHLSL
        }
        
        // Pass 2: Blur Horizontal
        Pass
        {
            Name "BlurHorizontal"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurH
            
            half4 FragBlurH(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 offset = float2(_BlitTexture_TexelSize.x * _BlurSize, 0);
                
                half4 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * gaussWeights7[0];
                
                for (int i = 1; i < 4; i++)
                {
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset * i) * gaussWeights7[i];
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset * i) * gaussWeights7[i];
                }
                
                return result;
            }
            ENDHLSL
        }
        
        // Pass 3: Blur Vertical
        Pass
        {
            Name "BlurVertical"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurV
            
            half4 FragBlurV(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 offset = float2(0, _BlitTexture_TexelSize.y * _BlurSize);
                
                half4 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * gaussWeights7[0];
                
                for (int i = 1; i < 4; i++)
                {
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset * i) * gaussWeights7[i];
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset * i) * gaussWeights7[i];
                }
                
                return result;
            }
            ENDHLSL
        }
        
        // Pass 4: Composite
        Pass
        {
            Name "Composite"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            
            half4 FragComposite(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 watercolor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                half2 densityData = SAMPLE_TEXTURE2D(_DensityTex, sampler_DensityTex, uv).rg;
                float edgeFactor = densityData.g;
                
                watercolor.rgb *= lerp(1.0, 0.85, edgeFactor * _EdgeDarkening * 0.5);
                
                return watercolor;
            }
            ENDHLSL
        }
    }
}
