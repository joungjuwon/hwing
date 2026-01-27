Shader "Custom/PostProcess/WatercolorBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
        _KuwaharaSize ("Kuwahara Kernel Size", Float) = 3.0
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
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize;
        float _BlurSize;
        float _KuwaharaSize;
        
        // Kuwahara improvements
        float _WeightH;
        float _WeightV;
        float _AnisotropyStrength;
        float _Sharpness;
        
        // Structure Tensor texture
        TEXTURE2D(_StructureTensorTex);
        SAMPLER(sampler_StructureTensorTex);
        
        // Global texture for composite pass
        TEXTURE2D(_BlurredTex);
        SAMPLER(sampler_BlurredTex);
        float _BleedingIntensity;
        float _EdgeSensitivity;
        float _UseEdgeDetection;
        
        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.texcoord = input.uv;
            return output;
        }
        
        // 9-tap Gaussian weights
        static const float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
        
        ENDHLSL
        
        // Pass 0: Horizontal Blur
        Pass
        {
            Name "BlurHorizontal"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragH
            
            half4 FragH(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float2 offset = float2(texelSize.x * _BlurSize, 0);
                
                half4 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord) * weights[0];
                
                for (int i = 1; i < 5; i++)
                {
                    result += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord + offset * i) * weights[i];
                    result += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord - offset * i) * weights[i];
                }
                
                return result;
            }
            ENDHLSL
        }
        
        // Pass 1: Vertical Blur
        Pass
        {
            Name "BlurVertical"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragV
            
            half4 FragV(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float2 offset = float2(0, texelSize.y * _BlurSize);
                
                half4 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord) * weights[0];
                
                for (int i = 1; i < 5; i++)
                {
                    result += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord + offset * i) * weights[i];
                    result += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord - offset * i) * weights[i];
                }
                
                return result;
            }
            ENDHLSL
        }
        
        // Pass 2: Edge-aware Composite
        Pass
        {
            Name "Composite"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            
            float DetectEdge(float2 uv)
            {
                float2 texelSize = _MainTex_TexelSize.xy * 2.0;
                
                half3 colorCenter = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                half3 colorLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize.x, 0)).rgb;
                half3 colorRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize.x, 0)).rgb;
                half3 colorUp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texelSize.y)).rgb;
                half3 colorDown = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, texelSize.y)).rgb;
                
                float edge = length(colorCenter - colorLeft) + length(colorCenter - colorRight) +
                             length(colorCenter - colorUp) + length(colorCenter - colorDown);
                
                return saturate(edge / _EdgeSensitivity);
            }
            
            half4 FragComposite(Varyings input) : SV_Target
            {
                half4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
                half4 blurred = SAMPLE_TEXTURE2D(_BlurredTex, sampler_BlurredTex, input.texcoord);
                
                float blendFactor = _BleedingIntensity;
                
                if (_UseEdgeDetection > 0.5)
                {
                    float edge = DetectEdge(input.texcoord);
                    blendFactor = edge * _BleedingIntensity;
                }
                
                return lerp(original, blurred, blendFactor);
            }
            ENDHLSL
        }
        
        // Pass 3: Structure Tensor
        Pass
        {
            Name "StructureTensor"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragStructureTensor
            
            half4 FragStructureTensor(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                float2 uv = input.texcoord;
                
                half3 u = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2(0, texelSize.y))).rgb;
                half3 d = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv - float2(0, texelSize.y))).rgb;
                half3 l = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv - float2(texelSize.x, 0))).rgb;
                half3 r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + float2(texelSize.x, 0))).rgb;
                
                half wH = max(0.01, _WeightH);
                half wV = max(0.01, _WeightV);
                
                half3 dx = (r - l) * 0.5 * wH;
                half3 dy = (u - d) * 0.5 * wV;
                
                half fxx = dot(dx, dx);
                half fyy = dot(dy, dy);
                half fxy = dot(dx, dy);
                
                return half4(fxx, fyy, fxy, 1.0);
            }
            ENDHLSL
        }

        // Pass 4: Anisotropic Kuwahara
        Pass
        {
            Name "AnisotropicKuwahara"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragAnisotropicKuwahara
            
            #ifndef PI
            #define PI 3.14159265359
            #endif
            
            half4 FragAnisotropicKuwahara(Varyings input) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                int radius = max(2, (int)_KuwaharaSize);
                float2 uv = input.texcoord;
                
                half3 t = SAMPLE_TEXTURE2D(_StructureTensorTex, sampler_StructureTensorTex, uv).xyz;
                
                float lambda1 = 0.5 * (t.x + t.y + sqrt((t.x - t.y) * (t.x - t.y) + 4.0 * t.z * t.z));
                float lambda2 = 0.5 * (t.x + t.y - sqrt((t.x - t.y) * (t.x - t.y) + 4.0 * t.z * t.z));
                
                float aniso = (lambda1 + lambda2) > 0.0 ? (lambda1 - lambda2) / (lambda1 + lambda2) : 0.0;
                aniso = saturate(aniso * max(0.01, _AnisotropyStrength));
                
                float phi = 0.5 * atan2(2.0 * t.z, t.x - t.y);
                float c = cos(phi);
                float s = sin(phi);
                float2x2 rot = float2x2(c, -s, s, c);
                
                float4 mean[8];
                float4 variance[8];
                for(int k = 0; k < 8; k++) { mean[k] = 0; variance[k] = 0; }
                
                float A = 1.0 + aniso;
                
                for (int yy = -radius; yy <= radius; yy++)
                {
                    for (int xx = -radius; xx <= radius; xx++)
                    {
                        float2 ofs = float2(xx, yy);
                        float r2 = dot(ofs, ofs);
                        if (r2 > radius * radius) continue;
                        
                        float2 v = mul(rot, ofs);
                        half3 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uv + ofs * texelSize)).rgb;
                        
                        float ang = atan2(v.y, v.x) + PI;
                        int sector = int(floor(ang / (2.0 * PI) * 8.0)) % 8;
                        
                        float sX = max(0.01, (radius * 0.5) * A);
                        float sY = max(0.01, (radius * 0.5) / A);
                        float wx = exp(-(v.x * v.x) / (2.0 * sX * sX));
                        float wy = exp(-(v.y * v.y) / (2.0 * sY * sY));
                        float ww = wx * wy;
                        
                        mean[sector].rgb += col * ww;
                        mean[sector].w += ww;
                        variance[sector].rgb += col * col * ww;
                        variance[sector].w += ww;
                    }
                }
                
                half3 finalColor = 0;
                float totalWeight = 0;
                float q = max(1.0, _Sharpness);
                
                for (int i = 0; i < 8; i++)
                {
                    if (mean[i].w > 0.0001)
                    {
                        mean[i].rgb /= mean[i].w;
                        variance[i].rgb = abs(variance[i].rgb / mean[i].w - mean[i].rgb * mean[i].rgb);
                        float sig2 = variance[i].r + variance[i].g + variance[i].b;
                        float ww = 1.0 / pow(1.0 + sig2, q);
                        finalColor += mean[i].rgb * ww;
                        totalWeight += ww;
                    }
                }
                
                if (totalWeight < 0.0001)
                {
                    return half4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb, 1.0);
                }
                
                return half4(finalColor / totalWeight, 1.0);
            }
            ENDHLSL
        }
        
        // Pass 5: Blend Layers
        Pass
        {
            Name "BlendLayers"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlend
            
            TEXTURE2D(_Layer2Tex);
            SAMPLER(sampler_Layer2Tex);
            float _BlendFactor;
            float4 _Layer2Offset;
            
            half4 FragBlend(Varyings input) : SV_Target
            {
                half4 layer1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
                
                float2 offsetUV = _Layer2Offset.xy * _MainTex_TexelSize.xy;
                half4 layer2 = SAMPLE_TEXTURE2D(_Layer2Tex, sampler_Layer2Tex, input.texcoord + offsetUV);
                
                float opacity = saturate(_BlendFactor);
                
                return lerp(layer1, layer2, opacity);
            }
            ENDHLSL
        }
    }
}
