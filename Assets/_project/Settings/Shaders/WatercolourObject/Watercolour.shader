Shader "Custom/Watercolour"
{
    Properties
    {
        [MainColor] _BaseColor("Colour", Color) = (0.5, 0.95, 0.6, 1)
        _ShadowColor("Shadow Colour", Color) = (0.3, 0.59, 0.61, 1)
        _DeepShadowColor("Deep Shadow Colour", Color) = (0.1, 0.3, 0.4, 1)
        
        [Header(Noise Settings)]
        _NoiseMap("Noise Texture", 2D) = "gray" {}
        _ShadowMap("Shadow Texture", 2D) = "white" {}
        _DeepShadowMap("Deep Shadow Texture", 2D) = "white" {}
        _NoiseStrength("Noise Strength", Float) = 0.2
        _NoiseBrighten("Noise Brighten", Float) = 0.1
        
        [Header(Shadow Settings)]
        _ShadowThreshold("Shadow Threshold", Range(-1, 1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0.0, 0.5)) = 0.05
        
        [Header(Deep Shadow Settings)]
        _DeepShadowThreshold("Deep Shadow Threshold", Range(-1, 1)) = 0.0
        _DeepShadowSmoothness("Deep Shadow Smoothness", Range(0.0, 0.5)) = 0.05
        _DeepShadowSpread("Deep Shadow Spread", Range(-1, 1)) = 0.0
        _DeepShadowFalloff("Deep Shadow Falloff", Range(0.1, 5)) = 1.0
        
        [Header(Texture Strength Settings)]
        _ShadowMapStrength("Shadow Map Strength", Range(0, 1)) = 1.0
        _DeepShadowMapStrength("Deep Shadow Map Strength", Range(0, 1)) = 1.0
        
        [Header(Fresnel Settings)]
        _FresnelAmount("Fresnel Amount", Float) = 0.5
        _FresnelPower("Fresnel Power", Float) = 3.0
        _FresnelThreshold("Fresnel Threshold", Range(0, 1)) = 0.5
        _FresnelSmoothness("Fresnel Smoothness", Range(0.001, 1)) = 0.1

        [Header(Outline Settings)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
        [Toggle(_USE_SMOOTHED_NORMALS_ENABLED)] _USE_SMOOTHED_NORMALS_ENABLED("Use Smoothed Normals (UV3)", Float) = 0
        
        [Header(Outline Noise)]
        _OutlineTexture ("Outline Texture (Alpha)", 2D) = "white" {}
        [Toggle(_INNER_OUTLINE)] _UseInnerOutline("Enable Inner Outline (Front)", Float) = 0
        _InnerOutlineAlpha("Inner Outline Alpha", Range(0, 1)) = 0.5
        _InnerOutlineThreshold("Inner Outline Threshold", Range(0, 1)) = 0.5
        _InnerOutlineSmoothness("Inner Outline Smoothness", Range(0, 1)) = 0.1
        _InnerOutlineNoiseStrength("Inner Outline Noise Strength", Range(0, 2)) = 0.5
        _InnerOutlineEdgePower("Inner Outline Edge Power", Range(1, 10)) = 1.0
        [Toggle(_TEXTURE_AS_MASK)] _UseTextureAsMask("Use Texture as Mask (Black=Transparent)", Float) = 0
        
        _OutlineNoiseTexture ("Noise Texture", 2D) = "white" {} 
        _OutlineNoiseFrequency ("Noise Frequency", Float) = 5.0
        _OutlineNoiseFramerate ("Noise Framerate", Float) = 12.0
        [Toggle(_RANDOM_OFFSETS_ENABLED)] _RANDOM_OFFSETS_ENABLED("Randomly offset the sample position", Float) = 0

        [Header(Highlight Settings)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Glossiness("Glossiness", Range(0.01, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            ZWrite On // Force Depth Write to prevent Outline overdraw

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Unity 6 / URP Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
                float3 normalSmooth : TEXCOORD3; // Smoothed Normals
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD3;
                float3 normalSmoothWS : TEXCOORD4; // Passed to Frag
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
                #ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV : TEXCOORD6;
                #endif
                float4 probeOcclusion : TEXCOORD7;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowThreshold;
                float _DeepShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _ShadowMapStrength;
                float _DeepShadowMapStrength;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _SpecularColor;
                float _Glossiness;
                
                // Inner Outline Properties (Added for ForwardLit integration)
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float _InnerOutlineThreshold;
                float _InnerOutlineSmoothness;
                float _InnerOutlineNoiseStrength;
                float _InnerOutlineEdgePower;
                float _UseTextureAsMask;
                float4 _OutlineTexture_ST;
                
                // OccaSoftware Properties
                float _USE_SMOOTHED_NORMALS_ENABLED;
            CBUFFER_END

            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_ShadowMap);
            SAMPLER(sampler_ShadowMap);
            TEXTURE2D(_DeepShadowMap);
            SAMPLER(sampler_DeepShadowMap);
            
            TEXTURE2D(_OutlineTexture);
            SAMPLER(sampler_OutlineTexture);

            // URP already provides SafeNormalize in Core.hlsl

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Pass Smoothed Normals (or fallback to regular)
                if (_USE_SMOOTHED_NORMALS_ENABLED > 0.5)
                {
                    output.normalSmoothWS = TransformObjectToWorldNormal(input.normalSmooth);
                }
                else
                {
                    output.normalSmoothWS = output.normalWS;
                }

                output.uv = input.uv; // Pass raw UVs, transform in Frag
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                output.probeOcclusion = 0.0;
                OUTPUT_SH4(GetAbsolutePositionWS(output.positionWS), output.normalWS, GetWorldSpaceNormalizeViewDir(output.positionWS), output.vertexSH, output.probeOcclusion);
                #ifdef DYNAMICLIGHTMAP_ON
                output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Use SafeNormalize to prevent NaN from degenerate normals
                float3 normalWS = SafeNormalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));

                // --- Lighting Calculation ---
                float4 shadowCoord;
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
                shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord);
                float3 mainLightColor = mainLight.color * mainLight.distanceAttenuation;
                
                float NdotL = dot(normalWS, mainLight.direction);
                float lightIntensity = saturate(NdotL);
                
                // Shadow Attenuation
                float shadowAtten = mainLight.shadowAttenuation;
                lightIntensity *= shadowAtten;
                float3 directLighting = mainLightColor * lightIntensity;

                // --- UV Calculations ---
                // Calculate separate UVs for each texture to allow independent Tiling/Offset
                float2 noiseUV = TRANSFORM_TEX(input.uv, _NoiseMap);
                float2 shadowUV = TRANSFORM_TEX(input.uv, _ShadowMap);
                float2 deepShadowUV = TRANSFORM_TEX(input.uv, _DeepShadowMap);

                // --- Noise Sampling ---
                half4 noiseSample = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV);
                float noiseVal = noiseSample.r;
                
                // Sample Shadow Texture
                half4 shadowTexSample = SAMPLE_TEXTURE2D(_ShadowMap, sampler_ShadowMap, shadowUV);
                float3 shadowPattern = shadowTexSample.rgb;

                // Sample Deep Shadow Texture
                half4 deepShadowTexSample = SAMPLE_TEXTURE2D(_DeepShadowMap, sampler_DeepShadowMap, deepShadowUV);
                float3 deepShadowPattern = deepShadowTexSample.rgb;

                // --- Color Definitions ---
                // Mix the texture with white based on strength before multiplying
                // or lerp the result: lerp(Color, Color * Texture, Strength)
                
                float3 shadowTexMixed = lerp(float3(1,1,1), shadowPattern, _ShadowMapStrength);
                float3 effectiveShadow = _ShadowColor.rgb * shadowTexMixed;
                
                float3 deepShadowTexMixed = lerp(float3(1,1,1), deepShadowPattern, _DeepShadowMapStrength);
                float3 effectiveDeepShadow = _DeepShadowColor.rgb * deepShadowTexMixed;

                // --- Specular Highlighting (Wet Paint Effect) ---
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, _Glossiness * 128.0);
                float3 specularColor = specular * _SpecularColor.rgb;

                // --- Lighting Calculation (Cel Shading Gradient) ---
                // We use NdotL directly for the gradient
                // Note: We DO NOT multiply shadowAtten here anymore.
                // We want the gradient to be based on the object's form (Self-Shadow).
                // Cast shadows will be applied as a "Force Dark" override later.
                float noisyNdotL = NdotL + (noiseVal - 0.5) * _NoiseStrength;

                // --- Global Shading (Deep Shadow) ---
                // Apply Spread (Offset) and Falloff (Power) to the gradient
                float gradientInput = noisyNdotL + _DeepShadowSpread;
                float remappedGradient = saturate(gradientInput * 0.5 + 0.5); // 0..1
                float globalShading = pow(remappedGradient, _DeepShadowFalloff);

                // 2. Two-Tone Shadow (Cel Shading Step)
                // This adds the stylized "Shadow" band
                float shadowEdge = _ShadowThreshold;
                float shadowSmooth = _ShadowSmoothness;
                float shadowFactor = smoothstep(shadowEdge - shadowSmooth, shadowEdge + shadowSmooth, noisyNdotL);
                
                // --- Apply Cast Shadows (Shadow Attenuation) ---
                // Cast shadows should force the surface into Shadow and then Deep Shadow.
                // We apply shadowAtten to the factors.
                // If shadowAtten is 0 (Dark), factors go to 0 (Dark state).
                shadowFactor = min(shadowFactor, shadowAtten);
                globalShading = min(globalShading, shadowAtten);
                
                // Calculate Cel Shading Base (Base vs Shadow)
                float3 celColor = lerp(effectiveShadow, _BaseColor.rgb, shadowFactor);
                
                // Combine Global Shading (Deep Shadow) with Cel Shading
                // We use the global gradient (globalShading) to interpolate towards Deep Shadow
                // globalShading: 1 = Lightest (use celColor), 0 = Darkest (use Deep Shadow)
                float3 mixedBase = lerp(effectiveDeepShadow, celColor, globalShading);
                
                // Apply Brightness from Noise (Watercolour paper effect) - subtle overlay
                float3 baseColor = mixedBase + (noiseVal - 0.5) * _NoiseBrighten * 0.5;

                // --- Fresnel Effect (Hard Rim Light) ---
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnelBase = pow(1.0 - NdotV, _FresnelPower);
                
                // Apply smoothstep for Hard Fresnel
                // _FresnelThreshold controls where the rim starts
                // _FresnelSmoothness controls how sharp the edge is
                float fresnel = smoothstep(_FresnelThreshold, _FresnelThreshold + _FresnelSmoothness, fresnelBase);
                
                float3 fresnelColor = fresnel * _FresnelAmount * _BaseColor.rgb; // Rim light color

                float3 additionalLighting = 0.0;
                #if defined(_ADDITIONAL_LIGHTS)
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
                {
                    Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                    float NdotLAdd = saturate(dot(normalWS, additionalLight.direction));
                    additionalLighting += additionalLight.color * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation * NdotLAdd;
                }
                #endif
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                additionalLighting += VertexLighting(input.positionWS, normalWS);
                #endif

                float3 bakedGI = 0.0;
                #if defined(DYNAMICLIGHTMAP_ON)
                bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, normalWS);
                #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                bakedGI = SAMPLE_GI(input.vertexSH,
                    GetAbsolutePositionWS(input.positionWS),
                    normalWS,
                    viewDirWS,
                    input.positionCS.xy,
                    input.probeOcclusion,
                    0);
                #else
                bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
                #endif
                float3 lighting = directLighting + additionalLighting + bakedGI;

                float3 finalColor = baseColor * lighting;
                finalColor += specularColor * directLighting;
                finalColor += fresnelColor * directLighting;

                // Apply Fog
                MixFog(finalColor, input.fogFactor);

                // --- Inner Outline Overlay (Integrated into Main Pass) ---
                if (_UseInnerOutline > 0.5)
                {
                    // Calculate UVs for Outline Texture
                    float2 outlineUV = TRANSFORM_TEX(input.uv, _OutlineTexture);
                    half4 outlineTexColor = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, outlineUV);
                    
                    // Fresnel Fade (Fade Inwards) using Smoothed Normals
                    float3 viewDir = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                    float3 smoothNormal = SafeNormalize(input.normalSmoothWS);
                    float NdotV_inner = saturate(dot(smoothNormal, viewDir));
                    
                    // 1. Base Gradient (0 at center, 1 at edge)
                    float fresnelBaseInner = 1.0 - NdotV_inner;
                    
                    // 2. Apply Noise Distortion (Blotchy Edge)
                    // Use the texture to perturb the gradient
                    fresnelBaseInner += (outlineTexColor.r - 0.5) * _InnerOutlineNoiseStrength;
                    
                    // 3. Apply Threshold & Smoothness (with minimum to prevent artifacts)
                    float innerFresnel = smoothstep(_InnerOutlineThreshold, _InnerOutlineThreshold + max(_InnerOutlineSmoothness, 0.001), fresnelBaseInner);
                    
                    // 4. Apply Edge Power (Coffee Ring Effect) with clamped input
                    // Makes the edge darker and center fade faster
                    float edgePower = max(_InnerOutlineEdgePower, 0.1);
                    innerFresnel = pow(max(innerFresnel, 0.0), edgePower);
                    
                    float overlayAlpha = 0;
                    float3 overlayColor = _OutlineColor.rgb;
                    
                    if (_UseTextureAsMask > 0.5)
                    {
                        // Mask Mode: Use Texture Brightness (R) as Alpha
                        // Ignore Texture Color (use Outline Color only)
                        overlayAlpha = outlineTexColor.r * _InnerOutlineAlpha * innerFresnel;
                        overlayColor = _OutlineColor.rgb;
                    }
                    else
                    {
                        // Standard Mode: Use Texture Alpha and Color
                        overlayAlpha = outlineTexColor.a * _InnerOutlineAlpha * innerFresnel;
                        overlayColor = _OutlineColor.rgb * outlineTexColor.rgb;
                    }
                    
                    // Blend Outline Color on top of Final Color
                    finalColor = lerp(finalColor, overlayColor, overlayAlpha);
                }

                return float4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLitOnly"
            Tags { "LightMode" = "UniversalForwardOnly" }
            
            ZWrite On // Force Depth Write to prevent Outline overdraw

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Unity 6 / URP Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV : TEXCOORD2;
                float3 normalSmooth : TEXCOORD3; // Smoothed Normals
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD3;
                float3 normalSmoothWS : TEXCOORD4; // Passed to Frag
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
                #ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV : TEXCOORD6;
                #endif
                float4 probeOcclusion : TEXCOORD7;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowThreshold;
                float _DeepShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _ShadowMapStrength;
                float _DeepShadowMapStrength;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _OutlineColor;
                float _OutlineWidth;
                float4 _SpecularColor;
                float _Glossiness;
                
                // Inner Outline Properties (Added for ForwardLit integration)
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float _InnerOutlineThreshold;
                float _InnerOutlineSmoothness;
                float _InnerOutlineNoiseStrength;
                float _InnerOutlineEdgePower;
                float _UseTextureAsMask;
                float4 _OutlineTexture_ST;
                
                // OccaSoftware Properties
                float _USE_SMOOTHED_NORMALS_ENABLED;
            CBUFFER_END

            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_ShadowMap);
            SAMPLER(sampler_ShadowMap);
            TEXTURE2D(_DeepShadowMap);
            SAMPLER(sampler_DeepShadowMap);
            
            TEXTURE2D(_OutlineTexture);
            SAMPLER(sampler_OutlineTexture);

            // URP already provides SafeNormalize in Core.hlsl

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Pass Smoothed Normals (or fallback to regular)
                if (_USE_SMOOTHED_NORMALS_ENABLED > 0.5)
                {
                    output.normalSmoothWS = TransformObjectToWorldNormal(input.normalSmooth);
                }
                else
                {
                    output.normalSmoothWS = output.normalWS;
                }

                output.uv = input.uv; // Pass raw UVs, transform in Frag
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                output.probeOcclusion = 0.0;
                OUTPUT_SH4(GetAbsolutePositionWS(output.positionWS), output.normalWS, GetWorldSpaceNormalizeViewDir(output.positionWS), output.vertexSH, output.probeOcclusion);
                #ifdef DYNAMICLIGHTMAP_ON
                output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Use SafeNormalize to prevent NaN from degenerate normals
                float3 normalWS = SafeNormalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));

                // --- Lighting Calculation ---
                float4 shadowCoord;
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
                shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord);
                float3 mainLightColor = mainLight.color * mainLight.distanceAttenuation;
                
                float NdotL = dot(normalWS, mainLight.direction);
                float lightIntensity = saturate(NdotL);
                
                // Shadow Attenuation
                float shadowAtten = mainLight.shadowAttenuation;
                lightIntensity *= shadowAtten;
                float3 directLighting = mainLightColor * lightIntensity;

                // --- UV Calculations ---
                // Calculate separate UVs for each texture to allow independent Tiling/Offset
                float2 noiseUV = TRANSFORM_TEX(input.uv, _NoiseMap);
                float2 shadowUV = TRANSFORM_TEX(input.uv, _ShadowMap);
                float2 deepShadowUV = TRANSFORM_TEX(input.uv, _DeepShadowMap);

                // --- Noise Sampling ---
                half4 noiseSample = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUV);
                float noiseVal = noiseSample.r;
                
                // Sample Shadow Texture
                half4 shadowTexSample = SAMPLE_TEXTURE2D(_ShadowMap, sampler_ShadowMap, shadowUV);
                float3 shadowPattern = shadowTexSample.rgb;

                // Sample Deep Shadow Texture
                half4 deepShadowTexSample = SAMPLE_TEXTURE2D(_DeepShadowMap, sampler_DeepShadowMap, deepShadowUV);
                float3 deepShadowPattern = deepShadowTexSample.rgb;

                // --- Color Definitions ---
                // Mix the texture with white based on strength before multiplying
                // or lerp the result: lerp(Color, Color * Texture, Strength)
                
                float3 shadowTexMixed = lerp(float3(1,1,1), shadowPattern, _ShadowMapStrength);
                float3 effectiveShadow = _ShadowColor.rgb * shadowTexMixed;
                
                float3 deepShadowTexMixed = lerp(float3(1,1,1), deepShadowPattern, _DeepShadowMapStrength);
                float3 effectiveDeepShadow = _DeepShadowColor.rgb * deepShadowTexMixed;

                // --- Specular Highlighting (Wet Paint Effect) ---
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, _Glossiness * 128.0);
                float3 specularColor = specular * _SpecularColor.rgb;

                // --- Lighting Calculation (Cel Shading Gradient) ---
                // We use NdotL directly for the gradient
                // Note: We DO NOT multiply shadowAtten here anymore.
                // We want the gradient to be based on the object's form (Self-Shadow).
                // Cast shadows will be applied as a "Force Dark" override later.
                float noisyNdotL = NdotL + (noiseVal - 0.5) * _NoiseStrength;

                // --- Global Shading (Deep Shadow) ---
                // Apply Spread (Offset) and Falloff (Power) to the gradient
                float gradientInput = noisyNdotL + _DeepShadowSpread;
                float remappedGradient = saturate(gradientInput * 0.5 + 0.5); // 0..1
                float globalShading = pow(remappedGradient, _DeepShadowFalloff);

                // 2. Two-Tone Shadow (Cel Shading Step)
                // This adds the stylized "Shadow" band
                float shadowEdge = _ShadowThreshold;
                float shadowSmooth = _ShadowSmoothness;
                float shadowFactor = smoothstep(shadowEdge - shadowSmooth, shadowEdge + shadowSmooth, noisyNdotL);
                
                // --- Apply Cast Shadows (Shadow Attenuation) ---
                // Cast shadows should force the surface into Shadow and then Deep Shadow.
                // We apply shadowAtten to the factors.
                // If shadowAtten is 0 (Dark), factors go to 0 (Dark state).
                shadowFactor = min(shadowFactor, shadowAtten);
                globalShading = min(globalShading, shadowAtten);
                
                // Calculate Cel Shading Base (Base vs Shadow)
                float3 celColor = lerp(effectiveShadow, _BaseColor.rgb, shadowFactor);
                
                // Combine Global Shading (Deep Shadow) with Cel Shading
                // We use the global gradient (globalShading) to interpolate towards Deep Shadow
                // globalShading: 1 = Lightest (use celColor), 0 = Darkest (use Deep Shadow)
                float3 mixedBase = lerp(effectiveDeepShadow, celColor, globalShading);
                
                // Apply Brightness from Noise (Watercolour paper effect) - subtle overlay
                float3 baseColor = mixedBase + (noiseVal - 0.5) * _NoiseBrighten * 0.5;

                // --- Fresnel Effect (Hard Rim Light) ---
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnelBase = pow(1.0 - NdotV, _FresnelPower);
                
                // Apply smoothstep for Hard Fresnel
                // _FresnelThreshold controls where the rim starts
                // _FresnelSmoothness controls how sharp the edge is
                float fresnel = smoothstep(_FresnelThreshold, _FresnelThreshold + _FresnelSmoothness, fresnelBase);
                
                float3 fresnelColor = fresnel * _FresnelAmount * _BaseColor.rgb; // Rim light color

                float3 additionalLighting = 0.0;
                #if defined(_ADDITIONAL_LIGHTS)
                uint additionalLightsCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
                {
                    Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                    float NdotLAdd = saturate(dot(normalWS, additionalLight.direction));
                    additionalLighting += additionalLight.color * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation * NdotLAdd;
                }
                #endif
                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                additionalLighting += VertexLighting(input.positionWS, normalWS);
                #endif

                float3 bakedGI = 0.0;
                #if defined(DYNAMICLIGHTMAP_ON)
                bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, normalWS);
                #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                bakedGI = SAMPLE_GI(input.vertexSH,
                    GetAbsolutePositionWS(input.positionWS),
                    normalWS,
                    viewDirWS,
                    input.positionCS.xy,
                    input.probeOcclusion,
                    0);
                #else
                bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, normalWS);
                #endif
                float3 lighting = directLighting + additionalLighting + bakedGI;

                float3 finalColor = baseColor * lighting;
                finalColor += specularColor * directLighting;
                finalColor += fresnelColor * directLighting;

                // Apply Fog
                MixFog(finalColor, input.fogFactor);

                // --- Inner Outline Overlay (Integrated into Main Pass) ---
                if (_UseInnerOutline > 0.5)
                {
                    // Calculate UVs for Outline Texture
                    float2 outlineUV = TRANSFORM_TEX(input.uv, _OutlineTexture);
                    half4 outlineTexColor = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, outlineUV);
                    
                    // Fresnel Fade (Fade Inwards) using Smoothed Normals
                    float3 viewDir = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                    float3 smoothNormal = SafeNormalize(input.normalSmoothWS);
                    float NdotV_inner = saturate(dot(smoothNormal, viewDir));
                    
                    // 1. Base Gradient (0 at center, 1 at edge)
                    float fresnelBaseInner = 1.0 - NdotV_inner;
                    
                    // 2. Apply Noise Distortion (Blotchy Edge)
                    // Use the texture to perturb the gradient
                    fresnelBaseInner += (outlineTexColor.r - 0.5) * _InnerOutlineNoiseStrength;
                    
                    // 3. Apply Threshold & Smoothness (with minimum to prevent artifacts)
                    float innerFresnel = smoothstep(_InnerOutlineThreshold, _InnerOutlineThreshold + max(_InnerOutlineSmoothness, 0.001), fresnelBaseInner);
                    
                    // 4. Apply Edge Power (Coffee Ring Effect) with clamped input
                    // Makes the edge darker and center fade faster
                    float edgePower = max(_InnerOutlineEdgePower, 0.1);
                    innerFresnel = pow(max(innerFresnel, 0.0), edgePower);
                    
                    float overlayAlpha = 0;
                    float3 overlayColor = _OutlineColor.rgb;
                    
                    if (_UseTextureAsMask > 0.5)
                    {
                        // Mask Mode: Use Texture Brightness (R) as Alpha
                        // Ignore Texture Color (use Outline Color only)
                        overlayAlpha = outlineTexColor.r * _InnerOutlineAlpha * innerFresnel;
                        overlayColor = _OutlineColor.rgb;
                    }
                    else
                    {
                        // Standard Mode: Use Texture Alpha and Color
                        overlayAlpha = outlineTexColor.a * _InnerOutlineAlpha * innerFresnel;
                        overlayColor = _OutlineColor.rgb * outlineTexColor.rgb;
                    }
                    
                    // Blend Outline Color on top of Final Color
                    finalColor = lerp(finalColor, overlayColor, overlayAlpha);
                }

                return float4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
        
        // Outline Pass (Inverted Hull) - OccaSoftware Logic Integrated
        // Outline Pass (Combined Outer & Inner)
        // Outline Pass (Outer Only - Inverted Hull)
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // --- OccaSoftware Logic Start ---
            float nrand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            // --------------------------------

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float3 normalSmooth : TEXCOORD3; // Smoothed Normals
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                
                // OccaSoftware Properties
                float _USE_SMOOTHED_NORMALS_ENABLED;
                float4 _OutlineNoiseTexture_ST;
                float _OutlineNoiseFrequency;
                float _OutlineNoiseFramerate;
                float _RANDOM_OFFSETS_ENABLED;
                
                float4 _OutlineTexture_ST;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
            CBUFFER_END
            
            TEXTURE2D(_OutlineNoiseTexture);
            SAMPLER(sampler_OutlineNoiseTexture);
            
            TEXTURE2D(_OutlineTexture);
            SAMPLER(sampler_OutlineTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 1. Normal Calculation
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                if(_USE_SMOOTHED_NORMALS_ENABLED > 0.5)
                {
                    normalWS = TransformObjectToWorldNormal(input.normalSmooth);
                }
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // 2. Thickness Calculation
                float thickness = max(_OutlineWidth, 0);
                
                // Noise Control
                float dist = length(input.positionOS.xyz);
                float r = _Time.y * _OutlineNoiseFramerate;
                if(_RANDOM_OFFSETS_ENABLED > 0.5)
                {
                    r = nrand(floor(r));
                }
                
                // Sample Noise
                float noiseVal = SAMPLE_TEXTURE2D_LOD(_OutlineNoiseTexture, sampler_OutlineNoiseTexture, float2(r + (dist * _OutlineNoiseFrequency), 0), 0).r;
                thickness *= noiseVal;

                // 3. Extrusion
                positionWS += normalWS * thickness;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                
                // Pass Data to Fragment
                output.uv = TRANSFORM_TEX(input.texcoord, _OutlineTexture);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Outer Outline is always solid (or uses texture color but opaque alpha)
                half4 texColor = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, input.uv);
                return float4(_OutlineColor.rgb * texColor.rgb, _OutlineColor.a);
            }
            ENDHLSL
        }


        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 normalSmooth : TEXCOORD3; // Smoothed Normals
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _USE_SMOOTHED_NORMALS_ENABLED;
            CBUFFER_END

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                
                // 1. Normal Calculation
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Use Smoothed Normals if enabled
                if(_USE_SMOOTHED_NORMALS_ENABLED > 0.5)
                {
                    normalWS = TransformObjectToWorldNormal(input.normalSmooth);
                }
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = NormalizeNormalPerVertex(normalWS);
                
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                // URP expects view-space normals encoded properly
                // Convert to view space and encode
                float3 normalVS = TransformWorldToViewNormal(normalWS);
                // Pack normal: remap from [-1,1] to [0,1]
                return float4(normalVS * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.uv = input.texcoord;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Apply Shadow Bias to prevent shadow acne (self-shadowing artifacts)
                // This uses the Depth Bias and Normal Bias settings from the URP Asset
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);
                
                output.positionCS = TransformWorldToHClip(biasedPositionWS);

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
