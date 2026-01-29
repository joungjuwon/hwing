Shader "Hwing/ScatterDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve Amount", Range(0, 1.5)) = 0
        
        // Scatter Settings
        _ScatterDistance ("Scatter Distance", Float) = 200.0
        _Gravity ("Gravity", Float) = 50.0
        _NoiseScale ("Noise Scale (Size)", Float) = 50.0
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.02

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
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            float _DissolveAmount;
            float _ScatterDistance;
            float _Gravity;
            float _NoiseScale;
            float _EdgeWidth;
            float random (float2 uv)
            {
                // sin 없는 해시 함수 (안정성 확보)
                float3 p3  = frac(float3(uv.xyx) * .1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                
                // 1. Calculate seed
                // UIMeshSplitter가 건네준 uvCenter(각 조각의 중심)를 그대로 시드로 사용
                // floor를 제거하여 경계선 부근의 정밀도 문제(일렁임) 해결
                float rand = random(v.uvCenter); 
                
                // 2. Progress
                float threshold = v.uvCenter.x + (rand * _EdgeWidth);
                
                // 3. Move logic
                float flyFactor = max(0, _DissolveAmount - threshold);
                
                float3 offset = float3(0,0,0);
                
                if (flyFactor > 0)
                {
                    float angle = rand * 6.283185;
                    float2 dir = float2(cos(angle), sin(angle));
                    
                    offset.xy = dir * flyFactor * _ScatterDistance;
                    offset.y -= flyFactor * flyFactor * _Gravity; // Parabolic
                    offset.z = -flyFactor * 10; // Z를 너무 많이 쓰면 일렁임(Sort) 원인이 됨 (100 -> 10)
                }

                float4 localPos = v.vertex;
                localPos.xyz += offset;

                OUT.worldPosition = localPos;
                OUT.vertex = UnityObjectToClipPos(localPos);

                OUT.texcoord = v.texcoord;

                // 4. Alpha Fade
                // 날아갈수록 투명해짐 (Blur 느낌 방지를 위해 비교적 단단하게 끊음)
                float alphaFade = 1.0 - smoothstep(0.0, 0.5, flyFactor); 
                OUT.color = v.color * _Color;
                OUT.color.a *= alphaFade;

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
