Shader "Custom/DepthOnly"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            // 핵심: 색깔(RGB)은 끄고, 깊이(Z)만 기록합니다.
            ColorMask 0
            ZWrite On
        }
    }
}