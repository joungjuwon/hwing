#ifndef BK_LAYERED_INPUT_INCLUDED
#define BK_LAYERED_INPUT_INCLUDED

// Include URP Core for macros and samplers
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// Helper function for Triplanar Sampling (Albedo/Metallic/Smoothness)
float4 TriplanarSampling(UnityTexture2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale)
{
    float3 projNormal = pow(abs(worldNormal), falloff);
    projNormal /= (projNormal.x + projNormal.y + projNormal.z) + 0.00001;
    float3 nsign = sign(worldNormal);
    
    // Use global linear repeat sampler to avoid potential sampler state issues from Shader Graph
    float4 xNorm = SAMPLE_TEXTURE2D(topTexMap.tex, sampler_LinearRepeat, tiling * worldPos.zy * float2(nsign.x, 1.0));
    float4 yNorm = SAMPLE_TEXTURE2D(topTexMap.tex, sampler_LinearRepeat, tiling * worldPos.xz * float2(nsign.y, 1.0));
    float4 zNorm = SAMPLE_TEXTURE2D(topTexMap.tex, sampler_LinearRepeat, tiling * worldPos.xy * float2(-nsign.z, 1.0));
    
    return xNorm * projNormal.x + yNorm * projNormal.y + zNorm * projNormal.z;
}

// Helper function for Triplanar Normal Sampling
float3 TriplanarNormalSampling(UnityTexture2D topTexMap, float3 worldPos, float3 worldNormal, float falloff, float2 tiling, float3 normalScale)
{
    float3 projNormal = pow(abs(worldNormal), falloff);
    projNormal /= (projNormal.x + projNormal.y + projNormal.z) + 0.00001;
    float3 nsign = sign(worldNormal);
    
    float4 xNorm = SAMPLE_TEXTURE2D(topTexMap.tex, sampler_LinearRepeat, tiling * worldPos.zy * float2(nsign.x, 1.0));
    float4 yNorm = SAMPLE_TEXTURE2D(topTexMap.tex, sampler_LinearRepeat, tiling * worldPos.xz * float2(nsign.y, 1.0));
    float4 zNorm = SAMPLE_TEXTURE2D(topTexMap.tex, sampler_LinearRepeat, tiling * worldPos.xy * float2(-nsign.z, 1.0));
    
    // UnpackNormalScale logic
    xNorm.xyz = UnpackNormalScale(xNorm, normalScale.y).xyy;
    xNorm.z = sqrt(1.0 - saturate(dot(xNorm.xy, xNorm.xy)));
    xNorm.xy = xNorm.xy * float2(nsign.x, 1.0) + worldNormal.zy;
    xNorm.z = worldNormal.x;
    
    yNorm.xyz = UnpackNormalScale(yNorm, normalScale.x).xyy;
    yNorm.z = sqrt(1.0 - saturate(dot(yNorm.xy, yNorm.xy)));
    yNorm.xy = yNorm.xy * float2(nsign.y, 1.0) + worldNormal.xz;
    yNorm.z = worldNormal.y;
    
    zNorm.xyz = UnpackNormalScale(zNorm, normalScale.y).xyy;
    zNorm.z = sqrt(1.0 - saturate(dot(zNorm.xy, zNorm.xy)));
    zNorm.xy = zNorm.xy * float2(-nsign.z, 1.0) + worldNormal.xy;
    zNorm.z = worldNormal.z;

    return normalize(xNorm.xyz * projNormal.x + yNorm.xyz * projNormal.y + zNorm.xyz * projNormal.z);
}

float4 CalculateContrast(float contrastValue, float4 colorTarget)
{
    float t = 0.5 * (1.0 - contrastValue);
    return mul(float4x4(contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1), colorTarget);
}

// Main Function
void CalculateBKLayered_float(
    UnityTexture2D MainTex, UnityTexture2D BumpMap, UnityTexture2D MetallicGlossMap,
    UnityTexture2D LayerAlbedo, UnityTexture2D LayerNormalMap, UnityTexture2D LayerMetallicGlossMap,
    UnityTexture2D DetailAlbedo, UnityTexture2D DetailNormalMap, UnityTexture2D DetailMetallicGlossMap,
    float3 WorldPos, float3 WorldNormal, float3 WorldTangent, float3 WorldBiTangent, float4 VertexColor,
    float Tiling, float Tiling2,
    float NormalPower, float SecondNormalPower, float MetallicPower, float SmoothnessPower, float OcclusionPower,
    float LayerPower, float LayerThreshold, float LayerPosition, float LayerContrast, float BlendPower,
    float UseVertexColor, float VertexColorChannel,
    out float3 OutAlbedo, out float3 OutNormal, out float OutMetallic, out float OutSmoothness, out float OutOcclusion
)
{
    // 1. Calculate Blend Alpha
    float staticSwitch162 = VertexColor.b;
    if (VertexColorChannel == 0) staticSwitch162 = VertexColor.r;
    else if (VertexColorChannel == 1) staticSwitch162 = VertexColor.g;
    else if (VertexColorChannel == 3) staticSwitch162 = VertexColor.a;
    
    float saferPower109 = abs(staticSwitch162);
    float4 temp_cast_3 = pow(saferPower109, LayerPosition).xxxx;
    float4 clampResult105 = clamp(CalculateContrast(LayerContrast, temp_cast_3), 0, 1);
    
    float4 _2ndColor = float4(1,1,1,1); 
    
    float blendFactor = (pow(saturate(((UseVertexColor > 0.5 ? (pow(clampResult105.x, (1.0 - LayerPower)) * clampResult105.x) : WorldNormal.y) + LayerPower)), (0.001 + (LayerThreshold - 0.0) * (1.0 - 0.001))));
    float BlendAlpha = _2ndColor.a * blendFactor;

    // 2. Triplanar Sampling
    float2 LayerUVs = float2(Tiling2, Tiling2);
    float2 DetailUVs = float2(Tiling, Tiling);

    // Albedo
    float4 baseAlbedo = TriplanarSampling(LayerAlbedo, WorldPos, WorldNormal, 1.0, LayerUVs, 1.0);
    float4 layerAlbedo = TriplanarSampling(DetailAlbedo, WorldPos, WorldNormal, 1.0, DetailUVs, 1.0);
    
    OutAlbedo = lerp(baseAlbedo, layerAlbedo, BlendAlpha).rgb;

    // Metallic/Smoothness/Occlusion
    float4 baseMAOS = TriplanarSampling(LayerMetallicGlossMap, WorldPos, WorldNormal, 1.0, LayerUVs, 1.0);
    float4 layerMAOS = TriplanarSampling(DetailMetallicGlossMap, WorldPos, WorldNormal, 1.0, DetailUVs, 1.0);
    
    float4 blendedMAOS = lerp(baseMAOS, layerMAOS, BlendAlpha);
    
    OutMetallic = blendedMAOS.r + MetallicPower;
    OutSmoothness = blendedMAOS.a * SmoothnessPower;
    OutOcclusion = pow(abs(blendedMAOS.g), OcclusionPower);
    
    // Normal (Simplified - passing WorldNormal for now to ensure graph works)
    OutNormal = WorldNormal; 
}
#endif
