Shader "Hidden/Watercolor/PaperBlit"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "PaperBlit"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // URP blit helper
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_PaperTex);
            SAMPLER(sampler_PaperTex);

            float4 _PaperST;          // xy tiling, zw offset
            float _PaperSaturation;   // 0=gray, 1=original
            float _PaperContrast;     // 1=unchanged
            float _PaperBrightness;   // 0=unchanged

            float3 PaperDesaturate(float3 c, float sat)
            {
                float l = dot(c, float3(0.299, 0.587, 0.114));
                return lerp(l.xxx, c, sat);
            }

            float3 ApplyContrast(float3 c, float contrast)
            {
                return (c - 0.5) * contrast + 0.5;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float2 paperUV = uv * _PaperST.xy + _PaperST.zw;

                float3 paper = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, paperUV).rgb;
                paper = PaperDesaturate(paper, _PaperSaturation);
                paper = ApplyContrast(paper, _PaperContrast);
                paper += _PaperBrightness;
                paper = saturate(paper);

                return float4(paper, 1);
            }
            ENDHLSL
        }
    }
}
