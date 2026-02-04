Shader "Hwing/ScatterDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture (Dissolve Pattern)", 2D) = "white" {}
        _FadeColor ("Fade Color", Color) = (1, 1, 1, 0)
        _Color ("Tint", Color) = (1,1,1,1)
        
        _DissolveAmount ("Dissolve Amount", Range(0, 1.5)) = 0
        
        // Scatter Settings
        _ScatterDistance ("Scatter Distance", Float) = 500.0
        _Gravity ("Gravity", Float) = 50.0
        _WindDirection ("Wind Direction (X,Y)", Vector) = (1.0, 0.5, 0, 0)
        _NoiseScale ("Noise Scale (Size)", Float) = 20.0
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.1
        
        // UI Stencil support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 uvCenter : TEXCOORD1; 
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float flyFactor : TEXCOORD1; 
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex; 
            float4 _NoiseTex_ST;
            
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            float _DissolveAmount;
            float _ScatterDistance;
            float _Gravity;
            float _NoiseScale;
            float _EdgeWidth;
            
            float4 _WindDirection;
            fixed4 _FadeColor; // 사라질 때 변할 색상

            // Simple Hash Noise
            float random (float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                
                // 1. Calculate Threshold using Noise Texture
                float noiseVal = tex2Dlod(_NoiseTex, float4(v.uvCenter * _NoiseScale, 0, 0)).r;
                float threshold = noiseVal; 
                
                float flyFactor = max(0, _DissolveAmount * 1.2 - threshold);
                OUT.flyFactor = flyFactor; 

                float3 offset = float3(0,0,0);
                
                if (flyFactor > 0)
                {
                    float2 seed = v.uvCenter * 12.9898; 
                    float rand = random(floor(seed)); 

                    // 기본 바람 방향
                    float2 baseDir = normalize(_WindDirection.xy + float2(0.001, 0.001)); 
                    
                    // 랜덤 분산
                    float angleVar = (rand - 0.5) * 1.0; 
                    float c = cos(angleVar);
                    float s = sin(angleVar);
                    
                    float2 finalDir = float2(
                        baseDir.x * c - baseDir.y * s,
                        baseDir.x * s + baseDir.y * c
                    );
                    
                    offset.xy = finalDir * flyFactor * _ScatterDistance;
                    offset.y -= flyFactor * flyFactor * _Gravity; 
                    offset.z = -flyFactor * 10; 
                }

                float4 localPos = v.vertex;
                localPos.xyz += offset;

                OUT.vertex = UnityObjectToClipPos(localPos);
                OUT.texcoord = v.texcoord;
                OUT.flyFactor = flyFactor;
                OUT.color = v.color * _Color;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // Color Tint Fade (날아가기 시작하면 지정한 색으로 변함)
                // 0.0 ~ 0.3 사이에서 서서히 FadeColor로 전환
                float tintAmount = smoothstep(0.0, 0.3, IN.flyFactor);
                color.rgb = lerp(color.rgb, _FadeColor.rgb, tintAmount * _FadeColor.a);

                // Alpha Fade (그리고 나서 투명해짐)
                float alphaFade = 1.0 - smoothstep(0.4, 1.2, IN.flyFactor); 
                color.a *= alphaFade;
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
