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

    // Removed BaseColor multiplication for cleaner color control
    float3 celColor = lerp(ShadowColor, BaseColor, shadowFactor);
    float3 mixedBase = lerp(DeepShadowColor, celColor, deepShadowFactor);
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

    // Removed BaseColor multiplication for cleaner color control
    float3 celColor = lerp(ShadowColor, BaseColor, ShadowFactor);
    float3 mixedBase = lerp(DeepShadowColor, celColor, DeepShadowFactor);
    OutColor = mixedBase * LightColor;
}


// Dummy Functions to fix Legacy Shader Compilation
void WatercolorBlenderNormals_float(
    float3 PositionOS,
    float3 NormalOS,
    float Offset,
    float3 Scale,
    float3 Amplitude,
    out float3 OutNormal
)
{
    OutNormal = NormalOS;
}

void WatercolorBlenderLighting_float(
    float3 NormalWS, 
    float3 LightDir, 
    float3 BaseColor, 
    float3 ShadowColor, 
    float RampThresh, 
    float RampSmooth, 
    out float3 OutColor
)
{
    OutColor = BaseColor;
}

void WatercolorBlenderEdges_float(
    float3 NormalWS, 
    float3 ViewDirWS, 
    float EdgeThresh, 
    float EdgeSmooth, 
    out float Factor
)
{
    Factor = 0.0;
}

void WatercolorPaperTexture_float(
    float2 UV, 
    float2 Tiling, 
    Texture2D Tex, 
    SamplerState SS, 
    float Hue, 
    float Sat, 
    float Val, 
    out float3 OutColor
)
{
    OutColor = float3(1,1,1);
}

#endif
