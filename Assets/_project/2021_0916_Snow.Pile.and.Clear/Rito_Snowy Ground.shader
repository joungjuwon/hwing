Shader "Rito/URP_SnowyGround"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _ColorIntensity("Color Intensity", Range(0, 5)) = 1
        _HeightMultiplier("Height Multiplier", Range(0, 5)) = 0.5
        // _EdgeLength는 코드 기반 URP 쉐이더에서는 구현이 복잡하여 제외되었으나, 
        // 고해상도 메쉬를 사용하거나 쉐이더 그래프를 사용하는 것을 권장합니다.
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry" 
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _HeightMultiplier;
            float _ColorIntensity;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float3 normalWS : NORMAL;
        };

        // 버텍스 변형 함수 (높이 맵 적용)
        float3 ApplyDisplacement(float3 positionOS, float2 uv)
        {
            // Vertex 단계에서는 텍스처의 MipMap 레벨 0을 샘플링해야 함
            float height = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).r;
            // Y축으로 높이 적용
            return positionOS + float3(0, height * _HeightMultiplier, 0);
        }
        ENDHLSL

        // ------------------------------------------------------------------
        //  Forward Pass (메인 렌더링)
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float2 uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // 높이 맵 적용
                float3 displacedPosOS = ApplyDisplacement(input.positionOS.xyz, uv);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(displacedPosOS);
                
                output.uv = uv;
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 기본 라이팅 계산 (간단한 Lambert)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                float3 lighting = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation * NdotL);
                
                // Ambient(환경광) 추가
                lighting += SampleSH(input.normalWS);

                // 최종 색상 = 텍스처 * 강도 * 라이팅
                float3 finalColor = texColor.rgb * _ColorIntensity * lighting;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        //  Shadow Caster Pass (그림자용 패스 - 변형된 버텍스 반영)
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings vert(ShadowAttributes input)
            {
                ShadowVaryings output;
                
                float2 uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // 그림자 패스에서도 동일하게 버텍스를 변형해야 그림자가 메쉬와 일치함
                float3 displacedPosOS = ApplyDisplacement(input.positionOS.xyz, uv);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(displacedPosOS);
                output.positionCS = vertexInput.positionCS;

                return output;
            }

            half4 frag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}