using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ErosionManager : MonoBehaviour {
    [Header("References")]
    public ComputeShader erosionShader;
    
    [Header("Import Settings (Optional)")]
    public Transform targetMeshObject; // 굴곡진 메쉬를 넣으면 모양을 복사함
    
    [Header("Simulation Settings")]
    [Range(32, 256)] public int mapSize = 64; 
    public float planeSize = 10.0f; // 맵의 가로세로 크기

    [Header("Visual Settings")]
    public Gradient terrainGradient;
    public float minHeightVis = -1.0f; 
    public float maxHeightVis = 1.0f;

    [Header("Editor Brush Settings")]
    public float brushRadius = 1.0f;     
    [Range(0, 1)] public float brushStrength = 0.5f; 
    public bool showMaskDebug = true;   

    [Header("Physics Settings")]
    public float colliderUpdateInterval = 0.1f; 
    private float colliderTimer = 0.0f;

    [HideInInspector] public List<float> savedMaskData = new List<float>();

    // 내부 변수
    struct HeightData { public float height; }
    HeightData[] mapData;
    float[] maskData;
    
    ComputeBuffer buffer;
    ComputeBuffer maskBuffer;

    Vector3[] vertices;
    Color[] colors;
    
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    void Start() {
        InitializeTerrain();
    }

    void Update() {
        if (erosionShader == null || buffer == null) return;

        // 1. 시뮬레이션
        int kernelIndex = erosionShader.FindKernel("CSMain");
        erosionShader.SetBuffer(kernelIndex, "heightBuffer", buffer);
        erosionShader.SetBuffer(kernelIndex, "maskBuffer", maskBuffer);
        erosionShader.SetInt("mapSize", mapSize);
        erosionShader.SetFloat("rTime", Time.time);

        erosionShader.Dispatch(kernelIndex, 1, 1, 1);

        // 2. 데이터 회수 및 비주얼 업데이트
        buffer.GetData(mapData);
        UpdateMeshVisualsRuntime();
    }

    public void InitializeTerrain() {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        
        // 1. 평면 메쉬 생성
        CreatePlaneMesh(); 
        
        // 2. 데이터 배열 초기화
        mapData = new HeightData[vertices.Length];
        maskData = new float[vertices.Length]; 

        // 타겟 메쉬가 있으면 스캔, 없으면 0으로 초기화
        if (targetMeshObject != null) {
            ScanTargetMesh();
        } else {
            for(int i=0; i<mapData.Length; i++) mapData[i].height = 0;
        }

        // 마스크 데이터 로드
        if (savedMaskData != null && savedMaskData.Count == vertices.Length) {
            for(int i=0; i<maskData.Length; i++) maskData[i] = savedMaskData[i];
        } else {
            savedMaskData = new List<float>(new float[vertices.Length]);
        }

        // 버퍼 생성 (Play 모드에서만)
        if (Application.isPlaying) {
            if (buffer != null) buffer.Release();
            buffer = new ComputeBuffer(mapData.Length, sizeof(float));
            buffer.SetData(mapData);

            if (maskBuffer != null) maskBuffer.Release();
            maskBuffer = new ComputeBuffer(maskData.Length, sizeof(float));
            maskBuffer.SetData(maskData);
        }

        // 초기 비주얼 업데이트
        if (Application.isPlaying) UpdateMeshVisualsRuntime();
        else UpdateEditorVisuals(); 
    }

    void CreatePlaneMesh() {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
        mesh.MarkDynamic(); // 최적화 힌트

        vertices = new Vector3[mapSize * mapSize];
        colors = new Color[mapSize * mapSize];
        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];

        float step = planeSize / (mapSize - 1);
        float offset = planeSize / 2.0f;

        // 정점 배치 (중앙 정렬)
        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                int i = y * mapSize + x;
                vertices[i] = new Vector3(x * step - offset, 0, y * step - offset);
                colors[i] = Color.black; 
            }
        }

        // 삼각형 연결
        int tri = 0;
        for (int y = 0; y < mapSize - 1; y++) {
            for (int x = 0; x < mapSize - 1; x++) {
                int i = y * mapSize + x;
                triangles[tri++] = i;
                triangles[tri++] = i + mapSize;
                triangles[tri++] = i + 1;
                triangles[tri++] = i + 1;
                triangles[tri++] = i + mapSize;
                triangles[tri++] = i + mapSize + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
        
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    void ScanTargetMesh() {
        Collider targetCol = targetMeshObject.GetComponent<Collider>();
        if (targetCol == null) return;

        float step = planeSize / (mapSize - 1);
        float offset = planeSize / 2.0f;
        float rayStartHeight = 1000.0f; 

        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                int i = y * mapSize + x;
                float localX = x * step - offset;
                float localZ = y * step - offset;

                Vector3 rayOrigin = transform.TransformPoint(new Vector3(localX, rayStartHeight, localZ));
                Ray ray = new Ray(rayOrigin, Vector3.down);
                RaycastHit hit;

                if (targetCol.Raycast(ray, out hit, 2000.0f)) {
                    // 스캔된 높이 적용
                    mapData[i].height = hit.point.y - transform.position.y;
                } else {
                    mapData[i].height = 0;
                }
            }
        }
    }

    public void PaintVertex(Vector3 worldPos) {
        if (vertices == null || savedMaskData.Count != vertices.Length) return;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        bool changed = false;

        for (int i = 0; i < vertices.Length; i++) {
            Vector2 vPos = new Vector2(vertices[i].x, vertices[i].z);
            Vector2 hitPos = new Vector2(localPos.x, localPos.z);
            
            float dist = Vector2.Distance(vPos, hitPos);

            if (dist < brushRadius) {
                float t = dist / brushRadius;
                float falloff = 1.0f - (t * t);
                float targetValue = falloff * brushStrength;

                if (targetValue > savedMaskData[i]) {
                    savedMaskData[i] = targetValue;
                    changed = true;
                }
            }
        }
        if (changed) UpdateEditorVisuals();
    }
    
    public void EraseVertex(Vector3 worldPos) {
        if (vertices == null || savedMaskData.Count != vertices.Length) return;
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        bool changed = false;

        for (int i = 0; i < vertices.Length; i++) {
            Vector2 vPos = new Vector2(vertices[i].x, vertices[i].z);
            Vector2 hitPos = new Vector2(localPos.x, localPos.z);
            
            float dist = Vector2.Distance(vPos, hitPos);

            if (dist < brushRadius) {
                float t = dist / brushRadius;
                float falloff = 1.0f - (t * t);

                float oldVal = savedMaskData[i];
                savedMaskData[i] = Mathf.Max(0.0f, savedMaskData[i] - falloff * brushStrength * 0.1f);
                
                if(oldVal != savedMaskData[i]) changed = true;
            }
        }
        if (changed) UpdateEditorVisuals();
    }

    public void UpdateEditorVisuals() {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null || vertices == null) return;
        
        for (int i = 0; i < vertices.Length; i++) {
            if (showMaskDebug) {
                float maskVal = savedMaskData[i];
                colors[i] = Color.Lerp(Color.black, Color.red, maskVal);
            } else {
                colors[i] = Color.black;
            }
        }
        meshFilter.sharedMesh.colors = colors;
    }

    void UpdateMeshVisualsRuntime() {
        for (int i = 0; i < vertices.Length; i++) {
            float h = mapData[i].height;
            vertices[i].y = h;
            
            // [핵심 변경]
            if (showMaskDebug) {
                // 1. 디버그 모드: 눈에 보이는 빨간색/검은색 그대로 표시 (기존 방식)
                if (maskData[i] > 0.1f) {
                     colors[i] = Color.Lerp(Color.black, Color.red, maskData[i]);
                } else {
                     colors[i] = Color.black;
                }
            } 
            else {
                // 2. 쉐이더 모드: 텍스처 블렌딩을 위한 "데이터"를 담아서 보냄
                // R 채널: 마스크 강도 (0 = 잔디 영역, 1 = 침식된 흙 영역)
                // G 채널: 높이 정보 (필요시 물 높이 표현에 사용)
                // B, A 채널: 비워둠 (나중에 바위나 눈 표현에 사용 가능)
                
                colors[i] = new Color(maskData[i], h, 0, 1);
            }
        }

        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.colors = colors;
        meshFilter.mesh.RecalculateNormals(); 

        colliderTimer += Time.deltaTime;
        if (colliderTimer > colliderUpdateInterval) {
            if (meshCollider != null) {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = meshFilter.mesh;
            }
            colliderTimer = 0.0f;
        }
    }

    void OnDestroy() {
        if (buffer != null) buffer.Release();
        if (maskBuffer != null) maskBuffer.Release();
    }
}