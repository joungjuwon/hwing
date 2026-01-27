Shader "Custom/Test/KuwaharaTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _BlitTexture ("Blit Texture", 2D) = "white" {}
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
        
        TEXTURE2D(_StructureTensorTex);
        SAMPLER(sampler_StructureTensorTex);
        
        TEXTURE2D(_BlendTex);
        SAMPLER(sampler_BlendTex);
        
        float _KuwaharaSize;
        float _TensorSpread;
        float _Anisotropy;
        float _Sharpness;
        float _BlendFactor;
        float _WeightH;
        float _WeightV;
        
        #define PI 3.14159265359
        ENDHLSL
        
        // Pass 0: Structure Tensor
        Pass
        {
            Name "StructureTensor"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            half4 Frag(Varyings input) : SV_Target
            {
                float2 ts = _BlitTexture_TexelSize.xy;
                float2 uv = input.texcoord;
                
                half3 u = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(0, ts.y))).rgb;
                half3 d = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(uv - float2(0, ts.y))).rgb;
                half3 l = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(uv - float2(ts.x, 0))).rgb;
                half3 r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(uv + float2(ts.x, 0))).rgb;
                
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
        
        // Pass 1: Blur H
        Pass
        {
            Name "BlurH"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            half4 Frag(Varyings input) : SV_Target
            {
                float2 ts = _BlitTexture_TexelSize.xy;
                float spread = max(0.1, _TensorSpread);
                static const float w[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
                
                half4 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord) * w[0];
                for (int i = 1; i < 5; i++)
                {
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(input.texcoord + float2(ts.x * spread * i, 0))) * w[i];
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(input.texcoord - float2(ts.x * spread * i, 0))) * w[i];
                }
                return result;
            }
            ENDHLSL
        }
        
        // Pass 2: Blur V
        Pass
        {
            Name "BlurV"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            half4 Frag(Varyings input) : SV_Target
            {
                float2 ts = _BlitTexture_TexelSize.xy;
                float spread = max(0.1, _TensorSpread);
                static const float w[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
                
                half4 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord) * w[0];
                for (int i = 1; i < 5; i++)
                {
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(input.texcoord + float2(0, ts.y * spread * i))) * w[i];
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(input.texcoord - float2(0, ts.y * spread * i))) * w[i];
                }
                return result;
            }
            ENDHLSL
        }

        // Pass 3: Kuwahara
        Pass
        {
            Name "Kuwahara"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            half4 Frag(Varyings input) : SV_Target
            {
                float2 ts = _BlitTexture_TexelSize.xy;
                int radius = max(2, (int)_KuwaharaSize);
                float2 uv = input.texcoord;
                
                half3 t = SAMPLE_TEXTURE2D(_StructureTensorTex, sampler_StructureTensorTex, uv).xyz;
                
                float lambda1 = 0.5 * (t.x + t.y + sqrt((t.x - t.y) * (t.x - t.y) + 4.0 * t.z * t.z));
                float lambda2 = 0.5 * (t.x + t.y - sqrt((t.x - t.y) * (t.x - t.y) + 4.0 * t.z * t.z));
                
                float aniso = (lambda1 + lambda2) > 0.0 ? (lambda1 - lambda2) / (lambda1 + lambda2) : 0.0;
                aniso = saturate(aniso * max(0.01, _Anisotropy));
                
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
                        // Clamp sampling UV
                        half3 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, saturate(uv + ofs * ts)).rgb;
                        
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
                
                // Fallback: If total weight is too small (no valid sectors found), use original pixel
                if (totalWeight < 0.0001)
                {
                    return half4(SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb, 1.0);
                }
                
                return half4(finalColor / totalWeight, 1.0);
            }
            ENDHLSL
        }
        
        // Pass 4: Blend (Layer 2 over Layer 1)
        Pass
        {
            Name "Blend"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            float4 _Layer2Offset;
            
            half4 Frag(Varyings input) : SV_Target
            {
                // Layer 1 is the Base (Fine details)
                half4 layer1 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                
                // Layer 2 is the Overlay (Coarse shapes)
                // Apply Offset: Convert pixel offset to UV space
                float2 offsetUV = _Layer2Offset.xy * _BlitTexture_TexelSize.xy;
                half4 layer2 = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, input.texcoord + offsetUV);
                
                // Standard Alpha Blending: Layer 1 * (1 - alpha) + Layer 2 * alpha
                // _BlendFactor controls the Opacity of Layer 2
                float opacity = saturate(_BlendFactor);
                
                return lerp(layer1, layer2, opacity);
            }
            ENDHLSL
        }
    }
}
