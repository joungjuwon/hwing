Shader "Hwing/SimpleWatercolorCloud_Solid"
{
    Properties
    {
        [Header(Base Settings)]
        _BaseColor ("Cloud Color", Color) = (0.3, 0.3, 0.35, 1) // 기본값을 약간 어두운 색으로
        
        [Header(Shape Settings)]
        _WobbleSpeed ("Wobble Speed", Float) = 0.5
        _WobbleScale ("Wobble Scale (Base Size)", Float) = 1.5
        _WobbleAmount ("Wobble Amount (Strength)", Float) = 0.3
        
        [Header(Watercolor Effect)]
        _RimPower ("Rim Power (Sharpness)", Range(0.1, 10.0)) = 3.0
        _RimColor ("Rim Color", Color) = (0.8, 0.8, 0.8, 1.0) // 림 컬러
        _RimIntensity ("Rim Intensity", Range(0.0, 1.0)) = 0.5 // [추가] 림 라이트 강도 조절
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        
        // [핵심 수정] ZWrite를 On으로 켜서 내부 겹침 현상을 해결합니다.
        // 단, 투명도(Alpha)가 섞이면서 약간의 경계가 보일 수 있으니 
        // 완벽한 투명보다는 '반투명한 고체' 느낌을 냅니다.
        ZWrite On 
        
        Cull Back

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 posWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _WobbleSpeed;
                float _WobbleScale;
                float _WobbleAmount;
                float _RimPower;
                float4 _RimColor;
                float _RimIntensity; // [추가]
            CBUFFER_END

            float GetSmoothWave(float3 posWS, float time)
            {
                float scale = _WobbleScale;
                float wave1 = sin(posWS.x * scale + time) + cos(posWS.z * scale * 0.8 + time * 0.9);
                float wave2 = sin(posWS.y * scale * 2.5 + time * 1.7) * 0.5 + cos(posWS.x * scale * 3.1 + time * 2.1) * 0.5;
                return (wave1 + wave2) * 0.5;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.posWS = positionWS;

                float time = _Time.y * _WobbleSpeed;
                float smoothWave = GetSmoothWave(positionWS, time);
                
                float3 nudgeDir = normalize(IN.normalOS + float3(0, 0.2, 0)); 
                float3 wobbleOffset = nudgeDir * smoothWave * _WobbleAmount;
                
                float3 modifiedPositionOS = IN.positionOS.xyz + wobbleOffset;

                OUT.positionCS = TransformObjectToHClip(modifiedPositionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // 림 라이트
                float NdotV = 1.0 - saturate(dot(normal, viewDir));
                float rimCalculation = pow(NdotV, _RimPower);
                
                // [수정] 림 강도 적용
                rimCalculation *= _RimIntensity;

                // 색상 합성
                // 림 라이트가 BaseColor 위에 덧씌워지는 느낌 (Additive가 아닌 Interpolation)
                half3 finalRGB = lerp(_BaseColor.rgb, _RimColor.rgb, rimCalculation);
                
                // 알파값
                // 림 부분은 조금 더 불투명하게, 안쪽은 BaseColor 알파 따라가기
                half finalAlpha = saturate(_BaseColor.a + rimCalculation);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}