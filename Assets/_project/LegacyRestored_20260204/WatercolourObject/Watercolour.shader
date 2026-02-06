Shader "Legacy/Watercolour"
{
    Properties
    {
        [Header(Blender Port Settings)]
        _BlenderNormalOffset("Normal Offset Strength", Float) = 0.7
        _BlenderNormalScale("Normal Scale Overrides", Vector) = (0,0,0,0) // 0 means default
        _BlenderNormalAmplitude("Normal Amplitude", Float) = 2.0
        
        _LightingRampThreshold("Lighting Threshold", Range(0, 1)) = 0.5
        _LightingRampSmoothness("Lighting Smoothness", Range(0, 0.5)) = 0.1
        
        _EdgeThreshold("Edge Threshold", Range(0, 1)) = 0.5
        _EdgeSmoothness("Edge Smoothness", Range(0, 0.5)) = 0.1
        
        [Header(Paper Settings)]
        _PaperTex("Paper Texture", 2D) = "white" {}
        _PaperTiling("Paper Tiling", Vector) = (1,1,0,0)
        _PaperScale("Paper Global Scale", Float) = 1.0
        _PaperHueShift("Hue Shift", Range(-0.5, 0.5)) = 0.0
        _PaperSaturation("Saturation", Range(0, 2)) = 1.0
        _PaperValue("Value (Brightness)", Range(0, 2)) = 1.0

        // Keep original properties for fallback/mixing if needed
        [MainColor] _BaseColor("Colour Tint", Color) = (1, 1, 1, 1)
        _ShadowColor("Shadow Tint", Color) = (0.3, 0.59, 0.61, 1)
        
        [MainTexture] _MainTex("Base Map (Texture)", 2D) = "white" {}
        _Cutoff("Alpha Cutout", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

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
            #include "Assets/_project/Shaders/Watercolor/Legacy/WatercolorCore.hlsl"

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                
                // Blender Port Vars
                float _BlenderNormalOffset;
                float4 _BlenderNormalScale;
                float _BlenderNormalAmplitude;
                float _LightingRampThreshold;
                float _LightingRampSmoothness;
                float _EdgeThreshold;
                float _EdgeSmoothness;
                float4 _PaperTex_ST;
                float4 _PaperTiling;
                float _PaperScale;
                float _PaperHueShift;
                float _PaperSaturation;
                float _PaperValue;
                
                // Legacy support if needed
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END
            
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);
            // Keep others just in case
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_ShadowMap); SAMPLER(sampler_ShadowMap);
            TEXTURE2D(_DeepShadowMap); SAMPLER(sampler_DeepShadowMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv; // Using UV0 (or pass PositionOS)
                
                // Pack positionOS into TEXCOORD3 or similar if free, 
                // but we can just use input.positionOS in frag? No, we need it in varying.
                // Re-using fogFactor channel or creating new one?
                // Let's create new one in struct.
                // Wait, I can't modify struct in this chunk easily as it's separate. 
                // I will assume I modify Fragment to reconstructed OS or use World Position for noise if OS is hard.
                // Actually, for "Object Space" noise, using positionWS - ObjectPosition is approximate if no rotation/scale.
                // But better to pass positionOS. 
                // Since I can't change the struct in this specific tool call cleanly without overlapping lines or massive replace...
                // I will use `TransformWorldToObject` in Fragment for now. It's slightly expensive but safe.
                
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = SafeNormalize(input.normalWS);
                float3 viewDirWS = SafeNormalize(GetWorldSpaceNormalizeViewDir(input.positionWS));

                // 1. Recover Object Space Position and Normal for Blender Logic
                float3 positionOS = TransformWorldToObject(input.positionWS);
                float3 normalOS = TransformWorldToObjectNormal(normalWS);
                
                // 2. Perturb Normals (Blender "Normals" Group)
                float3 perturbedNormalOS;
                WatercolorBlenderNormals_float(
                    positionOS * _PaperScale, // Scale coordinate space
                    normalOS,
                    _BlenderNormalOffset,
                    _BlenderNormalScale.xyz,
                    float3(_BlenderNormalAmplitude, _BlenderNormalAmplitude, _BlenderNormalAmplitude), // Simplify amplitude
                    perturbedNormalOS
                );
                
                float3 perturbedNormalWS = TransformObjectToWorldNormal(perturbedNormalOS);
                perturbedNormalWS = SafeNormalize(perturbedNormalWS);
                
                // 3. Lighting (Blender "Lighting" Group)
                // Use Main Light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightingColor;
                WatercolorBlenderLighting_float(
                    perturbedNormalWS,
                    mainLight.direction,
                    _BaseColor.rgb,
                    _ShadowColor.rgb,
                    _LightingRampThreshold,
                    _LightingRampSmoothness,
                    lightingColor
                );
                
                // Apply Shadow Attenuation just in case (though Blender graph didn't show it, it's good for Unity integration)
                lightingColor *= mainLight.shadowAttenuation;
                
                // 4. Edges (Blender "Edges" Group)
                float edgeFactor;
                WatercolorBlenderEdges_float(
                    perturbedNormalWS,
                    viewDirWS,
                    _EdgeThreshold,
                    _EdgeSmoothness,
                    edgeFactor
                );
                
                // Combine Lighting and Edges
                // Graph implied: Mix(Lighting, EdgeColor, EdgeFactor)
                // We'll use ShadowColor or OutlineColor for edges. Blender graph had "Color Ramp" -> Black/White/Color.
                // Let's blindly mix to black or ShadowTint for edges.
                // Actually, let's mix to _OutlineColor
                float3 combinedBase = lerp(lightingColor, _OutlineColor.rgb, edgeFactor);
                
                // 5. Paper Texture (Blender "Paper Texture" Group)
                float3 paperColor;
                // Use Screen Space or Object Space? Blender uses "Generated" (Object).
                // We passed positionOS. Maybe use that.
                // Graph: Mapping -> Texture.
                // Let's use UV for Paper to be standard, or positionOS projected.
                // User said "World View is like this... Paper Texture is Subgraph".
                WatercolorPaperTexture_float(
                    input.uv, // Or positionOS.xy
                    _PaperTiling.xy,
                    _PaperTex,
                    sampler_PaperTex,
                    _PaperHueShift,
                    _PaperSaturation,
                    _PaperValue,
                    paperColor
                );
                
                // 6. Final Combine
                // Multiply? Overlay? Blender "Mix" node (Multiply usually for paper).
                float3 finalColor = combinedBase * paperColor;

                // Fog
                MixFog(finalColor, input.fogFactor);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Offset 1, 1

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings OutlineVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                positionWS += normalWS * _OutlineWidth;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            float4 OutlineFrag(Varyings input) : SV_Target
            {
                clip(_OutlineWidth - 1e-5);
                return _OutlineColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma target 4.5
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
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
            #pragma target 4.5
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                float3 normalVS = TransformWorldToViewNormal(input.normalWS);
                return float4(normalVS * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }

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
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _ShadowColor;
                float4 _DeepShadowColor;
                float4 _MainTex_ST;
                float4 _NoiseMap_ST;
                float4 _ShadowMap_ST;
                float4 _DeepShadowMap_ST;
                float _NoiseStrength;
                float _NoiseBrighten;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _DeepShadowSpread;
                float _DeepShadowFalloff;
                float _FresnelAmount;
                float _FresnelPower;
                float _FresnelThreshold;
                float _FresnelSmoothness;
                float4 _SpecularColor;
                float _Glossiness;
                float _SpecularNoiseStrength;
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);

                output.positionCS = TransformWorldToHClip(biasedPositionWS);
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            float4 ShadowPassFragment(Varyings input) : SV_Target
            {
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
