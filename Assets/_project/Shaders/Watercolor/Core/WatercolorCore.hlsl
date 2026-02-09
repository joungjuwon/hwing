#ifndef WATERCOLOR_CORE_INCLUDED
#define WATERCOLOR_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// --- 1. Blender Noise Approximation (High Quality) ---
float w_hash(float3 p) {
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float w_valueNoise(float3 p) {
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    
    float n000 = w_hash(i + float3(0,0,0));
    float n100 = w_hash(i + float3(1,0,0));
    float n010 = w_hash(i + float3(0,1,0));
    float n110 = w_hash(i + float3(1,1,0));
    float n001 = w_hash(i + float3(0,0,1));
    float n101 = w_hash(i + float3(1,0,1));
    float n011 = w_hash(i + float3(0,1,1));
    float n111 = w_hash(i + float3(1,1,1));
    
    float n00 = lerp(n000, n100, u.x);
    float n10 = lerp(n010, n110, u.x);
    float n01 = lerp(n001, n101, u.x);
    float n11 = lerp(n011, n111, u.x);
    float n0 = lerp(n00, n10, u.y);
    float n1 = lerp(n01, n11, u.y);
    return lerp(n0, n1, u.z);
}

float BlenderNoise(float3 p, float scale, float detail, float roughness, float distortion) {
    if (distortion > 0.0) {
        p += (w_valueNoise(p + 0.5) - 0.5) * distortion;
    }
    
    float sum = 0.0;
    float amp = 1.0;
    float freq = scale;
    float maxAmp = 0.0;
    
    for(int i=0; i<4; i++) { 
        if (i >= (int)detail) break;
        sum += w_valueNoise(p * freq) * amp;
        maxAmp += amp;
        amp *= roughness;
        freq *= 2.0;
    }
    return sum / maxAmp;
}

void WatercolorDistortNormal_float(
    float3 PositionOS,
    float3 NormalOS,
    float NoiseScale,
    float NoiseDetail,
    float NoiseRoughness,
    float NoiseDistortion,
    float Strength,
    out float3 OutNormal
)
{
    float n1 = BlenderNoise(PositionOS, NoiseScale, NoiseDetail, NoiseRoughness, NoiseDistortion);
    float n2 = BlenderNoise(PositionOS + float3(12.3, 4.5, 6.7), NoiseScale, NoiseDetail, NoiseRoughness, NoiseDistortion);
    float n3 = BlenderNoise(PositionOS + float3(-5.1, 9.2, -3.4), NoiseScale, NoiseDetail, NoiseRoughness, NoiseDistortion);
    
    float3 offset = (float3(n1, n2, n3) - 0.5) * 2.0 * Strength;
    OutNormal = normalize(NormalOS + offset);
}

// --- Internal: ACES Tonemapping Approximation ---
float3 ACESFilm(float3 x) {
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return saturate((x*(a*x+b))/(x*(c*x+d)+e));
}

// --- 2. Base color control (texture vs palette) ---
// paletteStrength: 0 = keep BaseTex color influence, 1 = palette dominates (use tint only)
// detailStrength: 0 = no extra texture detail, 1 = multiply luma(BaseTex) back in fully
void WatercolorComputeBase_float(
    float3 BaseTexRGB,
    float3 BaseTintRGB,
    float PaletteStrength,
    float DetailStrength,
    out float3 BaseForLighting,
    out float DetailMul
)
{
    float p = saturate(PaletteStrength);
    float d = saturate(DetailStrength);

    float3 baseMul = BaseTexRGB * BaseTintRGB;
    BaseForLighting = lerp(baseMul, BaseTintRGB, p);

    float luma = dot(BaseTexRGB, float3(0.3, 0.59, 0.11));
    DetailMul = lerp(1.0, luma, d);
}

// --- 3. Inner line & pigment (material-level watercolor feel) ---
void WatercolorInnerLinePigment_float(
    float3 InColor,
    float3 PositionOS,
    float3 NormalWS,
    float3 ViewDirWS,
    float InnerLineStrength,
    float InnerLinePower,
    float3 InnerLineColor,
    float PigmentStrength,
    float PigmentScale,
    float PigmentNoiseStrength,
    out float3 OutColor
)
{
    // Edge darkening (soft fresnel)
    float fresnel = 1.0 - saturate(dot(NormalWS, ViewDirWS));
    fresnel = pow(fresnel, max(InnerLinePower, 0.0001));

    // Organic noise (object space) to break the line
    float3 p1 = PositionOS + float3(7.13, 7.13, 7.13);
    float3 p2 = PositionOS + float3(-3.7, -3.7, -3.7);

    float n = BlenderNoise(p1, max(PigmentScale, 0.0001), 3.0, 0.5, 1.0);
    float n2 = BlenderNoise(p2, max(PigmentScale, 0.0001) * 1.7, 3.0, 0.6, 1.0);
    float edgeNoise = (n2 - 0.5) * 2.0 * PigmentNoiseStrength;

    float edgeMask = saturate(fresnel + edgeNoise);

    float3 edgeTint = lerp(float3(1.0, 1.0, 1.0), InnerLineColor, edgeMask * saturate(InnerLineStrength));

    // Pigment pooling: darker/stronger around noisy boundaries
    float pigmentNoise = abs(n - 0.5) * 2.0;
    float pigmentDark = 1.0 - pigmentNoise * saturate(PigmentStrength);

    OutColor = max(InColor * edgeTint * pigmentDark, 0.0);
}

// --- 4. Advanced Watercolor Lighting ---
// Advanced Watercolor Lighting (production)
void WatercolorLightingComplex_float(
    float3 BaseColor,
    float3 NormalWS,
    float3 ViewDirWS,
    float3 LightDir,
    float3 LightColor,
    float ShadowAtten,

    Texture2D RampLightA, SamplerState SamplerA,
    Texture2D RampLightB, SamplerState SamplerB,
    Texture2D RampEdgeA,  SamplerState SamplerEA,
    Texture2D RampEdgeB,  SamplerState SamplerEB,
    Texture2D RampEdgeCol, SamplerState SamplerEC,

    float LayerBlend,
    out float3 OutColor
)
{
    // URP's mainLight.direction sign can vary by version/pipeline.
    // Make it robust by choosing the direction that gives the lit side (positive N·L).
    float ndl = dot(NormalWS, LightDir);
    float NdotL = saturate(ndl * 0.5 + 0.5);

    // Soft shadow remap: lower floor so shadowed regions don't stay overly bright.
    float softShadow = lerp(0.2, 1.0, saturate(ShadowAtten));
    float lightIntensity = pow(max(NdotL * softShadow, 0.01), 0.85);

    float3 rampA = SAMPLE_TEXTURE2D(RampLightA, SamplerA, float2(lightIntensity, 0.5)).rgb;
    float rampAFactor = dot(rampA, float3(0.3, 0.59, 0.11));

    float3 rampB = SAMPLE_TEXTURE2D(RampLightB, SamplerB, float2(rampAFactor, 0.5)).rgb;
    float3 bodyColor = rampB * BaseColor * LightColor;

    // Edge blend is driven by light direction (N dot L), not camera view angle.
    float edgeCoord = 1.0 - NdotL;
    edgeCoord = pow(edgeCoord, max(LayerBlend, 0.0001));

    float3 edgeA = SAMPLE_TEXTURE2D(RampEdgeA, SamplerEA, float2(edgeCoord, 0.5)).rgb;
    float3 edgeB = SAMPLE_TEXTURE2D(RampEdgeB, SamplerEB, float2(edgeCoord, 0.5)).rgb;
    float edgeMask = saturate(dot(edgeA, float3(0.33,0.33,0.33)) * dot(edgeB, float3(0.33,0.33,0.33)));

    float3 edgeColor = SAMPLE_TEXTURE2D(RampEdgeCol, SamplerEC, float2(edgeMask, 0.5)).rgb;

    // IMPORTANT: don't always multiply edgeColor (it can crush to black).
    float3 rawColor = lerp(bodyColor, bodyColor * edgeColor, edgeMask);

    // IMPORTANT: Tonemapping/contrast should be done in URP Volume (Filmic/High Contrast matching).
    // Keep shader output in scene-linear space.
    OutColor = max(rawColor, 0.0);
}

// Debug-capable version (returns intermediates for inspector tools)
void WatercolorLightingComplex_Debug_float(
    float3 BaseColor,
    float3 NormalWS,
    float3 ViewDirWS,
    float3 LightDir,
    float3 LightColor,
    float ShadowAtten,

    Texture2D RampLightA, SamplerState SamplerA,
    Texture2D RampLightB, SamplerState SamplerB,
    Texture2D RampEdgeA,  SamplerState SamplerEA,
    Texture2D RampEdgeB,  SamplerState SamplerEB,
    Texture2D RampEdgeCol, SamplerState SamplerEC,

    float LayerBlend,
    out float LightIntensity,
    out float RampAFactor,
    out float EdgeMask,
    out float3 RampBColor,
    out float3 EdgeColor,
    out float3 RawColor,
    out float3 TonemappedColor
)
{
    float ndl = dot(NormalWS, LightDir);
    float NdotL = saturate(ndl * 0.5 + 0.5);

    float softShadow = lerp(0.2, 1.0, saturate(ShadowAtten));
    LightIntensity = pow(max(NdotL * softShadow, 0.01), 0.85);

    float3 rampA = SAMPLE_TEXTURE2D(RampLightA, SamplerA, float2(LightIntensity, 0.5)).rgb;
    RampAFactor = dot(rampA, float3(0.3, 0.59, 0.11));

    RampBColor = SAMPLE_TEXTURE2D(RampLightB, SamplerB, float2(RampAFactor, 0.5)).rgb;
    float3 bodyColor = RampBColor * BaseColor * LightColor;

    // Edge blend is driven by light direction (N dot L), not camera view angle.
    float edgeCoord = 1.0 - NdotL;
    edgeCoord = pow(edgeCoord, max(LayerBlend, 0.0001));

    float3 edgeA = SAMPLE_TEXTURE2D(RampEdgeA, SamplerEA, float2(edgeCoord, 0.5)).rgb;
    float3 edgeB = SAMPLE_TEXTURE2D(RampEdgeB, SamplerEB, float2(edgeCoord, 0.5)).rgb;
    EdgeMask = saturate(dot(edgeA, float3(0.33,0.33,0.33)) * dot(edgeB, float3(0.33,0.33,0.33)));

    EdgeColor = SAMPLE_TEXTURE2D(RampEdgeCol, SamplerEC, float2(EdgeMask, 0.5)).rgb;

    RawColor = max(lerp(bodyColor, bodyColor * EdgeColor, EdgeMask), 0.0);
    // No tonemapping here (use URP Volume). Keep this output in scene-linear.
    TonemappedColor = RawColor;
}

// Simple 버전도 업데이트
void WatercolorLightingSimple_float(
    float3 BaseColor,
    float3 PositionWS,
    float3 NormalWS,
    Texture2D RampTex,
    SamplerState RampSampler,
    float RampY,
    float ShadowStrength,
    out float3 OutColor
)
{
    #ifdef SHADERGRAPH_PREVIEW
        float3 lightDir = float3(0.5, 0.5, 0);
        float3 lightColor = float3(1, 1, 1);
        float shadowAtten = 1.0;
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
        Light mainLight = GetMainLight(shadowCoord);
        float3 lightDir = mainLight.direction;
        float3 lightColor = mainLight.color;
        float shadowAtten = mainLight.shadowAttenuation;
    #endif

    float ndl = dot(NormalWS, lightDir);
    if (ndl < 0.0) ndl = dot(NormalWS, -lightDir);
    float NdotL = ndl * 0.5 + 0.5;
    float softShadow = lerp(1.0 - ShadowStrength, 1.0, shadowAtten);
    float lightIntensity = NdotL * softShadow;
    float2 rampUV = float2(lightIntensity, RampY);
    float4 rampSample = SAMPLE_TEXTURE2D(RampTex, RampSampler, rampUV);
    
    float3 rawColor = rampSample.rgb * BaseColor * lightColor;
    
    // No tonemapping here (use URP Volume). Keep scene-linear.
    OutColor = max(rawColor, 0.0);
}

void WatercolorPaper_float(
    float3 InColor,
    float2 ScreenUV,
    Texture2D PaperTex,
    SamplerState PaperSampler,
    float PaperTiling,
    float PaperStrength,
    out float3 OutColor
)
{
    float2 uv = ScreenUV * PaperTiling;
    float paper = SAMPLE_TEXTURE2D(PaperTex, PaperSampler, uv).r;
    // 종이 질감 합성 후에는 다시 톤매핑 할 필요 없음 (이미 색상 완성됨)
    float3 blended = InColor * paper;
    OutColor = lerp(InColor, blended, PaperStrength);
}

#endif
