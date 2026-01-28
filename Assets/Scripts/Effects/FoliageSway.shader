Shader "Hwing/FoliageSway"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        
        [Header(Wind Settings)]
        _SwaySpeed ("Sway Speed Multiplier", Float) = 1.0
        _SwayAmount ("Sway Amount Multiplier", Float) = 0.1
        _SwayHeight ("Sway Start Height (Y)", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;

            float4 _GlobalWindDir;
            float _GlobalWindStrength;
            float _GlobalWindTime;

            float _SwaySpeed;
            float _SwayAmount;
            float _SwayHeight;

            v2f vert (appdata v)
            {
                v2f o;
                
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float heightFactor = max(0, v.vertex.y - _SwayHeight);
                float time = _GlobalWindTime * _SwaySpeed;
                float phase = worldPos.x * 0.5 + worldPos.z * 0.5;
                float sinWave = sin(time + phase);
                
                float3 displacement = _GlobalWindDir.xyz * _GlobalWindStrength * _SwayAmount * heightFactor * sinWave;
                v.vertex.xyz += displacement;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                clip(col.a - _Cutoff); 
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
