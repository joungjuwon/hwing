Shader "Shader Graphs/S_Vegetation_Interactive_HLSL"
{
    Properties
    {
        _FadeOutDistance ("FadeOutDistance", Float) = 100

        _Grass_HighLight ("Grass_HighLight", Color) = (0.53333336, 0.62352943, 0.050980393, 1)
        _Grass_MidTone ("Grass_MidTone", Color) = (0.25490198, 0.44705883, 0.09019608, 1)
        _Grass_Shadow ("Grass_Shadow", Color) = (0.18039216, 0.35686275, 0.29411766, 1)
        _Grass_Dry ("Grass_Dry", Color) = (0.58431375, 0.38039216, 0.23529412, 1)

        _Cliff_SlopeAngle ("Cliff_SlopeAngle", Range(0, 1)) = 0.753000021
        _Grass_BlendHeight ("Grass_BlendHeight", Float) = 13.6000004
        _Grass_BlendHeightSmooth ("Grass_BlendHeightSmooth", Float) = 6

        _Mountain_ProceduralHeight ("Mountain_ProceduralHeight", Float) = 180
        _Mountain_ProceduralSmooth ("Mountain_ProceduralSmooth", Float) = 10

        _FakeLightColor ("FakeLightColor", Color) = (1, 0.9166667, 0, 0)
        _FakeShadowColor ("FakeShadowColor", Color) = (0, 0.6666667, 1, 1)
        _FakeLightStrength ("FakeLightStrength", Float) = 0.5

        _TargetTurbulenceSize ("TargetTurbulenceSize", Float) = 1
        _InteractionHorizontalPush ("Interaction Horizontal Push", Range(0, 2)) = 0.8
        _InteractionVerticalPush ("Interaction Vertical Push", Range(0, 1)) = 0.45
        _InteractionWindSuppression ("Interaction Wind Suppression", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv0 : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                                #if defined(LIGHTMAP_ON)
                float2 lightmapUV : TEXCOORD4;
                #else
                half3 vertexSH : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _FakeLightColor;
                half4 _FakeShadowColor;

                half4 _Grass_Dry;
                half4 _Grass_HighLight;
                half4 _Grass_MidTone;
                half4 _Grass_Shadow;

                float _Cliff_SlopeAngle;
                float _FadeOutDistance;
                float _FakeLightStrength;
                float _Grass_BlendHeight;
                float _Grass_BlendHeightSmooth;
                float _InteractionHorizontalPush;
                float _InteractionVerticalPush;
                float _InteractionWindSuppression;
                float _Mountain_ProceduralHeight;
                float _Mountain_ProceduralSmooth;
                float _TargetTurbulenceSize;
            CBUFFER_END

            float4 _TargetTurbulencePose1;
            float4 _TargetTurbulencePose2;
            float4 _TargetTurbulencePose3;
            float4 _TargetTurbulencePose4;
            float4 _TargetTurbulencePose5;
            float4 _WindDirection;
            float _WindSize;
            float _WindSpeed;
            float _WindStrength;

            float SGSimpleNoiseRandom(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float SGSimpleNoiseInterpolate(float a, float b, float t)
            {
                return (1.0 - t) * a + (t * b);
            }

            float SGSimpleNoiseValue(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float2 c0 = i + float2(0.0, 0.0);
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);

                float r0 = SGSimpleNoiseRandom(c0);
                float r1 = SGSimpleNoiseRandom(c1);
                float r2 = SGSimpleNoiseRandom(c2);
                float r3 = SGSimpleNoiseRandom(c3);

                float bottom = SGSimpleNoiseInterpolate(r0, r1, f.x);
                float top = SGSimpleNoiseInterpolate(r2, r3, f.x);
                return SGSimpleNoiseInterpolate(bottom, top, f.y);
            }

            float SGSimpleNoise(float2 uv, float scale)
            {
                float t = 0.0;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float freq = pow(2.0, (float)i);
                    float amp = pow(0.5, (float)(3 - i));
                    t += SGSimpleNoiseValue((uv * scale) / freq) * amp;
                }
                return t;
            }

            float SGBlendMode1(float baseValue, float blendValue, float opacity)
            {
                return lerp(baseValue, min(baseValue, blendValue), opacity);
            }

            float SGOverlayCore(float baseValue, float blendValue)
            {
                return baseValue <= 0.5 ? (2.0 * baseValue * blendValue) : (1.0 - 2.0 * (1.0 - baseValue) * (1.0 - blendValue));
            }

            float SGBlendMode15(float baseValue, float blendValue, float opacity)
            {
                return lerp(baseValue, SGOverlayCore(baseValue, blendValue), opacity);
            }

            float4 SGBlendMode15(float4 baseValue, float4 blendValue, float opacity)
            {
                float4 low = 2.0 * baseValue * blendValue;
                float4 high = 1.0 - 2.0 * (1.0 - baseValue) * (1.0 - blendValue);
                float4 overlay = lerp(high, low, step(baseValue, 0.5));
                return lerp(baseValue, overlay, opacity);
            }

            float SGRemap(float inValue, float2 inMinMax, float2 outMinMax)
            {
                return outMinMax.x + (inValue - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
            }

            float SGSegmentTrailValue(float3 p, float3 a, float3 b, float offset)
            {
                float3 ab = b - a;
                float abLenSq = max(dot(ab, ab), 0.0001);
                float t = saturate(dot(p - a, ab) / abLenSq);
                float dist = length((p - a) - (t * ab));
                return dist + (t / 4.0) + offset;
            }

            float ComputeTrailMask(float3 positionWS)
            {
                float s12 = SGSegmentTrailValue(positionWS, _TargetTurbulencePose1.xyz, _TargetTurbulencePose2.xyz, 0.0);
                float s23 = SGSegmentTrailValue(positionWS, _TargetTurbulencePose2.xyz, _TargetTurbulencePose3.xyz, 0.25);
                float s34 = SGSegmentTrailValue(positionWS, _TargetTurbulencePose3.xyz, _TargetTurbulencePose4.xyz, 0.5);
                float s45 = SGSegmentTrailValue(positionWS, _TargetTurbulencePose4.xyz, _TargetTurbulencePose5.xyz, 0.75);

                float s0 = SGBlendMode1(s12, s23, 1.0);
                float s1 = SGBlendMode1(s34, s45, 1.0);
                float s = SGBlendMode1(s0, s1, 1.0);

                return saturate(1.0 - (s / _TargetTurbulenceSize));
            }

            float ComputeWindNoiseBase(float3 positionWS)
            {
                float2 windUV = float2(positionWS.x, positionWS.z) * _WindSize;
                float2 windOffset = (_WindSpeed * _Time.y) * (-_WindDirection.xy);
                return SGSimpleNoise(windUV + windOffset, 1.0);
            }

            float ComputeWindNoiseVertex(float3 positionWS, float uvY)
            {
                float2 windUV = float2(positionWS.x, positionWS.z) * _WindSize;
                float2 windOffset = ((_WindSpeed * _Time.y) + (1.0 - uvY)) * (-_WindDirection.xy);
                return SGSimpleNoise(windUV + windOffset, 1.0);
            }

            float ComputeInteractionMask(float uvY, float trailMask)
            {
                float tipMask = uvY * uvY;
                return saturate(trailMask * tipMask);
            }

            float3 ComputeInteractivePushWS(float3 positionOS, float uvY, float trailMask)
            {
                float interaction = ComputeInteractionMask(uvY, trailMask);

                float2 radialOS = positionOS.xz;
                float radialLen = max(length(radialOS), 0.0001);
                float2 radialDirOS = radialOS / radialLen;
                float3 radialDirWS = TransformObjectToWorldDir(float3(radialDirOS.x, 0.0, radialDirOS.y), true);

                float horizontalPush = interaction * _TargetTurbulenceSize * _InteractionHorizontalPush;
                float verticalPush = interaction * _InteractionVerticalPush;

                return (radialDirWS * horizontalPush) + float3(0.0, -verticalPush, 0.0);
            }

            float3 EvaluateVertexPositionOS(float3 positionOS, float3 positionWS, float2 uv0)
            {
                float uvY = uv0.y;
                float trailMask = ComputeTrailMask(positionWS);
                float interactionMask = ComputeInteractionMask(uvY, trailMask);

                float noise = ComputeWindNoiseVertex(positionWS, uvY);
                float swayScalar = (noise - 0.30000001192092896) * _WindStrength;
                float windFadeByInteraction = 1.0 - (interactionMask * _InteractionWindSuppression);
                float swayStrength = swayScalar * uvY * windFadeByInteraction;

                float3 swayDirection = float3(_WindDirection.x, -1.0, _WindDirection.y);
                float3 displacedWS = positionWS + (swayDirection * swayStrength);
                displacedWS += ComputeInteractivePushWS(positionOS, uvY, trailMask);

                return TransformWorldToObject(displacedWS);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 initialPositionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 finalPositionOS = EvaluateVertexPositionOS(input.positionOS.xyz, initialPositionWS, input.uv0);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(finalPositionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, float4(1.0, 0.0, 0.0, 1.0));

                output.positionCS = positionInputs.positionCS;
                output.positionWS = GetAbsolutePositionWS(positionInputs.positionWS);
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.uv0 = input.uv0;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                return output;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half3 normalWS = NormalizeNormalPerPixel(IN.normalWS);

                float trailMask = ComputeTrailMask(IN.positionWS);

                float windNoise = ComputeWindNoiseBase(IN.positionWS);
                float fakeBase = saturate((1.0 - windNoise) - trailMask);
                float fakeLightT = saturate(fakeBase + (IN.uv0.y * _FakeLightStrength));

                float4 fakeLightColor = lerp(_FakeShadowColor, _FakeLightColor, fakeLightT);
                float overlayOpacity = saturate(trailMask + (IN.uv0.y * _FakeLightStrength));

                float bladeGradient = saturate(IN.uv0.y);
                float4 terrainColor = lerp(_Grass_MidTone, _Grass_HighLight, bladeGradient * bladeGradient);

                float4 blendedColor = SGBlendMode15(terrainColor, fakeLightColor, overlayOpacity);

                float smoothControl = saturate(fakeBase - 0.5);
                float smoothness = smoothControl * smoothControl;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.vertexLighting = VertexLighting(IN.positionWS, normalWS);
                inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUV);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = blendedColor.rgb;
                surfaceData.alpha = 1.0h;
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                Light mainLight = GetMainLight(inputData.shadowCoord);
                const half shadowWeakening = 0.25h;
                half shadowAtten = max(mainLight.shadowAttenuation, 0.001h);
                half shadowLift = (1.0h - shadowAtten) * shadowWeakening;
                half shadowScale = lerp(1.0h, rcp(shadowAtten), shadowLift);
                shadowScale = min(shadowScale, 1.35h);
                color.rgb *= shadowScale;
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    Fallback Off
}


