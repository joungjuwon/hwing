Shader "Watercolor/URP/Grass"
{
    Properties
    {
        [Header(Base Settings)]
        [MainColor] _BaseColor("Colour Tint", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Base Map", 2D) = "white" {}
        [Toggle] _AlphaClip("Alpha Clip", Float) = 1
        _Cutoff("Alpha Cutout", Range(0, 1)) = 0.5

        [Header(Grass Gradient)]
        _Grass_HighLight("Grass Highlight", Color) = (0.5, 0.9, 0.3, 1)
        _Grass_MidTone("Grass MidTone", Color) = (0.3, 0.7, 0.2, 1)
        _Grass_Shadow("Grass Shadow", Color) = (0.15, 0.4, 0.1, 1)
        _Grass_Dry("Grass Dry (Tips)", Color) = (0.6, 0.55, 0.3, 1)
        
        [Header(Fake Lighting)]
        _FakeLightColor("Fake Light Color", Color) = (1, 0.95, 0.8, 1)
        _FakeShadowColor("Fake Shadow Color", Color) = (0.3, 0.35, 0.5, 1)
        _FakeLightStrength("Fake Light Strength", Range(0, 1)) = 0.5
        
        [Header(Wind Animation)]
        _WindSpeed("Wind Speed", Float) = 1.0
        _WindStrength("Wind Strength", Range(0, 1)) = 0.3
        _WindFrequency("Wind Frequency", Float) = 1.5
        _WindGust("Wind Gust Intensity", Float) = 0.3
        
        [Header(Interactive Bend)]
        [Toggle] _EnableInteractive("Enable Interactive Bend", Float) = 1
        _BendMap("Bend Map (RT)", 2D) = "black" {}
        _BendMapWorldSize("Bend Map World Size", Float) = 50.0
        _BendStrength("Bend Strength", Range(0, 2)) = 1.0
        _BendFalloff("Bend Falloff", Range(0.1, 5)) = 2.0

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
        _PaperStrength("Paper Strength", Range(0, 1)) = 0.3
        
        [Header(Distortion)]
        _NoiseStrength("Distortion Strength", Range(0, 1)) = 0.05
        _NoiseScale("Noise Scale", Float) = 3.6
        _NoiseDetail("Noise Detail", Float) = 2.0
        _NoiseRoughness("Noise Roughness", Float) = 0.5
        _NoiseDistortion("Noise Distortion", Float) = 1.0

        [Header(Debug)]
        _WC_DebugView("Debug View (0..8)", Range(0,8)) = 0
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
                float4 color : COLOR;
                float fogFactor : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _AlphaClip;
                float _Cutoff;
                float4 _MainTex_ST;
                
                float4 _Grass_HighLight;
                float4 _Grass_MidTone;
                float4 _Grass_Shadow;
                float4 _Grass_Dry;
                
                float4 _FakeLightColor;
                float4 _FakeShadowColor;
                float _FakeLightStrength;
                
                float _WindSpeed;
                float _WindStrength;
                float _WindFrequency;
                float _WindGust;
                
                float _EnableInteractive;
                float _BendMapWorldSize;
                float _BendStrength;
                float _BendFalloff;
                
                float _LayerBlend;
                float _PaperTiling;
                float _PaperStrength;
                
                float _NoiseStrength;
                float _NoiseScale;
                float _NoiseDetail;
                float _NoiseRoughness;
                float _NoiseDistortion;

                float _WC_DebugView;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BendMap); SAMPLER(sampler_BendMap);
            TEXTURE2D(_RampLightingA); SAMPLER(sampler_RampLightingA);
            TEXTURE2D(_RampLightingB); SAMPLER(sampler_RampLightingB);
            TEXTURE2D(_RampEdgeA); SAMPLER(sampler_RampEdgeA);
            TEXTURE2D(_RampEdgeB); SAMPLER(sampler_RampEdgeB);
            TEXTURE2D(_RampEdgeCol); SAMPLER(sampler_RampEdgeCol);
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);

            // Global bend center (set by C# script tracking player/objects)
            float4 _GrassBendCenter; // xyz = world position, w = radius

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posOS = input.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(posOS);
                
                // Height mask: use vertex color R or UV.y as height factor (0 at root, 1 at tip)
                float heightMask = input.color.r;
                // Fallback: if vertex color is all white/zero, use UV.y
                if (heightMask < 0.01 && heightMask > -0.01)
                    heightMask = saturate(input.uv.y);

                // === WIND ANIMATION ===
                float windTime = _Time.y * _WindSpeed;
                float3 windOffset = float3(0, 0, 0);
                
                // Primary wave
                float wave1 = sin(windTime + worldPos.x * _WindFrequency + worldPos.z * 0.7);
                float wave2 = sin(windTime * 0.7 + worldPos.z * _WindFrequency * 0.8);
                float gust = sin(windTime * 0.3 + worldPos.x * 0.1) * _WindGust;
                
                windOffset.x = (wave1 + gust) * _WindStrength;
                windOffset.z = wave2 * _WindStrength * 0.5;
                windOffset.y = -abs(wave1) * _WindStrength * 0.1; // Slight droop when bent
                
                // Apply wind based on height
                posOS += windOffset * heightMask;

                // === INTERACTIVE BEND ===
                if (_EnableInteractive > 0.5)
                {
                    // Sample bend map (RenderTexture written by player trail system)
                    float2 bendUV = (worldPos.xz / _BendMapWorldSize) + 0.5;
                    float4 bendSample = SAMPLE_TEXTURE2D_LOD(_BendMap, sampler_BendMap, bendUV, 0);
                    
                    // bendSample.rg = bend direction XZ, bendSample.b = bend amount
                    float2 bendDir = (bendSample.rg * 2.0 - 1.0);
                    float bendAmount = bendSample.b * _BendStrength;
                    
                    // Also support simple radius-based bend from _GrassBendCenter
                    float3 toCenter = worldPos - _GrassBendCenter.xyz;
                    float distToCenter = length(toCenter.xz);
                    float radiusBend = saturate(1.0 - distToCenter / max(_GrassBendCenter.w, 0.01));
                    radiusBend = pow(radiusBend, _BendFalloff);
                    
                    float2 radiusBendDir = normalize(toCenter.xz + 0.001);
                    
                    // Combine bend map and radius-based bend
                    float2 totalBendDir = bendDir + radiusBendDir * radiusBend;
                    float totalBendAmount = max(bendAmount, radiusBend);
                    
                    // Apply bend - push grass away from center, weighted by height
                    posOS.xz += totalBendDir * totalBendAmount * heightMask;
                    posOS.y -= totalBendAmount * heightMask * 0.3; // Droop when bent
                }

                output.positionOS = posOS;
                output.positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                
                // Alpha clip
                if (_AlphaClip > 0.5)
                {
                    clip(baseTex.a - _Cutoff);
                }

                // Height-based gradient (root to tip)
                float heightFactor = saturate(input.uv.y);
                
                // Blend grass colors based on height
                float3 grassColor;
                if (heightFactor < 0.33)
                {
                    grassColor = lerp(_Grass_Shadow.rgb, _Grass_MidTone.rgb, heightFactor * 3.0);
                }
                else if (heightFactor < 0.66)
                {
                    grassColor = lerp(_Grass_MidTone.rgb, _Grass_HighLight.rgb, (heightFactor - 0.33) * 3.0);
                }
                else
                {
                    grassColor = lerp(_Grass_HighLight.rgb, _Grass_Dry.rgb, (heightFactor - 0.66) * 3.0);
                }

                // === WATERCOLOR LIGHTING ===
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                // Get main light
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                // N dot L for basic lighting
                float NdotL = dot(normalWS, lightDir);
                if (NdotL < 0) { lightDir = -lightDir; NdotL = -NdotL; }
                NdotL = NdotL * 0.5 + 0.5; // Half-lambert
                
                // Fake lighting blend
                float3 fakeLight = lerp(_FakeShadowColor.rgb, _FakeLightColor.rgb, NdotL);
                grassColor = lerp(grassColor, grassColor * fakeLight, _FakeLightStrength);

                // Noise distortion for watercolor feel
                float3 noisePos = input.positionOS * _NoiseScale;
                float noise = BlenderNoise(noisePos, _NoiseScale, _NoiseDetail, _NoiseRoughness, _NoiseDistortion);
                float3 distortedNormal = normalize(normalWS + (noise - 0.5) * _NoiseStrength);
                
                // Recalculate NdotL with distorted normal
                float distortedNdotL = saturate(dot(distortedNormal, lightDir) * 0.5 + 0.5);

                // Sample watercolor ramps
                float3 rampA = SAMPLE_TEXTURE2D(_RampLightingA, sampler_RampLightingA, float2(distortedNdotL, 0.5)).rgb;
                float3 rampB = SAMPLE_TEXTURE2D(_RampLightingB, sampler_RampLightingB, float2(distortedNdotL, 0.5)).rgb;
                
                // Edge detection via fresnel
                float fresnel = 1.0 - saturate(dot(normalWS, viewDir));
                fresnel = pow(fresnel, 2.0);
                float3 edgeMaskA = SAMPLE_TEXTURE2D(_RampEdgeA, sampler_RampEdgeA, float2(fresnel, 0.5)).rgb;
                float3 edgeMaskB = SAMPLE_TEXTURE2D(_RampEdgeB, sampler_RampEdgeB, float2(fresnel, 0.5)).rgb;
                float3 edgeColor = SAMPLE_TEXTURE2D(_RampEdgeCol, sampler_RampEdgeCol, float2(fresnel, 0.5)).rgb;
                
                // Combine watercolor layers
                float lightIntensity = rampA.r;
                float3 colorFromRamp = rampB;
                float edgeMask = lerp(edgeMaskA.r, edgeMaskB.r, _LayerBlend);
                
                float3 watercolorResult = grassColor * lightIntensity;
                watercolorResult = lerp(watercolorResult, watercolorResult * colorFromRamp, 0.5);
                watercolorResult = lerp(watercolorResult, edgeColor, edgeMask * _LayerBlend);

                // Paper texture overlay
                float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                float paper = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, screenUV * _PaperTiling).r;
                watercolorResult = lerp(watercolorResult, watercolorResult * (paper * 0.5 + 0.5), _PaperStrength);

                // Apply fog
                float3 finalColor = MixFog(watercolorResult, input.fogFactor);

                // Debug views
                if (_WC_DebugView > 0.5)
                {
                    float idx = floor(_WC_DebugView + 0.5);
                    if (idx < 1.5) return float4(grassColor, 1); // Base gradient
                    if (idx < 2.5) return float4(distortedNdotL.xxx, 1); // Light intensity
                    if (idx < 3.5) return float4(heightFactor.xxx, 1); // Height mask
                    if (idx < 4.5) return float4(edgeMask.xxx, 1); // Edge mask
                    if (idx < 5.5) return float4(noise.xxx, 1); // Noise
                    if (idx < 6.5) return float4(paper.xxx, 1); // Paper
                    return float4(finalColor, 1);
                }

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlphaClip;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            float3 _LightDirection;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 ShadowFrag(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                if (_AlphaClip > 0.5)
                {
                    clip(alpha - _Cutoff);
                }
                return 0;
            }
            ENDHLSL
        }

        // Depth only pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask R
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _AlphaClip;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 DepthFrag(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                if (_AlphaClip > 0.5)
                {
                    clip(alpha - _Cutoff);
                }
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
