#ifndef ROCKTOOLS_URP_COMMON_INCLUDED
#define ROCKTOOLS_URP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct RockAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct RockVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;
    float4 color : TEXCOORD3;
    float4 shadowCoord : TEXCOORD4;
    half fogFactor : TEXCOORD5;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

RockVaryings RockVert(RockAttributes input)
{
    RockVaryings output = (RockVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalize(normalInputs.normalWS);
    output.tangentWS = float4(normalize(normalInputs.tangentWS), input.tangentOS.w * GetOddNegativeScale());
    output.color = input.color;
    output.shadowCoord = TransformWorldToShadowCoord(positionInputs.positionWS);
    output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

    return output;
}

float3 RockScaleWorldPos(float3 positionWS, float uvTile, float uvOffset)
{
    return (positionWS + uvOffset.xxx) * uvTile;
}

float3 RockGetTriplanarWeightsSimple(float3 normalWS)
{
    float3 weights = pow(abs(normalWS), 4.0);
    return weights / max(dot(weights, 1.0.xxx), 1e-5);
}

float3 RockGetTriplanarWeightsSmooth(float3 normalWS, float threshold, float smoothness)
{
    float low = threshold - smoothness;
    float high = threshold + smoothness;
    float3 weights = smoothstep(low.xxx, high.xxx, abs(normalWS));
    return weights / max(dot(weights, 1.0.xxx), 1e-5);
}

float3 RockSampleTriplanarRGB(
    TEXTURE2D_PARAM(texMap, texSampler),
    float3 scaledWorldPos,
    float3 weights)
{
    float2 uvZ = scaledWorldPos.xy;
    float2 uvX = scaledWorldPos.zy;
    float2 uvY = scaledWorldPos.xz;

    float3 sampleZ = SAMPLE_TEXTURE2D(texMap, texSampler, uvZ).rgb;
    float3 sampleX = SAMPLE_TEXTURE2D(texMap, texSampler, uvX).rgb;
    float3 sampleY = SAMPLE_TEXTURE2D(texMap, texSampler, uvY).rgb;

    return sampleZ * weights.z + sampleX * weights.x + sampleY * weights.y;
}

float RockComputeAO(float3 aoRGB, float aoIntensity)
{
    float ao = dot(aoRGB, float3(0.333333, 0.333333, 0.333333));
    return saturate(ao + (1.0 - aoIntensity));
}

float3 RockBuildNormalWS(RockVaryings input, float3 normalRGB, float normalIntensity)
{
    float3 normalTS = normalRGB * 2.0 - 1.0;
    normalTS.xy *= normalIntensity;
    normalTS.z = sqrt(saturate(1.0 - dot(normalTS.xy, normalTS.xy)));

    float3 tangentWS = normalize(input.tangentWS.xyz);
    float3 normalWS = normalize(input.normalWS);
    float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
    float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);

    return normalize(TransformTangentToWorld(normalTS, tbn));
}

half3 RockShade(float3 albedo, float3 normalWS, float3 positionWS, float4 shadowCoord)
{
    float3 n = normalize(normalWS);
    float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(positionWS));

    Light mainLight = GetMainLight(shadowCoord);
    float ndotl = saturate(dot(n, mainLight.direction));

    half3 color = albedo * SampleSH(n);
    color += albedo * mainLight.color * (ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation);

    float3 halfVec = SafeNormalize(mainLight.direction + viewDirWS);
    float spec = pow(saturate(dot(n, halfVec)), 32.0) * mainLight.shadowAttenuation;
    color += mainLight.color * spec * 0.08;

    return color;
}

#endif
