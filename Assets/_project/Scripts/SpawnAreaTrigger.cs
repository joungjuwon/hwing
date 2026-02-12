using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine; 
#else
using Cinemachine; 
#endif

public class SpawnAreaTrigger : MonoBehaviour
{
    [Tooltip("플레이어가 이 구역에 들어오면 변경될 새로운 스폰 영역 콜라이더")]
    public Collider targetSpawnArea;

    [Header("Object Control")]
    [Tooltip("트리거 진입 시 비활성화할 오브젝트들")]
    public List<GameObject> objectsToDisable;

    [Tooltip("트리거 진입 시 활성화할 오브젝트들")]
    public List<GameObject> objectsToEnable;
    [Header("Death Sequence Settings")]
    [Tooltip("이 구역에 진입하면 플레이어를 죽일지 여부")]
    public bool killPlayerOnEnter = false;

    [Tooltip("플레이어 사망 시 생성할 추가 프리팹 (선택 사항)")]
    public GameObject deathSpawnPrefab;

    [Tooltip("사망 시 프리팹이 생성될 위치 (비워두면 플레이어 사망 위치)")]
    public Transform customDeathSpawnPoint;

    [Tooltip("연출용 카메라 (활성화 시 뒤로 빠지는 연출, Cinemachine Virtual Camera 권장)")]
    public GameObject pullBackCamera;

    [Tooltip("연출 시 재생할 BGM")]
    public AudioClip sequenceBgm;

    [Tooltip("시뮬레이션 뷰(UI 등)로 전환되기 전 대기 시간")]
    public float sequenceDuration = 4.0f;

    [Tooltip("한 번만 작동할지 여부")]
    public bool triggerOnce = true;

    [Header("Terrain Planting Settings")]
    [Tooltip("잔디/꽃을 심을 대상 터레인 (비워두면 현재 활성화된 터레인 사용)")]
    public Terrain targetTerrain;
    [Tooltip("심을 디테일(풀/꽃) 레이어 인덱스 (Terrain Inspector의 Details 탭 순서, 0부터 시작)")]
    public int[] detailLayerIndices;
    [Tooltip("심을 반경 (디테일 맵 그리드 단위)")]
    public int plantingRadius = 5;
    [Tooltip("심을 밀도 (한 셀당 생성할 개수)")]
    public int plantingDensity = 10;
    [Tooltip("심을 영역을 정의하는 콜라이더 리스트 (비워두면 위 반경 설정 사용)")]
    public List<Collider> plantingAreaColliders;

    [Header("Terrain Painting Settings")]
    [Tooltip("칠할 터레인 레이어 인덱스 (Terrain Inspector의 Layers 탭 순서, 0부터 시작, -1이면 사용 안 함)")]
    public int paintLayerIndex = -1;
    [Tooltip("칠할 불투명도 (0~1)")]
    [Range(0f, 1f)] public float paintOpacity = 1.0f;

    [Header("Environment Control")]
    [Tooltip("연출 시 제어할 구름 시스템 (선택 사항)")]
    public CloudSystem cloudSystem;
    [Tooltip("구름/날씨 등 환경 변화 시작 전 딜레이")]
    public float environmentChangeDelay = 1.0f;
    [Tooltip("연출 후 변경될 날씨 상태")]
    public WeatherState targetWeather = WeatherState.Rain;

    [Header("Ending Settings")]
    [Tooltip("이 구역에 진입하면 엔딩 컷신을 재생할지 여부")]
    public bool isEndingTrigger = false;
    [Tooltip("엔딩 연출 컨트롤러 (비워두면 자동으로 찾음)")]
    public EndingSequenceController endingController;

    private bool hasTriggered = false;


    private void Start()
    {
        // 터레인이 할당되지 않았다면 활성화된 터레인 찾기
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;

        // 게임 시작 시점의 터레인 디테일 데이터 백업
        if (targetTerrain != null && detailLayerIndices != null)
        {
            // TerrainManager가 없으면 자동으로 생성
            if (TerrainManager.Instance == null)
            {
                GameObject go = new GameObject("TerrainManager");
                go.AddComponent<TerrainManager>();
            }

            TerrainData td = targetTerrain.terrainData;

            foreach (int layerIndex in detailLayerIndices)
            {
                if (layerIndex >= 0 && layerIndex < td.detailPrototypes.Length)
                {
                    // TerrainManager에게 백업 요청
                    TerrainManager.Instance.BackupDetailLayer(targetTerrain, layerIndex);
                }
            }
        }

        // 터레인 텍스처(Alphamap) 데이터 백업
        if (targetTerrain != null && paintLayerIndex >= 0)
        {
            TerrainManager.Instance.BackupAlphamaps(targetTerrain);
        }
    }

    private void OnDestroy()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;

        // 플레이어 확인 로직 개선: 자식 콜라이더가 닿아도 인식되도록 부모까지 검색
        TPSController controller = other.GetComponentInParent<TPSController>();
        
        // 태그가 Player이거나, TPSController 컴포넌트를 가지고 있다면 플레이어로 간주
        if (other.CompareTag("Player") || controller != null)
        {
            // 실제 플레이어 오브젝트 (컴포넌트가 있는 루트 객체 사용 권장)
            GameObject playerObj = controller != null ? controller.gameObject : other.gameObject;

            SimulationManager simManager = FindAnyObjectByType<SimulationManager>();
            
            // 1. 스폰 영역 변경
            if (simManager != null && targetSpawnArea != null)
            {
                simManager.SetSpawnArea(targetSpawnArea);
                Debug.Log($"[SpawnAreaTrigger] Spawn area updated to: {targetSpawnArea.name}");
            }

            // 1.5 오브젝트 비활성화
            if (objectsToDisable != null)
            {
                foreach (var obj in objectsToDisable)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }

             // 1.6 오브젝트 활성화
            if (objectsToEnable != null)
            {
                foreach (var obj in objectsToEnable)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }

            // 2. 죽음 및 연출 처리
            if (killPlayerOnEnter || isEndingTrigger)
            {
                hasTriggered = true; // 죽음 처리는 확실하게 한 번만
                StartCoroutine(PlayDeathSequence(playerObj, simManager));
            }
            else
            {
                hasTriggered = true;
            }
        }
    }

    private void TriggerEnding(GameObject player)
    {
        if (endingController == null)
        {
            endingController = FindAnyObjectByType<EndingSequenceController>();
        }

        if (endingController != null)
        {
            Debug.Log("[SpawnAreaTrigger] Starting Ending Sequence...");
            
            // 플레이어 비활성화 (엔딩 연출이 별도 오브젝트를 사용하므로)
            if (player != null) player.SetActive(false);

            // 엔딩 재생
            endingController.PlayEnding();
        }
        else
        {
            Debug.LogError("[SpawnAreaTrigger] isEndingTrigger is true but EndingSequenceController not found!");
        }
    }

    private IEnumerator PlayDeathSequence(GameObject player, SimulationManager simManager)
    {
        // 0. 위치 저장 및 플레이어 사망 처리
        Vector3 deathPosition = player.transform.position;
        Quaternion spawnRotation = Quaternion.identity;

        if (customDeathSpawnPoint != null)
        {
            deathPosition = customDeathSpawnPoint.position;
            spawnRotation = customDeathSpawnPoint.rotation;
        }

        var lifeCycle = player.GetComponent<PlayerLifeCycle>();
        if (lifeCycle != null)
        {
            lifeCycle.suppressSproutEvent = true;
            lifeCycle.Die();
        }

        if (deathSpawnPrefab != null)
        {
            Instantiate(deathSpawnPrefab, deathPosition, spawnRotation);
        }

        CinemachineCamera pullBackVCam = null;

        // 1. BGM 재생 및 연출 카메라 활성화
        if (pullBackCamera != null)
        {
            pullBackCamera.SetActive(true);
            pullBackVCam = pullBackCamera.GetComponent<CinemachineCamera>();
            if (pullBackVCam != null) pullBackVCam.Priority = 100; // 높은 우선순위로 강제 전환
        }

        if (sequenceBgm != null)
        {
            // SoundManager를 통해 BGM 재생 (Loop: true로 변경하여 시퀀스 동안 유지)
            SoundManager.Instance.PlayBGM(sequenceBgm, 1f, true);
        }

        // 2. 터레인 변화 연출 (중심에서부터 차오르기)
        yield return StartCoroutine(AnimateTerrainChanges(deathPosition, sequenceDuration));

        // 2.5 엔딩 트리거라면 여기서 엔딩으로 전환 (시뮬레이션 모드 진입 대신)
        if (isEndingTrigger)
        {
            TriggerEnding(player);
            yield break;
        }

        // 3. 시뮬레이션 모드로 전환
        if (simManager != null)
        {
            simManager.EnableSimulationMode(deathPosition);

            if (pullBackCamera != null)
            {
                if (pullBackVCam != null) // Add this check
                {
                    pullBackVCam.Priority = 0; // 우선순위 초기화
                }
                pullBackCamera.SetActive(false);
            }
        }

        // 3.5 환경 변화 전 딜레이
        if (environmentChangeDelay > 0f)
        {
            yield return new WaitForSeconds(environmentChangeDelay);
        }

        // 4. 시뮬레이션 뷰 전환 후, 구름 제거 및 날씨 변경
        if (WeatherManager.Instance != null)
        {
            float weatherTransitionDuration = WeatherManager.Instance.transitionDuration;

            if (cloudSystem != null)
            {
                cloudSystem.FadeOutAndDisable(weatherTransitionDuration);
            }
            WeatherManager.Instance.SetWeather(targetWeather);
        }

        // 시퀀스 종료 후 이벤트 BGM 정지
        if (sequenceBgm != null)
        {
            SoundManager.Instance.StopBGM();
        }
    }

    private IEnumerator AnimateTerrainChanges(Vector3 centerPos, float duration)
    {
        if (targetTerrain == null) yield break;

        // 콜라이더 영역 모드에서는 애니메이션을 지원하지 않으므로 즉시 적용하고 종료합니다.
        if (plantingAreaColliders != null && plantingAreaColliders.Count > 0)
        {
            PlantDetailsOnTerrain(centerPos, 1.0f);
            PaintTerrain(centerPos, 1.0f);
            // 즉시 종료하지 않고 설정된 시간만큼 대기하여 연출 시간을 확보합니다.
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            PlantDetailsOnTerrain(centerPos, progress);
            PaintTerrain(centerPos, progress);

            yield return null;
        }

        // 연출이 끝난 후, 100% 상태로 최종 적용 보장
        PlantDetailsOnTerrain(centerPos, 1.0f);
        PaintTerrain(centerPos, 1.0f);
    }

    private void PlantDetailsOnTerrain(Vector3 centerPos, float progress = 1.0f)
    {
        // 터레인이 할당되지 않았다면 활성화된 터레인 찾기
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
        if (targetTerrain == null || detailLayerIndices == null || detailLayerIndices.Length == 0) return;

        TerrainData terrainData = targetTerrain.terrainData;
        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        // 0. 콜라이더 리스트가 있다면 해당 영역에 심기
        if (plantingAreaColliders != null && plantingAreaColliders.Count > 0)
        {
            foreach (var col in plantingAreaColliders)
            {
                if (col != null) PlantDetailsInCollider(col, targetTerrain, terrainData, detailWidth, detailHeight);
            }
            return; // 콜라이더 모드 사용 시 반경 모드는 무시
        }

        // 월드 좌표를 터레인 디테일 맵 좌표로 변환
        Vector3 relativePos = centerPos - targetTerrain.transform.position;
        int centerX = (int)(relativePos.x / terrainData.size.x * detailWidth);
        int centerY = (int)(relativePos.z / terrainData.size.z * detailHeight);

        int currentRadius = Mathf.CeilToInt(plantingRadius * progress);

        foreach (int layerIndex in detailLayerIndices)
        {
            // 유효한 레이어 인덱스인지 확인
            if (layerIndex < 0 || layerIndex >= terrainData.detailPrototypes.Length) continue;

            // 수정할 영역 계산 (전체 맵을 가져오면 느리므로 필요한 부분만 가져옴)
            int startX = Mathf.Max(0, centerX - currentRadius);
            int startY = Mathf.Max(0, centerY - currentRadius);
            int width = Mathf.Min(detailWidth, centerX + currentRadius) - startX;
            int height = Mathf.Min(detailHeight, centerY + currentRadius) - startY;

            if (width <= 0 || height <= 0) continue;

            // 현재 디테일 레이어 데이터 가져오기
            int[,] map = terrainData.GetDetailLayer(startX, startY, width, height, layerIndex);

            // 원형으로 심기
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 중심에서의 거리 체크
                    float dist = Vector2.Distance(new Vector2(startX + x, startY + y), new Vector2(centerX, centerY));
                    if (dist <= currentRadius)
                    {
                        map[y, x] = Mathf.Max(map[y, x], plantingDensity); // 기존 밀도보다 낮으면 덮어쓰지 않음
                    }
                }
            }

            // 변경된 데이터 적용
            terrainData.SetDetailLayer(startX, startY, layerIndex, map);
        }
    }

    private void PlantDetailsInCollider(Collider col, Terrain terrain, TerrainData terrainData, int detailWidth, int detailHeight)
    {
        Bounds bounds = col.bounds;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        // 콜라이더 바운드에 해당하는 디테일 맵 범위 계산
        int startX = Mathf.FloorToInt((bounds.min.x - terrainPos.x) / terrainSize.x * detailWidth);
        int startY = Mathf.FloorToInt((bounds.min.z - terrainPos.z) / terrainSize.z * detailHeight);
        int endX = Mathf.CeilToInt((bounds.max.x - terrainPos.x) / terrainSize.x * detailWidth);
        int endY = Mathf.CeilToInt((bounds.max.z - terrainPos.z) / terrainSize.z * detailHeight);

        startX = Mathf.Clamp(startX, 0, detailWidth);
        startY = Mathf.Clamp(startY, 0, detailHeight);
        endX = Mathf.Clamp(endX, 0, detailWidth);
        endY = Mathf.Clamp(endY, 0, detailHeight);

        int width = endX - startX;
        int height = endY - startY;

        if (width <= 0 || height <= 0) return;

        foreach (int layerIndex in detailLayerIndices)
        {
            if (layerIndex < 0 || layerIndex >= terrainData.detailPrototypes.Length) continue;

            int[,] map = terrainData.GetDetailLayer(startX, startY, width, height, layerIndex);
            bool modified = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 현재 셀의 월드 좌표 계산
                    float normX = (startX + x) / (float)detailWidth;
                    float normY = (startY + y) / (float)detailHeight;
                    float worldX = terrainPos.x + normX * terrainSize.x;
                    float worldZ = terrainPos.z + normY * terrainSize.z;
                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;

                    // 콜라이더 내부에 있는지 확인 (ClosestPoint 이용)
                    Vector3 worldPos = new Vector3(worldX, worldY, worldZ);
                    if (Vector3.SqrMagnitude(col.ClosestPoint(worldPos) - worldPos) < 0.01f)
                    {
                        map[y, x] = Mathf.Max(map[y, x], plantingDensity);
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                terrainData.SetDetailLayer(startX, startY, layerIndex, map);
            }
        }
    }

    private void PaintTerrain(Vector3 centerPos, float progress = 1.0f)
    {
        if (targetTerrain == null || paintLayerIndex < 0 || progress <= 0) return;

        TerrainData terrainData = targetTerrain.terrainData;
        if (paintLayerIndex >= terrainData.alphamapLayers) return;

        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;

        // 0. 콜라이더 리스트가 있다면 해당 영역 칠하기
        if (plantingAreaColliders != null && plantingAreaColliders.Count > 0)
        {
            foreach (var col in plantingAreaColliders)
            {
                if (col != null) PaintTerrainInCollider(col, targetTerrain, terrainData, alphamapWidth, alphamapHeight);
            }
            return;
        }

        // 월드 좌표를 알파맵 좌표로 변환
        Vector3 relativePos = centerPos - targetTerrain.transform.position;
        int centerX = (int)(relativePos.x / terrainData.size.x * alphamapWidth);
        int centerY = (int)(relativePos.z / terrainData.size.z * alphamapHeight);

        // 수정할 영역 계산
        // plantingRadius는 디테일 맵 기준이므로 알파맵 해상도에 맞춰 비율 조정 필요할 수 있으나, 
        // 여기서는 편의상 같은 값을 사용하거나 비율을 곱해줍니다.
        float resolutionRatio = (float)alphamapWidth / terrainData.detailWidth;
        int maxPaintRadius = Mathf.Max(1, (int)(plantingRadius * resolutionRatio));
        int currentPaintRadius = Mathf.CeilToInt(maxPaintRadius * progress);

        int startX = Mathf.Max(0, centerX - currentPaintRadius);
        int startY = Mathf.Max(0, centerY - currentPaintRadius);
        int width = Mathf.Min(alphamapWidth, centerX + currentPaintRadius) - startX;
        int height = Mathf.Min(alphamapHeight, centerY + currentPaintRadius) - startY;

        if (width <= 0 || height <= 0) return;

        float[,,] splatmapData = terrainData.GetAlphamaps(startX, startY, width, height);
        int numLayers = terrainData.alphamapLayers;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (Vector2.Distance(new Vector2(startX + x, startY + y), new Vector2(centerX, centerY)) <= currentPaintRadius)
                {
                    ApplyPaintToSplatmap(splatmapData, x, y, numLayers);
                }
            }
        }

        terrainData.SetAlphamaps(startX, startY, splatmapData);
    }

    private void PaintTerrainInCollider(Collider col, Terrain terrain, TerrainData terrainData, int mapWidth, int mapHeight)
    {
        Bounds bounds = col.bounds;
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        int startX = Mathf.FloorToInt((bounds.min.x - terrainPos.x) / terrainSize.x * mapWidth);
        int startY = Mathf.FloorToInt((bounds.min.z - terrainPos.z) / terrainSize.z * mapHeight);
        int endX = Mathf.CeilToInt((bounds.max.x - terrainPos.x) / terrainSize.x * mapWidth);
        int endY = Mathf.CeilToInt((bounds.max.z - terrainPos.z) / terrainSize.z * mapHeight);

        startX = Mathf.Clamp(startX, 0, mapWidth);
        startY = Mathf.Clamp(startY, 0, mapHeight);
        endX = Mathf.Clamp(endX, 0, mapWidth);
        endY = Mathf.Clamp(endY, 0, mapHeight);

        int width = endX - startX;
        int height = endY - startY;

        if (width <= 0 || height <= 0) return;

        float[,,] splatmapData = terrainData.GetAlphamaps(startX, startY, width, height);
        int numLayers = terrainData.alphamapLayers;
        bool modified = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normX = (startX + x) / (float)mapWidth;
                float normY = (startY + y) / (float)mapHeight;
                Vector3 worldPos = new Vector3(terrainPos.x + normX * terrainSize.x, 0, terrainPos.z + normY * terrainSize.z);
                worldPos.y = terrain.SampleHeight(worldPos) + terrainPos.y;

                if (Vector3.SqrMagnitude(col.ClosestPoint(worldPos) - worldPos) < 0.01f)
                {
                    ApplyPaintToSplatmap(splatmapData, x, y, numLayers);
                    modified = true;
                }
            }
        }

        if (modified) terrainData.SetAlphamaps(startX, startY, splatmapData);
    }

    private void ApplyPaintToSplatmap(float[,,] data, int x, int y, int numLayers)
    {
        // 다른 레이어들을 0으로 만들고 타겟 레이어를 1로 설정 (간단한 덮어쓰기)
        // 부드러운 블렌딩이 필요하면 기존 값을 읽어서 비율 조정 로직 추가 가능
        for (int i = 0; i < numLayers; i++)
        {
            data[y, x, i] = (i == paintLayerIndex) ? paintOpacity : 0f;
        }
    }
}
