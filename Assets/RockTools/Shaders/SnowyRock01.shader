Shader "Shader Forge/SnowyRock01"
{
    Properties
    {
        _Rock_Top_Color ("Rock_Top_Color", Color) = (1,0.7921569,0.4392157,1)
        _Rock_Bottom_Color ("Rock_Bottom_Color", Color) = (0.7254902,0.4156863,0.06666667,1)
        _Rock_Color_Pos ("Rock_Color_Pos", Range(0, 1)) = 0.8
        _Rock_Color_Smooth ("Rock_Color_Smooth", Range(0, 1)) = 0.3
        [NoScaleOffset]_AO_Texture ("AO_Texture", 2D) = "bump" {}
        _AO_Intensity ("AO_Intensity", Range(0, 1)) = 0.7
        [NoScaleOffset]_Normal_Map ("Normal_Map", 2D) = "white" {}
        _Snow_Color ("Snow_Color", Color) = (1,0.8745099,0.6666667,1)
        _Snow_Dir_X ("Snow_Dir_X", Range(-1, 1)) = 0
        _Snow_Dir_Y ("Snow_Dir_Y", Range(-1, 1)) = 1
        _Snow_Dir_Z ("Snow_Dir_Z", Range(-1, 1)) = 0
        _Snow_Amount ("Snow_Amount", Range(0, 0.4)) = 0.1
        _Snow_Edge_Smooth ("Snow_Edge_Smooth", Range(0.005, 0.15)) = 0.08
        _UV_Tile ("UV_Tile", Range(0, 1)) = 0.08
        _UV_Offset ("UV_Offset", Range(0, 10)) = 0
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
                float4 _Rock_Top_Color;
                float4 _Rock_Bottom_Color;
                float _Rock_Color_Pos;
                float _Rock_Color_Smooth;
                float _AO_Intensity;
                float4 _Snow_Color;
                float _Snow_Dir_X;
                float _Snow_Dir_Y;
                float _Snow_Dir_Z;
                float _Snow_Amount;
                float _Snow_Edge_Smooth;
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
                float3 weights = RockGetTriplanarWeightsSimple(baseNormalWS);

                float3 aoRGB = RockSampleTriplanarRGB(TEXTURE2D_ARGS(_AO_Texture, sampler_AO_Texture), scaledWorldPos, weights);
                float ao = RockComputeAO(aoRGB, _AO_Intensity);

                float3 normalRGB = RockSampleTriplanarRGB(TEXTURE2D_ARGS(_Normal_Map, sampler_Normal_Map), scaledWorldPos, weights);
                float3 normalWS = RockBuildNormalWS(input, normalRGB, 1.0);

                float rockBand = smoothstep(
                    _Rock_Color_Pos - _Rock_Color_Smooth,
                    _Rock_Color_Pos + _Rock_Color_Smooth,
                    saturate(input.color.r));
                float3 rockColor = lerp(_Rock_Bottom_Color.rgb, _Rock_Top_Color.rgb, rockBand);
                float3 albedoBase = rockColor * ao;

                float3 snowDir = normalize(float3(_Snow_Dir_X, _Snow_Dir_Y, _Snow_Dir_Z) + float3(1e-4, 1e-4, 1e-4));
                float slope = saturate(dot(baseNormalWS, snowDir));
                float snowStart = 1.0 - (_Snow_Amount + _Snow_Edge_Smooth);
                float snowEnd = 1.0 - max(_Snow_Amount - _Snow_Edge_Smooth, 1e-4);
                float snowMask = smoothstep(snowStart, snowEnd, slope);

                float3 albedo = lerp(albedoBase, _Snow_Color.rgb, snowMask);
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
