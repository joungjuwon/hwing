#ifndef WATERCOLOR_CORE_INCLUDED
#define WATERCOLOR_CORE_INCLUDED

void WatercolorCore_ShadowFactors_float(
    float3 NormalWS,
    float3 LightDirection,
    float ShadowAttenuation,
    float NoiseVal,
    float NoiseStrength,
    float ShadowThreshold,
    float ShadowSmoothness,
    float DeepShadowSpread,
    float DeepShadowFalloff,
    out float ShadowFactor,
    out float DeepShadowFactor
)
{
    float NdotL = dot(NormalWS, LightDirection);
    float noisyNdotL = NdotL + (NoiseVal - 0.5) * NoiseStrength;

    float shadowSmooth = max(ShadowSmoothness, 1e-5);
    ShadowFactor = smoothstep(ShadowThreshold - shadowSmooth, ShadowThreshold + shadowSmooth, noisyNdotL);

    float gradientInput = noisyNdotL + DeepShadowSpread;
    float remappedGradient = saturate(gradientInput * 0.5 + 0.5);
    DeepShadowFactor = pow(remappedGradient, DeepShadowFalloff);

    ShadowFactor = min(ShadowFactor, ShadowAttenuation);
    DeepShadowFactor = min(DeepShadowFactor, ShadowAttenuation);
}

void WatercolorCore_float(
    float3 BaseColor,
    float3 NormalWS,
    float3 LightDirection,
    float3 LightColor,
    float ShadowAttenuation,
    float3 ShadowColor,
    float3 DeepShadowColor,
    float NoiseVal,
    float NoiseStrength,
    float ShadowThreshold,
    float ShadowSmoothness,
    float DeepShadowSpread,
    float DeepShadowFalloff,
    out float3 OutColor
)
{
    float shadowFactor;
    float deepShadowFactor;
    WatercolorCore_ShadowFactors_float(
        NormalWS,
        LightDirection,
        ShadowAttenuation,
        NoiseVal,
        NoiseStrength,
        ShadowThreshold,
        ShadowSmoothness,
        DeepShadowSpread,
        DeepShadowFalloff,
        shadowFactor,
        deepShadowFactor
    );

    float3 celColor = lerp(ShadowColor * BaseColor, BaseColor, shadowFactor);
    float3 mixedBase = lerp(DeepShadowColor * BaseColor, celColor, deepShadowFactor);
    OutColor = mixedBase * LightColor;
}

void WatercolorCoreEx_float(
    float3 BaseColor,
    float3 NormalWS,
    float3 LightDirection,
    float3 LightColor,
    float ShadowAttenuation,
    float3 ShadowColor,
    float3 DeepShadowColor,
    float NoiseVal,
    float NoiseStrength,
    float ShadowThreshold,
    float ShadowSmoothness,
    float DeepShadowSpread,
    float DeepShadowFalloff,
    out float3 OutColor,
    out float ShadowFactor,
    out float DeepShadowFactor
)
{
    WatercolorCore_ShadowFactors_float(
        NormalWS,
        LightDirection,
        ShadowAttenuation,
        NoiseVal,
        NoiseStrength,
        ShadowThreshold,
        ShadowSmoothness,
        DeepShadowSpread,
        DeepShadowFalloff,
        ShadowFactor,
        DeepShadowFactor
    );

    float3 celColor = lerp(ShadowColor * BaseColor, BaseColor, ShadowFactor);
    float3 mixedBase = lerp(DeepShadowColor * BaseColor, celColor, DeepShadowFactor);
    OutColor = mixedBase * LightColor;
}

#endif
