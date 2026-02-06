Shader "Custom/DepthOnly"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            // Key: disable color writes and only write depth.
            ColorMask 0
            ZWrite On
        }
    }
}