using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ErosionManager : MonoBehaviour
{
    [Header("Generation Settings")]
    public int mapSize = 128;          // 맵 해상도
    public float planeSize = 20.0f;    // 실제 크기
    public float terrainHeight = 3.0f; // 언덕 높이
    public float noiseScale = 5.0f;    // 언덕 빈도
    public Vector2 noiseOffset;        // 지형 모양 시드

    [Header("Erosion Settings")]
    public float brushRadius = 3.0f;
    public float digStrength = 1.0f;
    public float paintStrength = 2.0f;

    // 내부 데이터
    private Vector3[] vertices;
    private Color[] colors; // 쉐이더로 보낼 마스크 데이터 (R채널)
    private Mesh mesh;
    private MeshCollider meshCollider;

    void Start()
    {
        GenerateTerrain();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInteraction();
        }
    }

    void GenerateTerrain()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();

        vertices = new Vector3[mapSize * mapSize];
        colors = new Color[mapSize * mapSize]; // Vertex Color 배열
        Vector2[] uvs = new Vector2[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];

        float step = planeSize / (mapSize - 1);
        float offset = planeSize / 2.0f;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int i = y * mapSize + x;
                
                // 1. 펄린 노이즈로 높이 생성
                float xCoord = (float)x / mapSize * noiseScale + noiseOffset.x;
                float yCoord = (float)y / mapSize * noiseScale + noiseOffset.y;
                float height = Mathf.PerlinNoise(xCoord, yCoord) * terrainHeight;

                vertices[i] = new Vector3(x * step - offset, height, y * step - offset);
                
                // 2. 초기 색상: 검은색 (R=0, 즉 잔디)
                colors[i] = Color.black; 
                
                uvs[i] = new Vector2((float)x / (mapSize - 1), (float)y / (mapSize - 1));
            }
        }

        // 삼각형 인덱스 (Quad 구성)
        int t = 0;
        for (int y = 0; y < mapSize - 1; y++)
        {
            for (int x = 0; x < mapSize - 1; x++)
            {
                int i = y * mapSize + x;
                triangles[t++] = i; triangles[t++] = i + mapSize; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + mapSize; triangles[t++] = i + mapSize + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.colors = colors; // [중요] 쉐이더로 전송
        mesh.uv = uvs;
        mesh.triangles = triangles;
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshCollider.sharedMesh = mesh;
    }

    void HandleInteraction()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                DeformTerrain(hit.point);
            }
        }
    }

    void DeformTerrain(Vector3 point)
    {
        Vector3 localPos = transform.InverseTransformPoint(point);
        bool modified = false;

        // 최적화를 위해 전체 루프 대신 거리 기반 검색 권장 (여기선 간략화)
        for (int i = 0; i < vertices.Length; i++)
        {
            float dist = Vector3.Distance(new Vector3(vertices[i].x, vertices[i].z), new Vector3(localPos.x, localPos.z)); // Y축 제외 거리

            if (dist < brushRadius)
            {
                float influence = 1.0f - (dist / brushRadius);

                // 1. 땅 파기
                vertices[i].y -= influence * digStrength * Time.deltaTime;

                // 2. 흙 칠하기 (R채널 증가)
                float currentR = colors[i].r;
                colors[i].r = Mathf.Clamp01(currentR + influence * paintStrength * Time.deltaTime);
                
                modified = true;
            }
        }

        if (modified)
        {
            mesh.vertices = vertices;
            mesh.colors = colors; // 변경된 마스크 전송
            mesh.RecalculateNormals();
            meshCollider.sharedMesh = mesh; // 충돌체 갱신
        }
    }
}