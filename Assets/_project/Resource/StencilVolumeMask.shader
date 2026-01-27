Shader "Custom/StencilVolumeMask"
{
    SubShader
    {
        // [중요 1] 일반 물체(Geometry)보다 나중에 그려야, 
        // 벽 뒤에 숨었을 때 마스크가 작동하지 않고 자연스럽게 가려집니다.
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        
        Pass
        {
            // [중요 2] 큐브의 앞면은 뚫고, 뒷면(안쪽면)만 그립니다.
            Cull Front
            
            // [중요 3] 다시 On으로 변경!
            // 이것이 '깊이 차단막' 역할을 하여, 
            // 상자보다 뒤에 있는(범위를 벗어난) 히든 오브젝트를 잘라냅니다.
            ZWrite On
            
            ColorMask 0 // 눈에는 보이지 않음
            
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }
}