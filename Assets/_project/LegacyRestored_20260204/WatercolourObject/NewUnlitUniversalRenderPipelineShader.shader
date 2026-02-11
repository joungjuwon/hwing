Shader "Hwing/SimpleWatercolorCloud_Lightning"
{
    Properties
    {
        [Header(Base Settings)]
        _BaseColor ("Cloud Color", Color) = (0.3, 0.3, 0.35, 1)
        
        [Header(Shape Settings)]
        _WobbleSpeed ("Wobble Speed", Float) = 0.5
        _WobbleScale ("Wobble Scale", Float) = 1.5
        _WobbleAmount ("Wobble Amount", Float) = 0.3
        
        [Header(Watercolor Effect)]
        _RimPower ("Rim Power", Range(0.1, 10.0)) = 3.0
        _RimColor ("Rim Color", Color) = (0.8, 0.8, 0.8, 1.0)
        _RimIntensity ("Rim Intensity", Range(0.0, 1.0)) = 0.5

        [Header(Lightning Effect)]
        [HDR] _LightningColor ("Lightning Color", Color) = (1, 0.95, 0.8, 1) // HDR로 빛나게
        _LightningStrength ("Lightning Strength", Range(0.0, 1.0)) = 0.0     // 스크립트로 제어할 변수
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On 
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewDirWS : TEXCOORD1; float3 posWS : TEXCOORD2; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _WobbleSpeed; float _WobbleScale; float _WobbleAmount;
                float _RimPower; float4 _RimColor; float _RimIntensity;
                float4 _LightningColor;    // [추가]
                float _LightningStrength;  // [추가]
            CBUFFER_END

            float GetSmoothWave(float3 posWS, float time)
            {
                float scale = _WobbleScale;
                return (sin(posWS.x*scale+time) + cos(posWS.z*scale*0.8+time*0.9) + sin(posWS.y*scale*2.5+time*1.7)*0.5) * 0.3;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.posWS = positionWS;
                float time = _Time.y * _WobbleSpeed;
                float3 nudgeDir = normalize(IN.normalOS + float3(0, 0.2, 0));
                float3 wobbleOffset = nudgeDir * GetSmoothWave(positionWS, time) * _WobbleAmount;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz + wobbleOffset);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // 1. 기본 먹구름 렌더링
                float NdotV = 1.0 - saturate(dot(normal, viewDir));
                float rimCalculation = pow(NdotV, _RimPower) * _RimIntensity;
                half3 baseRGB = lerp(_BaseColor.rgb, _RimColor.rgb, rimCalculation);
                
                // 2. [추가] 번개 효과 (Additive blending)
                // LightningStrength가 높을수록 번개 색상을 더해줍니다.
                half3 lightningRGB = _LightningColor.rgb * _LightningStrength * 2.0; // 강도 2배 뻥튀기
                
                half3 finalRGB = baseRGB + lightningRGB; // 색상 더하기
                half finalAlpha = saturate(_BaseColor.a + rimCalculation);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}