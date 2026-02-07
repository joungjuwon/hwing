Shader "Hwing/SimpleWatercolorCloud_Smooth"
{
    Properties
    {
        [Header(Base Settings)]
        _BaseColor ("Cloud Color", Color) = (1, 1, 1, 0.8)
        
        [Header(Shape Settings)]
        _WobbleSpeed ("Wobble Speed", Float) = 0.5
        _WobbleScale ("Wobble Scale (Base Size)", Float) = 1.5
        _WobbleAmount ("Wobble Amount (Strength)", Float) = 0.3
        
        [Header(Watercolor Effect)]
        _RimPower ("Rim Power (Softness)", Range(0.1, 5.0)) = 2.0
        _RimColor ("Rim Color", Color) = (0.9, 0.95, 1.0, 0.5)
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
        ZWrite Off
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
                float3 posWS : TEXCOORD2; // 프래그먼트에서도 월드 좌표 사용
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _WobbleSpeed;
                float _WobbleScale;
                float _WobbleAmount;
                float _RimPower;
                float4 _RimColor;
            CBUFFER_END

            // === [핵심 수정] 부드러운 노이즈 함수 추가 ===
            // 여러 개의 sin/cos 파동을 겹쳐서 뾰족함을 줄입니다.
            float GetSmoothWave(float3 posWS, float time)
            {
                float scale = _WobbleScale;
                
                // 레이어 1: 기본이 되는 크고 느린 움직임
                float wave1 = sin(posWS.x * scale + time) 
                            + cos(posWS.z * scale * 0.8 + time * 0.9);
                            
                // 레이어 2: 디테일을 더하고 각진 부분을 뭉개는 작고 빠른 움직임
                // (스케일은 키우고, 영향력은 줄임)
                float wave2 = sin(posWS.y * scale * 2.5 + time * 1.7) * 0.5 
                            + cos(posWS.x * scale * 3.1 + time * 2.1) * 0.5;

                // 두 레이어를 합치고 정규화
                return (wave1 + wave2) * 0.5;
            }
            // ==============================================

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 1. 월드 포지션 기준점 계산
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.posWS = positionWS; // 프래그먼트로 전달

                // 2. 시간 계산
                float time = _Time.y * _WobbleSpeed;

                // 3. [수정됨] 부드러운 웨이브 함수 호출
                float smoothWave = GetSmoothWave(positionWS, time);
                
                // 4. 버텍스 위치 변형
                // 노멀 방향으로 밀어내되, 너무 날카롭지 않게 약간의 곡률을 줍니다.
                float3 nudgeDir = normalize(IN.normalOS + float3(0, 0.2, 0)); // 약간 위쪽으로 부풀게 보정
                float3 wobbleOffset = nudgeDir * smoothWave * _WobbleAmount;
                
                float3 modifiedPositionOS = IN.positionOS.xyz + wobbleOffset;

                // 5. 최종 위치 계산
                OUT.positionCS = TransformObjectToHClip(modifiedPositionOS);
                
                // 6. 조명/림라이트 계산용 데이터 전달
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // 림 라이트 (수채화 외곽선)
                float NdotV = 1.0 - saturate(dot(normal, viewDir));
                float rimIntensity = pow(NdotV, _RimPower);

                // 색상 합성
                half4 finalColor = lerp(_BaseColor, _RimColor, rimIntensity);
                
                // [추가 팁] 중앙 부분의 투명도를 약간 낮춰 더 몽글해 보이게 함
                finalColor.a *= lerp(0.6, 1.0, rimIntensity); 

                return finalColor;
            }
            ENDHLSL
        }
    }
}