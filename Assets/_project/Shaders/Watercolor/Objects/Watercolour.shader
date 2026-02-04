Shader "Watercolor/URP/Watercolour"
{
    Properties
    {
        [Header(Base Settings)]
        [MainColor] _BaseColor("Colour Tint", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Base Map (Texture)", 2D) = "white" {}
        _Cutoff("Alpha Cutout", Range(0, 1)) = 0.5

        [Header(Base Texture vs Palette)]
        _WC_PaletteStrength("Palette Strength (0=keep texture color)", Range(0,1)) = 0.35
        _WC_BaseDetailStrength("Base Detail Strength (luma)", Range(0,1)) = 0.35

        [Header(Inner Line (Front Overlay))]
        [Toggle] _UseInnerOutline("Use Inner Line", Float) = 0
        _InnerOutlineAlpha("Inner Line Alpha", Range(0,1)) = 0.35
        _InnerOutlineColor("Inner Line Color", Color) = (0.12, 0.08, 0.06, 1)
        _InnerOutlineTex("Inner Line Texture (RGBA)", 2D) = "white" {}
        _InnerOutlineTexTiling("Inner Line Tex Tiling", Float) = 1.0
        _InnerOutlineWidth("Inner Line Width (Mesh Extrude)", Range(0, 0.1)) = 0.01

        // Silhouette-distance mask controls (depth-based)
        _InnerOutlineDepthWidth("Inner Line Depth Width", Range(0.0001, 2.0)) = 0.08
        _InnerOutlineThreshold("Inner Line Threshold (Depth)", Range(0.0001, 2.0)) = 0.25
        _InnerOutlineSmoothness("Inner Line Smoothness (Depth)", Range(0.0001, 0.5)) = 0.03

        [Header(Complex Watercolor Lighting)]
        _RampLightingA("Ramp Lighting A (Intensity)", 2D) = "white" {}
        _RampLightingB("Ramp Lighting B (Color)", 2D) = "white" {}
        _RampEdgeA("Ramp Edge A (Mask)", 2D) = "white" {}
        _RampEdgeB("Ramp Edge B (Mask)", 2D) = "white" {}
        _RampEdgeCol("Ramp Edge Color", 2D) = "white" {}
        
        _LayerBlend("Edge Blend Strength", Range(0, 1)) = 0.2

        [Header(Noise Distortion)]
        _NoiseStrength("Distortion Strength", Range(0, 1)) = 0.1
        _NoiseScale("Noise Scale", Float) = 3.6
        _NoiseDetail("Noise Detail", Float) = 2.0
        _NoiseRoughness("Noise Roughness", Float) = 0.5
        _NoiseDistortion("Noise Distortion", Float) = 1.0
        
        [Header(Paper Texture)]
        _PaperTex("Paper Texture", 2D) = "white" {}
        _PaperTiling("Paper Tiling", Float) = 1.0
        _PaperStrength("Paper Strength", Range(0, 1)) = 0.5
        
        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02

        [Header(Debug)]
        // Unity 6 material Enum drawer can fail when explicit numeric pairs are used. Use implicit 0..N.
        [Enum(Off,BaseMap,LightIntensity,RampB,EdgeMask,EdgeColor,Raw,Tonemapped,Final)] _WC_DebugView("Debug View", Float) = 0
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
            Cull Back

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Cutoff;
                float4 _MainTex_ST;

                float _LayerBlend;
                float _WC_PaletteStrength;
                float _WC_BaseDetailStrength;

                // Inner line (front overlay) controls
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _InnerOutlineColor;
                float _InnerOutlineTexTiling;
                float _InnerOutlineDepthWidth;
                float _InnerOutlineThreshold;
                float _InnerOutlineSmoothness;

                float _NoiseStrength;
                float _NoiseScale;
                float _NoiseDetail;
                float _NoiseRoughness;
                float _NoiseDistortion;

                float _PaperTiling;
                float _PaperStrength;

                float4 _OutlineColor;
                float _OutlineWidth;

                float _WC_DebugView;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_RampLightingA); SAMPLER(sampler_RampLightingA);
            TEXTURE2D(_RampLightingB); SAMPLER(sampler_RampLightingB);
            TEXTURE2D(_RampEdgeA); SAMPLER(sampler_RampEdgeA);
            TEXTURE2D(_RampEdgeB); SAMPLER(sampler_RampEdgeB);
            TEXTURE2D(_RampEdgeCol); SAMPLER(sampler_RampEdgeCol);
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);

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
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 1. Prepare Data
                float3 normalWS = SafeNormalize(input.normalWS);
                float3 positionOS = TransformWorldToObject(input.positionWS);
                float3 normalOS = TransformWorldToObjectNormal(normalWS);
                
                // 2. Distort Normals (High Quality)
                float3 distortedNormalOS;
                WatercolorDistortNormal_float(
                    positionOS, normalOS, 
                    _NoiseScale, _NoiseDetail, _NoiseRoughness, _NoiseDistortion, 
                    _NoiseStrength, distortedNormalOS
                );
                float3 distortedNormalWS = TransformObjectToWorldNormal(distortedNormalOS);
                distortedNormalWS = SafeNormalize(distortedNormalWS);

                // 3. Get Main Light
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                // 4. Complex Watercolor Lighting
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                clip(baseTex.a - _Cutoff);

                // Base texture vs palette control
                float3 baseForLighting;
                float detailMul;
                WatercolorComputeBase_float(baseTex.rgb, _BaseColor.rgb, _WC_PaletteStrength, _WC_BaseDetailStrength, baseForLighting, detailMul);
                
                float3 watercolorColor;
                float3 viewDirWS = SafeNormalize(input.viewDirWS);

                // Debug intermediates
                float dbgLightIntensity;
                float dbgRampAFactor;
                float dbgEdgeMask;
                float3 dbgRampB;
                float3 dbgEdgeColor;
                float3 dbgRaw;
                float3 dbgTone;

                WatercolorLightingComplex_Debug_float(
                    baseForLighting,
                    distortedNormalWS,
                    viewDirWS,
                    mainLight.direction,
                    mainLight.color,
                    mainLight.shadowAttenuation,
                    _RampLightingA, sampler_RampLightingA,
                    _RampLightingB, sampler_RampLightingB,
                    _RampEdgeA, sampler_RampEdgeA,
                    _RampEdgeB, sampler_RampEdgeB,
                    _RampEdgeCol, sampler_RampEdgeCol,
                    _LayerBlend,
                    dbgLightIntensity,
                    dbgRampAFactor,
                    dbgEdgeMask,
                    dbgRampB,
                    dbgEdgeColor,
                    dbgRaw,
                    dbgTone
                );

                watercolorColor = dbgTone * detailMul;

                // (Inner line/pigment removed — now handled by front overlay pass)

                // 5. Paper Texture
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                screenUV.x *= _ScreenParams.x / _ScreenParams.y;

                float3 finalColor;
                WatercolorPaper_float(
                    watercolorColor,
                    screenUV,
                    _PaperTex, sampler_PaperTex,
                    _PaperTiling,
                    _PaperStrength,
                    finalColor
                );

                // Debug views (material inspector)
                if (_WC_DebugView > 0.5)
                {
                    // 1 BaseMap
                    if (_WC_DebugView < 1.5) return float4(baseTex.rgb, 1);
                    // 2 LightIntensity
                    if (_WC_DebugView < 2.5) return float4(dbgLightIntensity.xxx, 1);
                    // 3 RampB
                    if (_WC_DebugView < 3.5) return float4(dbgRampB, 1);
                    // 4 EdgeMask
                    if (_WC_DebugView < 4.5) return float4(dbgEdgeMask.xxx, 1);
                    // 5 EdgeColor
                    if (_WC_DebugView < 5.5) return float4(dbgEdgeColor, 1);
                    // 6 Raw
                    if (_WC_DebugView < 6.5) return float4(dbgRaw, 1);
                    // 7 Tonemapped
                    if (_WC_DebugView < 7.5) return float4(dbgTone, 1);
                    // 8 Final
                    return float4(finalColor, 1);
                }

                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return float4(finalColor, baseTex.a);
            }
            ENDHLSL
        }

        // Inner line (front overlay) pass: draws an alpha-textured layer on the *front faces* only.
        // Controlled by the same material checkbox-style toggle the legacy stack used: _UseInnerOutline.
        Pass
        {
            Name "InnerLineFront"
            Tags { "LightMode" = "InnerLineFront" }

            ZWrite Off

            // Inner-outline style: draw an expanded shell ONLY where it is behind the original surface.
            // This prevents drawing outside the silhouette.
            ZTest Greater
            Offset 1, 1

            Cull Back

            // Alpha overlay on top of the lit pass
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vertInner
            #pragma fragment fragInner
            #pragma target 4.5
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float4 screenPos : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float _UseInnerOutline;
                float _InnerOutlineAlpha;
                float4 _InnerOutlineColor;
                float _InnerOutlineTexTiling;
                float _InnerOutlineWidth;

                float _InnerOutlineDepthWidth;
                float _InnerOutlineThreshold;
                float _InnerOutlineSmoothness;
            CBUFFER_END

            TEXTURE2D(_InnerOutlineTex); SAMPLER(sampler_InnerOutlineTex);

            Varyings vertInner(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);

                float3 nWS = SafeNormalize(nrm.normalWS);
                float3 pWS = pos.positionWS + nWS * _InnerOutlineWidth;

                o.positionCS = TransformWorldToHClip(pWS);
                o.positionWS = pWS;
                o.normalWS = nWS;
                o.uv = input.uv;
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float4 fragInner(Varyings input) : SV_Target
            {
                // Checkbox off → do nothing
                clip(_UseInnerOutline - 0.5);

                // Silhouette-distance mask (depth difference between the current shell pixel and the scene depth)
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                float sceneRaw = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float currEye = LinearEyeDepth(input.positionCS.z, _ZBufferParams);

                // Positive when this shell pixel is behind the original surface.
                float d = max(0.0, currEye - sceneEye);

                // Band near silhouette: strong when depth difference is small, fades as it goes deeper.
                float width = max(_InnerOutlineDepthWidth, 1e-5);
                float baseMask = 1.0 - saturate(d / width);

                float s = max(_InnerOutlineSmoothness, 1e-5);
                float mask = smoothstep(_InnerOutlineThreshold - s, _InnerOutlineThreshold + s, baseMask);

                // Texture (RGBA), scaled tiling
                float2 uv = input.uv * max(_InnerOutlineTexTiling, 0.0001);
                float4 t = SAMPLE_TEXTURE2D(_InnerOutlineTex, sampler_InnerOutlineTex, uv);

                float a = saturate(_InnerOutlineAlpha) * mask * t.a;
                float3 col = t.rgb * _InnerOutlineColor.rgb;

                col = MixFog(col, input.fogFactor);
                return float4(col, a);
            }
            ENDHLSL
        }
        
        // Stencil-only pass for screen-space inner edge (masked PP)
        Pass
        {
            Name "WCStencil"
            Tags { "LightMode" = "WCStencil" }

            ZWrite Off
            ZTest LEqual
            Cull Back
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            float4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
