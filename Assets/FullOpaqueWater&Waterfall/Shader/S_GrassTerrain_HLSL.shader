Shader "Shader Graphs/S_GrassTerrain_HLSL"
{
    Properties
    {
        _Grass_Shadow ("Grass_Shadow", Color) = (0.18039216, 0.35686275, 0.29411766, 1.0)
        _Grass_MidTone ("Grass_MidTone", Color) = (0.25490198, 0.44705883, 0.09019608, 1.0)
        _Grass_HighLight ("Grass_HighLight", Color) = (0.53333336, 0.62352943, 0.050980393, 1.0)
        _Grass_Dry ("Grass_Dry", Color) = (0.58431375, 0.38039216, 0.23529412, 1.0)

        _Cliff_SlopeAngle ("Cliff_SlopeAngle", Range(0, 1)) = 0.753000021
        _Mountain_ProceduralHeight ("Mountain_ProceduralHeight", Float) = 180.0
        _Mountain_ProceduralSmooth ("Mountain_ProceduralSmooth", Float) = 10.0
        _Grass_BlendHeight ("Grass_BlendHeight", Float) = 13.6000004
        _Grass_BlendHeightSmooth ("Grass_BlendHeightSmooth", Float) = 6.0
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
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3)
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Grass_Shadow;
                half4 _Grass_MidTone;
                half4 _Grass_HighLight;
                half4 _Grass_Dry;

                float _Cliff_SlopeAngle;
                float _Mountain_ProceduralHeight;
                float _Mountain_ProceduralSmooth;
                float _Grass_BlendHeight;
                float _Grass_BlendHeightSmooth;
            CBUFFER_END

            float RemapFloat(float inValue, float2 inMinMax, float2 outMinMax)
            {
                return outMinMax.x + (inValue - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, float4(1.0, 0.0, 0.0, 1.0));

                output.positionCS = positionInputs.positionCS;
                output.positionWS = GetAbsolutePositionWS(positionInputs.positionWS);
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);

                half slopeBase = saturate(RemapFloat(normalWS.y, float2(_Cliff_SlopeAngle, 1.0), float2(0.0, 0.99)));
                half shadowToMid = saturate(RemapFloat(slopeBase, float2(0.0, 0.5), float2(0.0, 1.0)));
                half midToHigh = saturate(RemapFloat(slopeBase, float2(0.5, 1.0), float2(0.0, 1.0)));

                half3 grassTone = saturate(lerp(
                    lerp(_Grass_Shadow.rgb, _Grass_MidTone.rgb, shadowToMid),
                    _Grass_HighLight.rgb,
                    midToHigh));

                half mountainBlend = saturate(((((input.positionWS.y - (_Mountain_ProceduralHeight - (2.0 * _Mountain_ProceduralSmooth))) +
                    (1.0 - (20.0 * normalWS.y))) / _Mountain_ProceduralSmooth) +
                    _Grass_BlendHeight) / _Grass_BlendHeightSmooth);

                half3 baseColor = lerp(grassTone, _Grass_Dry.rgb, mountainBlend);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = VertexLighting(input.positionWS, normalWS);
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor;
                surfaceData.alpha = 1.0h;
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.smoothness = 0.0h;
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
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

