Shader "Hwing/SimpleWatercolorCloud_Smooth"
{
    Properties
    {
        [Header(Base Settings)]
        _BaseColor ("Cloud Color", Color) = (1, 1, 1, 1) // 기본 알파를 1로 변경
        
        [Header(Shape Settings)]
        _WobbleSpeed ("Wobble Speed", Float) = 0.5
        _WobbleScale ("Wobble Scale (Base Size)", Float) = 1.5
        _WobbleAmount ("Wobble Amount (Strength)", Float) = 0.3
        
        [Header(Watercolor Effect)]
        _RimPower ("Rim Power (Softness)", Range(0.1, 5.0)) = 2.0
        _RimColor ("Rim Color", Color) = (0.9, 0.95, 1.0, 0.5)
        
        // [추가됨] 중심부 불투명도 조절 슬라이더 (1.0이면 완전히 불투명)
        _CenterOpacity ("Center Opacity", Range(0.0, 1.0)) = 0.9
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
                float3 posWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _WobbleSpeed;
                float _WobbleScale;
                float _WobbleAmount;
                float _RimPower;
                float4 _RimColor;
                float _CenterOpacity; // [추가됨] 변수 선언
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
                float rimIntensity = pow(NdotV, _RimPower);

                // 색상 합성
                half4 finalColor = lerp(_BaseColor, _RimColor, rimIntensity);
                
                // [수정됨] 투명도 조절 로직
                // 이전 코드: lerp(0.6, 1.0, rimIntensity) -> 강제로 0.6배 투명해짐
                // 수정 코드: lerp(_CenterOpacity, 1.0, rimIntensity) -> 설정값만큼만 투명해짐
                
                // Rim(가장자리)는 무조건 불투명(1.0)하게 유지하여 윤곽을 살리고,
                // Center(중심)는 _CenterOpacity 값(0~1)을 따릅니다.
                finalColor.a = _BaseColor.a * lerp(_CenterOpacity, 1.0, rimIntensity);

                return finalColor;
            }
            ENDHLSL
        }
    }
}