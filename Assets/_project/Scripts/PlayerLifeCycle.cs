using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class PlayerLifeCycle : MonoBehaviour
{
    // Inspector에서 Vector3 매개변수를 받는 이벤트를 보이기 위한 래퍼 클래스
    [System.Serializable]
    public class SproutEvent : UnityEvent<Vector3> { }

    [Header("Life Settings")]
    public float maxLifeTime = 24.0f; // 최대 생존 시간
    public GameObject deathSpawnPrefab; // 죽을 때 생성할 오브젝트
    public GameObject playerVisuals; // 플레이어 모델
    public float deathStopDamping = 5.0f; // 죽은 뒤 멈출 때 적용할 마찰력
    public LayerMask lifeDecreaseLayer; // 수명이 줄어드는 지형 레이어

    [Header("Terrain Effect Settings")]
    [Tooltip("터레인을 변경할지 여부")]
    public bool enableTerrainModification = true;
    [Tooltip("변화시킬 반경 (터레인 그리드 단위)")]
    public int effectRadius = 4;
    [Tooltip("변경할 바닥 텍스처(Splat)의 레이어 인덱스 (예: 1 = 풀)")]
    public int targetGroundLayerIndex = 1;
    [Tooltip("심을 잔디(Detail)의 레이어 인덱스")]
    public int targetGrassLayerIndex = 0;
    [Tooltip("심을 잔디의 밀도 (0~16)")]
    public int grassDensity = 8;

    [Header("Events")]
    [Tooltip("싹이 트고 환경이 변하기 시작할 때 호출되는 이벤트")]
    public SproutEvent onSprout;

    [HideInInspector]
    public bool suppressSproutEvent = false; // 이벤트 발생 억제 플래그 (연출용)

    // 외부(SimulationManager)에서 접근 가능한 생존율 속성 (0.0 ~ 1.0)
    public float LifeRatio => Mathf.Clamp01(currentLifeTime / maxLifeTime);

    private TPSController controller;
    private Rigidbody rb;
    private float currentLifeTime;
    private bool isDead = false;
    private bool hasSpawnedDeathObject = false;

    private void Awake()
    {
        controller = GetComponent<TPSController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentLifeTime = maxLifeTime;
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            HandleDeathPhysics();
            return;
        }

        // 컨트롤러가 있고 땅에 있을 때만 시간 감소
        if (controller != null && controller.IsGrounded)
        {
            // 지정된 레이어(예: 땅) 위에 있을 때만 수명 감소
            if (((1 << controller.CurrentGroundLayer) & lifeDecreaseLayer) != 0)
            {
                currentLifeTime -= Time.fixedDeltaTime;

                if (currentLifeTime <= 0f)
                {
                    Die();
                }
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    private void HandleDeathPhysics()
    {
        rb.linearDamping = deathStopDamping;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);

        if (!hasSpawnedDeathObject && rb.linearVelocity.sqrMagnitude < 0.01f && rb.angularVelocity.sqrMagnitude < 0.01f)
        {
            SpawnDeathObject();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SpawnDeathObject()
    {
        hasSpawnedDeathObject = true;

        if (deathSpawnPrefab != null)
        {
            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = Quaternion.identity;

            // 바닥 위치 보정 (Raycast)
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, Mathf.Infinity))
            {
                spawnPosition = hit.point;
            }

            // 1. 싹 오브젝트 생성
            Instantiate(deathSpawnPrefab, spawnPosition, spawnRotation);

            // 2. 터레인 변경 (옵션이 켜져있을 때만)
            if (enableTerrainModification)
            {
                ApplyTerrainChanges(spawnPosition);
            }

            // 3. 이벤트 발생 (억제 플래그 체크)
            if (!suppressSproutEvent)
            {
                onSprout?.Invoke(spawnPosition);
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 지정된 위치 주변의 터레인 텍스처(Splatmap)를 바꾸고 잔디(DetailLayer)를 심습니다.
    /// </summary>
    private void ApplyTerrainChanges(Vector3 worldPos)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        TerrainData data = terrain.terrainData;

        // --- 1. 바닥 텍스처(Splatmap) 변경 ---
        // 월드 좌표 -> 알파맵 좌표 변환
        int mapX = Mathf.FloorToInt((worldPos.x - terrain.transform.position.x) / data.size.x * data.alphamapWidth);
        int mapZ = Mathf.FloorToInt((worldPos.z - terrain.transform.position.z) / data.size.z * data.alphamapHeight);

        // 수정할 범위 계산 (배열 범위를 넘지 않게 Clamp)
        int startX = Mathf.Max(0, mapX - effectRadius);
        int startZ = Mathf.Max(0, mapZ - effectRadius);
        int width = Mathf.Min(data.alphamapWidth - startX, effectRadius * 2 + 1);
        int height = Mathf.Min(data.alphamapHeight - startZ, effectRadius * 2 + 1);

        // 현재 데이터 가져오기
        float[,,] splatmapData = data.GetAlphamaps(startX, startZ, width, height);
        int numLayers = data.alphamapLayers;

        // 원형으로 칠하기 위해 거리 계산 루프
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 중심점(effectRadius, effectRadius)으로부터의 거리 체크
                if (Vector2.Distance(new Vector2(x, y), new Vector2(effectRadius, effectRadius)) <= effectRadius)
                {
                    // 타겟 레이어를 1로, 나머지는 0으로 설정
                    for (int i = 0; i < numLayers; i++)
                    {
                        splatmapData[y, x, i] = (i == targetGroundLayerIndex) ? 1.0f : 0.0f;
                    }
                }
            }
        }
        // 변경된 알파맵 적용
        data.SetAlphamaps(startX, startZ, splatmapData);


        // --- 2. 잔디(Detail Layer) 심기 ---
        // 잔디 맵은 알파맵과 해상도가 다를 수 있으므로 별도 좌표 계산
        int detailX = Mathf.FloorToInt((worldPos.x - terrain.transform.position.x) / data.size.x * data.detailResolution);
        int detailZ = Mathf.FloorToInt((worldPos.z - terrain.transform.position.z) / data.size.z * data.detailResolution);

        // 비율에 맞춰 반경 재조정 (Splatmap 해상도 vs Detail 해상도 비율)
        float resolutionRatio = (float)data.detailResolution / data.alphamapResolution;
        int detailRadius = Mathf.RoundToInt(effectRadius * resolutionRatio);

        int dStartX = Mathf.Max(0, detailX - detailRadius);
        int dStartZ = Mathf.Max(0, detailZ - detailRadius);
        int dWidth = Mathf.Min(data.detailResolution - dStartX, detailRadius * 2 + 1);
        int dHeight = Mathf.Min(data.detailResolution - dStartZ, detailRadius * 2 + 1);

        // 현재 잔디 데이터 가져오기
        int[,] detailMap = data.GetDetailLayer(dStartX, dStartZ, dWidth, dHeight, targetGrassLayerIndex);

        for (int y = 0; y < dHeight; y++)
        {
            for (int x = 0; x < dWidth; x++)
            {
                if (Vector2.Distance(new Vector2(x, y), new Vector2(detailRadius, detailRadius)) <= detailRadius)
                {
                    detailMap[y, x] = grassDensity; // 잔디 심기
                }
            }
        }
        // 변경된 잔디 적용
        data.SetDetailLayer(dStartX, dStartZ, targetGrassLayerIndex, detailMap);
    }
}