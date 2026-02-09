Shader "Shader Forge/CrystalRock"
{
    Properties
    {
        _Color_1 ("Color_1", Color) = (0.5,0.5,0.5,1)
        _Color_2 ("Color_2", Color) = (0.5,0.5,0.5,1)
        _Color_3 ("Color_3", Color) = (0.5,0.5,0.5,1)
        _Color_4 ("Color_4", Color) = (0.5,0.5,0.5,1)
        _Top_Layer_Pos ("Top_Layer_Pos", Range(0, 1)) = 0.05
        _Top_Layer_Smooth ("Top_Layer_Smooth", Range(0, 1)) = 0
        _Rock_Color_Pos ("Rock_Color_Pos", Range(0, 1)) = 0.85
        _Rock_Color_Smooth ("Rock_Color_Smooth", Range(0, 1)) = 0.1
        [NoScaleOffset]_AO_Texture ("AO_Texture", 2D) = "bump" {}
        _AO_Intensity ("AO_Intensity", Range(0, 1)) = 0.7
        [NoScaleOffset]_Normal_Map ("Normal_Map", 2D) = "white" {}
        _Normal_Map_Intensity ("Normal_Map_Intensity", Range(0, 1)) = 0.5
        _Top_Color_Blend ("Top_Color_Blend", Range(0, 1.5)) = 1.5
        _Proj_Mask_Smooth ("Proj_Mask_Smooth", Range(0.2, 0.7)) = 0.7
        _Proj_Mask_Threshold ("Proj_Mask_Threshold", Range(0.6, 0.8)) = 0.8
        _UV_Tile ("UV_Tile", Range(0, 10)) = 0
        _UV_Offset ("UV_Offset", Range(0, 7)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex RockVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Assets/RockTools/Shaders/RockToolsURPCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color_1;
                float4 _Color_2;
                float4 _Color_3;
                float4 _Color_4;
                float _Top_Layer_Pos;
                float _Top_Layer_Smooth;
                float _Rock_Color_Pos;
                float _Rock_Color_Smooth;
                float _AO_Intensity;
                float _Normal_Map_Intensity;
                float _Top_Color_Blend;
                float _Proj_Mask_Smooth;
                float _Proj_Mask_Threshold;
                float _UV_Tile;
                float _UV_Offset;
            CBUFFER_END

            TEXTURE2D(_AO_Texture);
            SAMPLER(sampler_AO_Texture);
            TEXTURE2D(_Normal_Map);
            SAMPLER(sampler_Normal_Map);

            half4 frag(RockVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 baseNormalWS = normalize(input.normalWS);
                float3 scaledWorldPos = RockScaleWorldPos(input.positionWS, _UV_Tile, _UV_Offset);
                float3 weights = RockGetTriplanarWeightsSmooth(baseNormalWS, _Proj_Mask_Threshold, _Proj_Mask_Smooth);

                float3 aoRGB = RockSampleTriplanarRGB(TEXTURE2D_ARGS(_AO_Texture, sampler_AO_Texture), scaledWorldPos, weights);
                float ao = RockComputeAO(aoRGB, _AO_Intensity);

                float3 normalRGB = RockSampleTriplanarRGB(TEXTURE2D_ARGS(_Normal_Map, sampler_Normal_Map), scaledWorldPos, weights);
                float3 normalWS = RockBuildNormalWS(input, normalRGB, _Normal_Map_Intensity);

                float rockBand = smoothstep(
                    _Rock_Color_Pos - _Rock_Color_Smooth,
                    _Rock_Color_Pos + _Rock_Color_Smooth,
                    saturate(input.color.r));

                float3 middleLayer = lerp(_Color_4.rgb, _Color_3.rgb, rockBand);
                float3 baseLayer = (_Color_2.rgb * ao) + middleLayer;

                float normalRef = dot(normalRGB, float3(0.333333, 0.333333, 0.333333));
                float topRef = saturate(input.color.g + normalRef * (_Top_Color_Blend * 0.1));
                float topStart = (1.0 - _Top_Layer_Pos) - _Top_Layer_Smooth;
                float topEnd = (1.0 - _Top_Layer_Pos) + _Top_Layer_Smooth;
                float topMask = smoothstep(topStart, topEnd, topRef);

                float3 albedo = lerp(baseLayer, _Color_1.rgb, topMask);
                half3 color = RockShade(albedo, normalWS, input.positionWS, input.shadowCoord);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}
