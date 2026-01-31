// WatercolourFunc.hlsl

// 함수 이름 뒤에 _float를 붙이는 건 쉐이더 그래프 규칙입니다.
void CalculateWatercolour_float(
    // [입력 변수들]
    float3 BaseColor,
    float3 ShadowColor,
    float3 DeepShadowColor,
    
    float3 NormalWS,        // 월드 노말
    float3 ViewDirWS,       // 카메라 보는 방향
    float3 MainLightDir,    // 주 조명 방향
    float3 MainLightColor,  // 주 조명 색상
    float MainLightShadowAtten, // 그림자 감쇠값
    
    float NoiseVal,         // 노이즈 텍스처의 R값 (0~1)
    float3 ShadowPattern,   // 쉐도우 패턴 텍스처의 RGB
    float3 DeepShadowPattern, // 딥 쉐도우 패턴 텍스처의 RGB
    
    // 각종 설정값들
    float NoiseStrength,
    float NoiseBrighten,
    float ShadowStrength,
    float DeepShadowStrength,
    float DeepShadowSpread,
    float DeepShadowFalloff,
    float ShadowThreshold,
    float ShadowSmoothness,
    float SpecularStrength,
    float Glossiness,
    float FresnelPower,
    float FresnelThreshold,
    float FresnelSmoothness,
    float FresnelAmount,

    // [출력 변수] out 키워드 필수
    out float3 FinalColor
)
{
    // 1. 기본 라이팅 계산 (NdotL)
    float NdotL = dot(NormalWS, MainLightDir);
    
    // 2. 노이즈를 더해 울퉁불퉁한 경계면 만들기
    float noisyNdotL = NdotL + (NoiseVal - 0.5) * NoiseStrength;

    // 3. 텍스처와 색상 섞기 (Lerp)
    float3 shadowTexMixed = lerp(float3(1,1,1), ShadowPattern, ShadowStrength);
    float3 effectiveShadow = ShadowColor * shadowTexMixed;
    
    float3 deepShadowTexMixed = lerp(float3(1,1,1), DeepShadowPattern, DeepShadowStrength);
    float3 effectiveDeepShadow = DeepShadowColor * deepShadowTexMixed;

    // 4. 전체적인 음영 그라데이션 (Global Shading)
    float gradientInput = noisyNdotL + DeepShadowSpread;
    float remappedGradient = saturate(gradientInput * 0.5 + 0.5);
    float globalShading = pow(remappedGradient, DeepShadowFalloff);

    // 5. 셀 쉐이딩 단계 (Step)
    float shadowFactor = smoothstep(ShadowThreshold - ShadowSmoothness, ShadowThreshold + ShadowSmoothness, noisyNdotL);

    // 6. 유니티 그림자(Cast Shadow) 적용
    // 그림자가 지는 곳(Atten=0)은 강제로 어둡게 만듦
    shadowFactor = min(shadowFactor, MainLightShadowAtten);
    globalShading = min(globalShading, MainLightShadowAtten);

    // 7. 색상 합성
    // 밝은 부분 vs 중간 그림자
    float3 celColor = lerp(effectiveShadow, BaseColor, shadowFactor);
    // 중간 그림자 vs 깊은 그림자
    float3 mixedBase = lerp(effectiveDeepShadow, celColor, globalShading);

    // 8. 종이 질감(노이즈) 밝기 추가
    float3 colorWithNoise = mixedBase + (NoiseVal - 0.5) * NoiseBrighten * 0.5;

    // 9. 스펙큘러 (반짝임)
    float3 halfDir = normalize(MainLightDir + ViewDirWS);
    float NdotH = saturate(dot(NormalWS, halfDir));
    float specular = pow(NdotH, Glossiness * 128.0);
    float3 specularOut = specular * SpecularStrength; // 단순화: 흰색 스펙큘러

    // 10. 프레넬 (가장자리 빛)
    float NdotV = saturate(dot(NormalWS, ViewDirWS));
    float fresnelBase = pow(1.0 - NdotV, FresnelPower);
    float fresnel = smoothstep(FresnelThreshold, FresnelThreshold + FresnelSmoothness, fresnelBase);
    float3 fresnelOut = fresnel * FresnelAmount * BaseColor;

    // 최종 결과 출력
    FinalColor = colorWithNoise + specularOut + fresnelOut;
}