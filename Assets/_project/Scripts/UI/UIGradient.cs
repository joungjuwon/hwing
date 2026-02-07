using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color color1 = Color.white;
    public Color color2 = Color.white;
    [Range(-180f, 180f)]
    public float angle = 0f;
    public bool ignoreRatio = true;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        List<UIVertex> list = new List<UIVertex>();
        vh.GetUIVertexStream(list);

        int count = list.Count;
        if (count == 0) return;

        float bottomY = list[0].position.y;
        float topY = list[0].position.y;
        float leftX = list[0].position.x;
        float rightX = list[0].position.x;

        for (int i = 1; i < count; i++)
        {
            float y = list[i].position.y;
            if (y > topY) topY = y;
            else if (y < bottomY) bottomY = y;

            float x = list[i].position.x;
            if (x > rightX) rightX = x;
            else if (x < leftX) leftX = x;
        }

        float uiWidth = rightX - leftX;
        float uiHeight = topY - bottomY;

        UIVertex v = new UIVertex();
        for (int i = 0; i < count; i++)
        {
            // vh.PopulateUIVertex 대신 리스트에서 직접 가져옴
            v = list[i];

            float t = 0f;
            
            float xPos = (uiWidth == 0) ? 0 : (v.position.x - leftX) / uiWidth;
            float yPos = (uiHeight == 0) ? 0 : (v.position.y - bottomY) / uiHeight;

            // 각도에 따른 블렌딩
            float rad = angle * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);

            // 회전된 좌표계에서의 위치 비율 계산
            t = (xPos * c) + (yPos * s);
            
            // 보정
            if (angle == 0) t = xPos;
            else if (angle == 90 || angle == -90) t = (angle == 90) ? yPos : 1 - yPos;
            else if (angle == 180) t = 1 - xPos;
            else t = (t + 1f) * 0.5f;

            v.color = Color.Lerp(color1, color2, t);
            list[i] = v; // 리스트 업데이트
        }
        
        // 중요: 변경된 리스트를 VertexHelper에 다시 주입
        vh.Clear();
        vh.AddUIVertexTriangleStream(list);
    }
}
