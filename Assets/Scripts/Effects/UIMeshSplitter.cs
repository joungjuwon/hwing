using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI 이미지를 격자(Grid) 형태로 쪼개서, 셰이더에서 각 조각(Vertex)을 독립적으로 제어할 수 있게 해주는 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class UIMeshSplitter : BaseMeshEffect
{
    [Header("Grid Settings")]
    [Tooltip("가로 분할 개수")]
    public int gridX = 100;

    [Tooltip("세로 분할 개수")]
    public int gridY = 50;

    /// <summary>
    /// 분할 해상도를 동적으로 설정합니다.
    /// </summary>
    public void Configure(int x, int y)
    {
        gridX = x;
        gridY = y;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        // 원본 UI 버텍스 정보 가져오기 (보통 UI는 4개의 버텍스인 Quad 1개)
        List<UIVertex> originalVerts = new List<UIVertex>();
        vh.GetUIVertexStream(originalVerts);
        
        // 원본이 비어있으면 패스
        if (originalVerts.Count == 0) return;

        // 기존 메쉬 클리어
        vh.Clear();

        // RectTransform 크기 계산 (간단히 0번과 2번 버텍스 사용)
        // 주의: UI Vertex 좌표는 Local 좌표임.
        // 일반적으로 0:bottom-left, 1:top-left, 2:top-right, 3:bottom-right 순서 (Triangle strip 등 상황따라 다름)
        // 하지만 GetUIVertexStream은 Triangle list로 나옴 (6 verts per quad)
        
        // 간단한 처리를 위해 RectTransform의 크기를 직접 참조
        RectTransform rectTransform = GetComponent<RectTransform>();
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        float left = -width * rectTransform.pivot.x;
        float bottom = -height * rectTransform.pivot.y;

        float cellW = width / gridX;
        float cellH = height / gridY;

        // 원본 UV 범위 (일반적으로 0~1)
        // 하지만 Sprite를 쓰면 UV가 0~1이 아닐 수 있음. RawImage는 보통 0~1.
        float uvLeft = 0;
        float uvBottom = 0;
        float uvW = 1f / gridX;
        float uvH = 1f / gridY;

        // Vertex 생성 루프
        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                // 현재 셀의 로컬 위치
                float xPos = left + (x * cellW);
                float yPos = bottom + (y * cellH);

                // 현재 셀의 UV
                float uPos = uvLeft + (x * uvW);
                float vPos = uvBottom + (y * uvH);

                // 셀 중심 UV (셰이더에서 랜덤 시드로 사용)
                Vector2 centerUV = new Vector2(uPos + uvW * 0.5f, vPos + uvH * 0.5f);

                // 4개 버텍스 생성 (Quad)
                UIVertex[] quad = new UIVertex[4];
                
                // BL (Local UV: 0,0)
                quad[0] = CreateVertex(xPos, yPos, uPos, vPos, centerUV, new Vector2(0, 0));
                // TL (Local UV: 0,1)
                quad[1] = CreateVertex(xPos, yPos + cellH, uPos, vPos + uvH, centerUV, new Vector2(0, 1));
                // TR (Local UV: 1,1)
                quad[2] = CreateVertex(xPos + cellW, yPos + cellH, uPos + uvW, vPos + uvH, centerUV, new Vector2(1, 1));
                // BR (Local UV: 1,0)
                quad[3] = CreateVertex(xPos + cellW, yPos, uPos + uvW, vPos, centerUV, new Vector2(1, 0));

                vh.AddUIVertexQuad(quad);
            }
        }
    }

    private UIVertex CreateVertex(float x, float y, float u, float v, Vector2 centerUV, Vector2 localUV)
    {
        UIVertex vert = UIVertex.simpleVert;
        vert.position = new Vector3(x, y, 0);
        vert.uv0 = new Vector2(u, v);
        
        // ★ 중요: uv1에 "이 조각의 중심점 UV"를 저장 (Movement Pivot)
        vert.uv1 = centerUV; 
        
        // ★ 중요: uv2에 "이 조각(Cell) 내부의 로컬 UV(0~1)"를 저장 (SubTex Mapping용)
        vert.uv2 = localUV;

        vert.color = GetComponent<Graphic>().color;
        
        return vert;
    }
}
