#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class TerrainMeshBaker
{
    [MenuItem("Tools/Terrain/Bake Heightmap 1:1 Mesh (Save .asset)")]
    public static void BakeSelectedTerrainToMeshAsset()
    {
        // 선택된 오브젝트에서 Terrain 찾기
        Terrain terrain = null;

        if (Selection.activeGameObject != null)
            terrain = Selection.activeGameObject.GetComponent<Terrain>();

        if (terrain == null)
            terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Terrain Mesh Baker",
                "Terrain을 선택하거나(Selection) 씬에 activeTerrain이 있어야 합니다.", "OK");
            return;
        }

        TerrainData td = terrain.terrainData;
        if (td == null)
        {
            EditorUtility.DisplayDialog("Terrain Mesh Baker",
                "TerrainData가 없습니다.", "OK");
            return;
        }

        int hmRes = td.heightmapResolution; // 예: 513
        if (hmRes < 2)
        {
            EditorUtility.DisplayDialog("Terrain Mesh Baker",
                "heightmapResolution이 너무 작습니다.", "OK");
            return;
        }

        // 저장 경로 선택
        string defaultName = $"{terrain.name}_HM{hmRes}_Mesh.asset";
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Mesh Asset",
            defaultName,
            "asset",
            "Mesh 에셋(.asset) 저장 위치를 선택하세요."
        );

        if (string.IsNullOrEmpty(path))
            return;

        // Mesh 생성
        Mesh mesh = BuildMeshFromHeightmap_1to1(td, hmRes);
        mesh.name = Path.GetFileNameWithoutExtension(path);

        // 동일 경로에 기존 에셋 있으면 덮어쓰기 처리(안전하게 삭제 후 생성)
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 에셋 선택
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        EditorGUIUtility.PingObject(Selection.activeObject);

        EditorUtility.DisplayDialog("Terrain Mesh Baker",
            $"완료!\n- Terrain: {terrain.name}\n- HeightmapRes: {hmRes}x{hmRes}\n- Saved: {path}", "OK");
    }

    private static Mesh BuildMeshFromHeightmap_1to1(TerrainData td, int hmRes)
    {
        // heights: [y, x] / 값 범위 0..1
        float[,] heights = td.GetHeights(0, 0, hmRes, hmRes);

        Vector3 size = td.size;

        int vertsPerLine = hmRes;                 // 1:1 정점
        int quadsPerLine = hmRes - 1;             // 1:1 셀
        int vertCount = vertsPerLine * vertsPerLine;

        // spacing: Terrain 가로/세로를 (hmRes-1)로 나눈 간격
        float stepX = size.x / (hmRes - 1);
        float stepZ = size.z / (hmRes - 1);

        var vertices = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var normals = new Vector3[vertCount];

        // 정점/UV/노멀 생성
        // Terrain local space 기준 (Terrain 위치에 메쉬 오브젝트를 놓으면 딱 맞음)
        int idx = 0;
        for (int y = 0; y < vertsPerLine; y++)
        {
            float v = (hmRes == 1) ? 0f : (float)y / (hmRes - 1); // 0..1
            for (int x = 0; x < vertsPerLine; x++)
            {
                float u = (hmRes == 1) ? 0f : (float)x / (hmRes - 1); // 0..1

                float h01 = heights[y, x];
                float vy = h01 * size.y;

                float vx = x * stepX;
                float vz = y * stepZ;

                vertices[idx] = new Vector3(vx, vy, vz);
                uvs[idx] = new Vector2(u, v);

                // Terrain이 쓰는 노멀에 최대한 가깝게(보간 노멀)
                normals[idx] = td.GetInterpolatedNormal(u, v);

                idx++;
            }
        }

        // 인덱스(삼각형) 생성
        int triCount = quadsPerLine * quadsPerLine * 6;
        var triangles = new int[triCount];

        int ti = 0;
        for (int y = 0; y < quadsPerLine; y++)
        {
            for (int x = 0; x < quadsPerLine; x++)
            {
                int i00 = (y * vertsPerLine) + x;
                int i10 = i00 + 1;
                int i01 = i00 + vertsPerLine;
                int i11 = i01 + 1;

                // winding: 위에서 보면 정면이 위로
                triangles[ti++] = i00;
                triangles[ti++] = i01;
                triangles[ti++] = i10;

                triangles[ti++] = i10;
                triangles[ti++] = i01;
                triangles[ti++] = i11;
            }
        }

        var mesh = new Mesh();

        // 65k 넘어갈 가능성 큼(예: 513x513=263,169)
        if (vertCount > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals; // 이미 만들어둔 노멀 사용
        mesh.RecalculateBounds();

        return mesh;
    }
}
#endif
