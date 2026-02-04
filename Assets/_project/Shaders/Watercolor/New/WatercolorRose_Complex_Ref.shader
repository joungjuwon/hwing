Shader "Custom/WatercolorRoseURP_Reference"
{
    Properties
    {
        _PaperTex("Paper Tex (sRGB)", 2D) = "white" {}
        
        // Ramp LUTs (1D textures, linear)
        _RampLightingA("Ramp ColorRamp.004", 2D) = "white" {}
        _RampLightingB("Ramp ColorRamp.005", 2D) = "white" {}
        _RampEdgeA("Ramp ColorRamp", 2D) = "white" {}
        _RampEdgeB("Ramp ColorRamp.006", 2D) = "white" {}
        _RampEdgeColor("Ramp ColorRamp.008", 2D) = "white" {}
        
        // RGB Curves (combined curve LUT, 1D texture, linear)
        _CurveC("CurveC (RGB Curves Combined)", 2D) = "gray" {}
        
        _BoundsMin("Bounds Min (OS)", Vector) = (0,0,0,0)
        _BoundsSize("Bounds Size (OS)", Vector) = (1,1,1,0)
        
        _PaperOffset("Paper Offset", Vector) = (0.61,0,0,0)
        _PaperScale("Paper Scale", Vector) = (2,2,1,0)
        
        _Hue("Hue (0.5 = no shift)", Range(0,1)) = 0.5
        _Saturation("Saturation", Range(0,2)) = 0.1
        _Value("Value", Range(0,2)) = 1.0
        
        _NoiseStrength("Noise Strength", Float) = 0.7
        
        // Noise params: (Scale, Detail, Roughness, Lacunarity, Distortion)
        _NoiseScale0("Noise0 Scale", Float) = 3.66
        _NoiseDetail0("Noise0 Detail", Float) = 16.0
        _NoiseRoughness0("Noise0 Roughness", Float) = 0.5
        _NoiseLacunarity0("Noise0 Lacunarity", Float) = 2.0
        _NoiseDistortion0("Noise0 Distortion", Float) = 2.41
        
        _NoiseScale1("Noise1 Scale", Float) = 5.9
        _NoiseDetail1("Noise1 Detail", Float) = 2.0
        _NoiseRoughness1("Noise1 Roughness", Float) = 0.5
        _NoiseLacunarity1("Noise1 Lacunarity", Float) = 2.0
        _NoiseDistortion1("Noise1 Distortion", Float) = 2.50
        
        _NoiseScale2("Noise2 Scale", Float) = 8.5
        _NoiseDetail2("Noise2 Detail", Float) = 2.0
        _NoiseRoughness2("Noise2 Roughness", Float) = 0.5
        _NoiseLacunarity2("Noise2 Lacunarity", Float) = 2.0
        _NoiseDistortion2("Noise2 Distortion", Float) = 2.14
        
        _LayerBlend("Layer Weight Blend", Float) = 0.2
    }
    
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            
            Cull Back
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
            float4 _BoundsMin;
            float4 _BoundsSize;
            float4 _PaperOffset;
            float4 _PaperScale;
            
            float _Hue;
            float _Saturation;
            float _Value;
            
            float _NoiseStrength;
            float _NoiseScale0, _NoiseDetail0, _NoiseRoughness0, _NoiseLacunarity0, _NoiseDistortion0;
            float _NoiseScale1, _NoiseDetail1, _NoiseRoughness1, _NoiseLacunarity1, _NoiseDistortion1;
            float _NoiseScale2, _NoiseDetail2, _NoiseRoughness2, _NoiseLacunarity2, _NoiseDistortion2;
            
            float _LayerBlend;
            CBUFFER_END
            
            TEXTURE2D(_PaperTex);          SAMPLER(sampler_PaperTex);
            TEXTURE2D(_RampLightingA);     SAMPLER(sampler_RampLightingA);
            TEXTURE2D(_RampLightingB);     SAMPLER(sampler_RampLightingB);
            TEXTURE2D(_RampEdgeA);         SAMPLER(sampler_RampEdgeA);
            TEXTURE2D(_RampEdgeB);         SAMPLER(sampler_RampEdgeB);
            TEXTURE2D(_RampEdgeColor);     SAMPLER(sampler_RampEdgeColor);
            TEXTURE2D(_CurveC);            SAMPLER(sampler_CurveC);
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
            };
            
            float rgb2bw(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }
            
            float3 SampleRamp(TEXTURE2D_PARAM(tex, samp), float t)
            {
                return SAMPLE_TEXTURE2D(tex, samp, float2(saturate(t), 0.5)).rgb;
            }
            
            float ApplyCurveC(float v)
            {
                return SAMPLE_TEXTURE2D(_CurveC, sampler_CurveC, float2(saturate(v), 0.5)).r;
            }
            
            float3 ApplyCurveC(float3 c)
            {
                return float3(ApplyCurveC(c.r), ApplyCurveC(c.g), ApplyCurveC(c.b));
            }
            
            float3 RGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            
            float3 HSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }
            
            float3 ApplyHSV(float3 rgb, float hue, float sat, float val)
            {
                float3 hsv = RGBToHSV(rgb);
                hsv.x = frac(hsv.x + (hue - 0.5));
                hsv.y *= sat;
                hsv.z *= val;
                return HSVToRGB(hsv);
            }
            
            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                
                float n000 = Hash(i + float3(0,0,0));
                float n100 = Hash(i + float3(1,0,0));
                float n010 = Hash(i + float3(0,1,0));
                float n110 = Hash(i + float3(1,1,0));
                float n001 = Hash(i + float3(0,0,1));
                float n101 = Hash(i + float3(1,0,1));
                float n011 = Hash(i + float3(0,1,1));
                float n111 = Hash(i + float3(1,1,1));
                
                float n00 = lerp(n000, n100, u.x);
                float n10 = lerp(n010, n110, u.x);
                float n01 = lerp(n001, n101, u.x);
                float n11 = lerp(n011, n111, u.x);
                float n0 = lerp(n00, n10, u.y);
                float n1 = lerp(n01, n11, u.y);
                return lerp(n0, n1, u.z);
            }
            
            float BlenderNoiseApprox(float3 p, float scale, float detail, float roughness, float lacunarity, float distortion)
            {
                float3 dp = p + distortion * (ValueNoise(p * 1.7) - 0.5);
                float freq = scale;
                float amp = 1.0;
                float sum = 0.0;
                for (int i = 0; i < 8; i++)
                {
                    if (i >= (int)detail) break;
                    sum += ValueNoise(dp * freq) * amp;
                    freq *= lacunarity;
                    amp *= roughness;
                }
                return sum;
            }
            
            float LayerWeightFresnelApprox(float3 N, float3 V, float blend)
            {
                float facing = saturate(dot(N, V));
                return pow(1.0 - facing, blend);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                
                o.positionCS = pos.positionCS;
                o.positionOS = input.positionOS.xyz;
                o.positionWS = pos.positionWS;
                o.normalWS = NormalizeNormalPerVertex(nrm.normalWS);
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(o.positionWS);
                return o;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 windowUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float3 generated = (IN.positionOS - _BoundsMin.xyz) / max(_BoundsSize.xyz, 1e-5);
                float3 coord = generated + float3(windowUV, 0.0);
                
                float n0 = BlenderNoiseApprox(coord, _NoiseScale0, _NoiseDetail0, _NoiseRoughness0, _NoiseLacunarity0, _NoiseDistortion0);
                float n1 = BlenderNoiseApprox(coord, _NoiseScale1, _NoiseDetail1, _NoiseRoughness1, _NoiseLacunarity1, _NoiseDistortion1);
                float n2 = BlenderNoiseApprox(coord, _NoiseScale2, _NoiseDetail2, _NoiseRoughness2, _NoiseLacunarity2, _NoiseDistortion2);
                
                float3 offset = float3(n0, n1, n2) * _NoiseStrength;
                float3 normalWS = normalize(IN.normalWS + offset);
                float3 viewDirWS = normalize(IN.viewDirWS);
                
                // Layer Weight & Ramps
                float lw = LayerWeightFresnelApprox(normalWS, viewDirWS, _LayerBlend);
                float3 rampEdgeA = SampleRamp(TEXTURE2D_ARGS(_RampEdgeA, sampler_RampEdgeA), lw);
                float3 rampEdgeB = SampleRamp(TEXTURE2D_ARGS(_RampEdgeB, sampler_RampEdgeB), lw);
                float edgeMask = rgb2bw(rampEdgeA) * rgb2bw(rampEdgeB);
                float3 edgeColor = SampleRamp(TEXTURE2D_ARGS(_RampEdgeColor, sampler_RampEdgeColor), edgeMask);
                
                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float lighting = NdotL * mainLight.shadowAttenuation;
                float3 lightingColor = mainLight.color * lighting;
                
                float lightingFac = rgb2bw(lightingColor);
                float3 rampLightingA = SampleRamp(TEXTURE2D_ARGS(_RampLightingA, sampler_RampLightingA), lightingFac);
                float rampLightingFac = rgb2bw(rampLightingA);
                float3 bodyColor = SampleRamp(TEXTURE2D_ARGS(_RampLightingB, sampler_RampLightingB), rampLightingFac);
                
                // Paper
                float2 paperUV = (windowUV + _PaperOffset.xy) * _PaperScale.xy;
                float3 paper = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, paperUV).rgb;
                paper = ApplyHSV(paper, _Hue, _Saturation, _Value);
                paper = ApplyCurveC(paper);
                
                // Combine
                float3 mix1 = edgeColor * paper;
                float3 finalColor = bodyColor * mix1;
                
                return half4(saturate(finalColor), 1.0);
            }
            ENDHLSL
        }
    }
}
