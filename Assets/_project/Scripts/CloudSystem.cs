using UnityEngine;
using System.Collections.Generic;

public class CloudSystem : MonoBehaviour
{
    [Header("리소스 설정")]
    public GameObject[] cloudPrefabs; // 여러 종류의 구름 프리팹 배열
    public int cloudCount = 15;       // 화면에 보여질 총 구름 개수

    [Header("영역 설정")]
    public BoxCollider spawnArea;     // 스폰 영역 (박스 콜라이더)
    public Transform despawnPoint;    // 도착(소멸) 지점

    [Header("움직임 설정")]
    public float moveSpeed = 2.0f;
    // scaleRange 변수 제거됨 (크기 고정)
    
    [Header("페이드 효과 설정")]
    public float fadeDistance = 10.0f; // 양쪽 끝에서 페이드되는 거리

    [Header("추가 구름 설정")]
    public List<Renderer> standaloneClouds; // 따로 배치된 구름들
    [Tooltip("이 오브젝트의 자식으로 있는 모든 구름들을 자동으로 등록합니다.")]
    public Transform standaloneCloudsRoot;

    private float globalAlpha = 1.0f; // 전체 투명도 제어

    // 내부 관리용 클래스
    private class CloudInstance
    {
        public Transform transform;
        public MeshRenderer renderer;
        public MaterialPropertyBlock propertyBlock;
        public Color originalColor;
    }

    private class StandaloneCloudInstance
    {
        public Renderer renderer;
        public MaterialPropertyBlock propertyBlock;
        public Color originalColor;
    }

    private List<CloudInstance> clouds = new List<CloudInstance>();
    private List<StandaloneCloudInstance> standaloneInstances = new List<StandaloneCloudInstance>();

    void Start()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0)
        {
            Debug.LogError("구름 프리팹을 1개 이상 등록해주세요!");
            return;
        }

        // 게임 시작 시 구름 미리 생성 및 배치
        for (int i = 0; i < cloudCount; i++)
        {
            CreateCloud(i, true);
        }

        // 루트 오브젝트가 있다면 자식들에서 렌더러를 찾아 추가
        if (standaloneCloudsRoot != null)
        {
            if (standaloneClouds == null) standaloneClouds = new List<Renderer>();
            
            Renderer[] childRenderers = standaloneCloudsRoot.GetComponentsInChildren<Renderer>();
            foreach (var r in childRenderers)
            {
                if (r != null && !standaloneClouds.Contains(r))
                {
                    standaloneClouds.Add(r);
                }
            }
        }

        // 독립 구름 초기화
        if (standaloneClouds != null)
        {
            foreach (var r in standaloneClouds)
            {
                if (r != null)
                {
                    var instance = new StandaloneCloudInstance();
                    instance.renderer = r;
                    instance.propertyBlock = new MaterialPropertyBlock();
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                        instance.originalColor = r.sharedMaterial.GetColor("_BaseColor");
                    else
                        instance.originalColor = Color.white;
                    standaloneInstances.Add(instance);
                }
            }
        }
    }

    void Update()
    {
        // 이동 방향 계산
        Vector3 moveDir = (despawnPoint.position - spawnArea.transform.position).normalized;
        float totalDist = Vector3.Distance(spawnArea.transform.position, despawnPoint.position);

        foreach (var cloud in clouds)
        {
            // 1. 이동
            cloud.transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

            // 2. 페이드 인/아웃 계산
            Vector3 vectorToCloud = cloud.transform.position - spawnArea.transform.position;
            float currentDist = Vector3.Dot(vectorToCloud, moveDir);

            float alpha = 1.0f;

            if (currentDist < fadeDistance) // 시작 부분 (Fade In)
            {
                alpha = Mathf.Clamp01(currentDist / fadeDistance);
            }
            else if (currentDist > totalDist - fadeDistance) // 끝 부분 (Fade Out)
            {
                alpha = Mathf.Clamp01((totalDist - currentDist) / fadeDistance);
            }

            // 3. 색상 적용
            UpdateCloudAlpha(cloud, alpha * globalAlpha);

            // 4. 도착 지점 통과 시 재활용
            if (currentDist > totalDist) 
            {
                RecycleCloud(cloud);
            }
        }

        // 독립 구름 투명도 업데이트
        foreach (var cloud in standaloneInstances)
        {
            UpdateStandaloneCloudAlpha(cloud, globalAlpha);
        }
    }

    void CreateCloud(int index, bool initialSpawn)
    {
        // 프리팹 배열에서 랜덤 선택
        GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        GameObject obj = Instantiate(prefab, transform);

        // 데이터 클래스 생성 및 정보 캐싱
        CloudInstance cloudData = new CloudInstance();
        cloudData.transform = obj.transform;
        cloudData.renderer = obj.GetComponent<MeshRenderer>();
        cloudData.propertyBlock = new MaterialPropertyBlock();

        // 원래 색상 저장
        if (cloudData.renderer.sharedMaterial.HasProperty("_BaseColor"))
        {
            cloudData.originalColor = cloudData.renderer.sharedMaterial.GetColor("_BaseColor");
        }
        else
        {
            cloudData.originalColor = Color.white; 
        }

        // 초기 위치 잡기
        Vector3 randomPos = GetRandomPointInCollider(spawnArea);

        if (initialSpawn)
        {
            float ratio = (float)index / cloudCount;
            Vector3 pathDir = despawnPoint.position - spawnArea.transform.position;
            cloudData.transform.position = randomPos + (pathDir * ratio);
        }
        else
        {
            cloudData.transform.position = randomPos;
        }

        // 랜덤 변형 (회전만)
        RandomizeCloud(cloudData);

        clouds.Add(cloudData);
    }

    void RecycleCloud(CloudInstance cloud)
    {
        // 위치 재설정
        cloud.transform.position = GetRandomPointInCollider(spawnArea);
        
        // 회전 다시 랜덤화
        RandomizeCloud(cloud);

        // 알파값 0으로 초기화
        UpdateCloudAlpha(cloud, 0f);
    }

    void RandomizeCloud(CloudInstance cloud)
    {
        // [수정됨] 스케일 랜덤 로직 제거 -> 프리팹 원본 크기 유지

        // 회전 랜덤 (Z축 고정)
        // X축: -15도 ~ 15도 (앞뒤 기울기)
        // Y축: 0도 ~ 360도 (수평 회전)
        // Z축: 0으로 고정 (옆으로 눕지 않음)
        float randomX = Random.Range(-15f, 15f);
        float randomY = Random.Range(0f, 360f);
        
        cloud.transform.rotation = Quaternion.Euler(randomX, randomY, 0f);
    }

    void UpdateCloudAlpha(CloudInstance cloud, float alpha)
    {
        if (cloud.renderer == null) return;

        Color newColor = cloud.originalColor;
        newColor.a = alpha;

        cloud.renderer.GetPropertyBlock(cloud.propertyBlock);
        cloud.propertyBlock.SetColor("_BaseColor", newColor);
        cloud.renderer.SetPropertyBlock(cloud.propertyBlock);
    }

    void UpdateStandaloneCloudAlpha(StandaloneCloudInstance cloud, float alpha)
    {
        if (cloud.renderer == null) return;

        Color newColor = cloud.originalColor;
        newColor.a = cloud.originalColor.a * alpha;

        cloud.renderer.GetPropertyBlock(cloud.propertyBlock);
        cloud.propertyBlock.SetColor("_BaseColor", newColor);
        cloud.renderer.SetPropertyBlock(cloud.propertyBlock);
    }

    Vector3 GetRandomPointInCollider(BoxCollider box)
    {
        Vector3 center = box.center;
        Vector3 size = box.size;

        Vector3 randomLocal = new Vector3(
            Random.Range(-size.x / 2, size.x / 2),
            Random.Range(-size.y / 2, size.y / 2),
            Random.Range(-size.z / 2, size.z / 2)
        );

        return box.transform.TransformPoint(center + randomLocal);
    }

    public void FadeOutAndDisable(float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            globalAlpha = 0f;
            return;
        }
        StartCoroutine(FadeOutRoutine(duration));
    }

    private System.Collections.IEnumerator FadeOutRoutine(float duration)
    {
        float startAlpha = globalAlpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            globalAlpha = Mathf.Lerp(startAlpha, 0f, t / duration);
            yield return null;
        }
        globalAlpha = 0f;
        
        // 독립 구름 비활성화
        foreach (var cloud in standaloneInstances)
        {
            if (cloud.renderer != null)
                cloud.renderer.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}