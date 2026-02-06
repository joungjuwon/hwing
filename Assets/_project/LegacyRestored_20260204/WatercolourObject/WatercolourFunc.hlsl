// CleanWatercolour.hlsl

void CalculateWatercolour_float(
    // [그룹 1: 재질 속성]
    float3 BaseColor,
    float3 ShadowColor,
    float3 DeepShadowColor,
    float NoiseVal,             // (중요) Float 타입입니다. 텍스처의 R채널값
    float3 ShadowPattern,       // (중요) Vector3. 텍스처의 RGB값
    float3 DeepShadowPattern,   // (중요) Vector3. 텍스처의 RGB값

    // [그룹 2: 지형/라이팅 필수 데이터]
    float3 NormalWS,            // 월드 노말
    float3 ViewDirWS,           // 카메라 뷰 방향
    float3 MainLightDir,        // 주 조명 방향
    float MainLightShadowAtten, // (중요) Float. 유니티가 계산한 그림자 값

    // [그룹 3: 조절 슬라이더 (모두 Float)]
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

    // [최종 출력]
    out float3 FinalColor // (중요) Vector3로 내보냅니다.
)
{
    // 1. 기본 라이팅 (NdotL) 계산 + 노이즈로 표면 울퉁불퉁하게 왜곡
    float NdotL = dot(NormalWS, MainLightDir);
    float noisyNdotL = NdotL + (NoiseVal - 0.5) * NoiseStrength;

    // 2. 텍스처 믹싱 (강도에 따라 흰색~텍스처색 보간)
    float3 shadowTexMixed = lerp(float3(1,1,1), ShadowPattern, ShadowStrength);
    float3 effectiveShadow = ShadowColor * shadowTexMixed;
    float3 deepShadowTexMixed = lerp(float3(1,1,1), DeepShadowPattern, DeepShadowStrength);
    float3 effectiveDeepShadow = DeepShadowColor * deepShadowTexMixed;

    // 3. 그림자 영역 계산 (Global Shading & Cel Shading Step)
    float gradientInput = noisyNdotL + DeepShadowSpread;
    float remappedGradient = saturate(gradientInput * 0.5 + 0.5);
    float globalShading = pow(remappedGradient, DeepShadowFalloff);
    float shadowFactor = smoothstep(ShadowThreshold - ShadowSmoothness, ShadowThreshold + ShadowSmoothness, noisyNdotL);

    // 4. 유니티 그림자(Cast Shadow) 강제 적용
    shadowFactor = min(shadowFactor, MainLightShadowAtten);
    globalShading = min(globalShading, MainLightShadowAtten);

    // 5. 최종 색상 합성 (밝은곳 -> 중간그림자 -> 짙은그림자)
    float3 celColor = lerp(effectiveShadow, BaseColor, shadowFactor);
    float3 mixedBase = lerp(effectiveDeepShadow, celColor, globalShading);
    float3 colorWithNoise = mixedBase + (NoiseVal - 0.5) * NoiseBrighten * 0.5;

    // 6. 스펙큘러(반사) 및 프레넬(외곽선) 추가
    float3 halfDir = normalize(MainLightDir + ViewDirWS);
    float NdotH = saturate(dot(NormalWS, halfDir));
    float specular = pow(NdotH, Glossiness * 128.0) * SpecularStrength;
    
    float NdotV = saturate(dot(NormalWS, ViewDirWS));
    float fresnelBase = pow(1.0 - NdotV, FresnelPower);
    float fresnel = smoothstep(FresnelThreshold, FresnelThreshold + FresnelSmoothness, fresnelBase);
    float3 fresnelOut = fresnel * FresnelAmount * BaseColor;

    FinalColor = colorWithNoise + specular + fresnelOut;
}