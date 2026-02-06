Shader "Custom/StencilVolumeMask"
{
    SubShader
    {
        // [IMPORTANT 1] Render after normal geometry so the mask occludes naturally.
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        
        Pass
        {
            // [IMPORTANT 2] Cull front faces so only the inside/back faces write stencil/depth.
            Cull Front
            
            // [IMPORTANT 3] ZWrite ON so this volume acts as a depth blocker for hidden objects.
            ZWrite On
            
            ColorMask 0 // invisible
            
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }
}