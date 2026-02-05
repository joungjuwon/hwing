using UnityEngine;
using UnityEngine.UI; 
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine; 
#else
using Cinemachine; 
#endif

public class SimulationManager : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("플레이어 조작 시 사용하는 시네머신 카메라")]
    public GameObject playerCamera;
    [Tooltip("시뮬레이션 모드 전환 시 활성화할 시네머신 카메라")]
    public GameObject simulationCamera;

    [Header("UI")]
    [Tooltip("플레이어 상태 UI (숨길 대상)")]
    public GameObject playerUI;
    [Tooltip("시뮬레이션 모드 UI (보여줄 대상)")]
    public GameObject simulationUI;

    [Header("Time UI")]
    [Tooltip("하루 시간을 표시할 슬라이더 (상단 바)")]
    public Slider dayTimeSlider;
    [Tooltip("하루의 길이 (초 단위)")]
    public float dayCycleDuration = 60f;

    [Header("Respawn Settings")]
    [Tooltip("리스폰할 플레이어 프리팹 리스트 (Intro와 인덱스 일치시켜야 함)")]
    public GameObject[] playerPrefabs; 
    
    [Tooltip("다음에 스폰될 씨앗의 인덱스")]
    public int currentSeedIndex = 0;

    [Tooltip("랜덤 위치에 생성될 UI 프리팹 (World Space Canvas 권장)")]
    public GameObject spawnUiPrefab;
    public Vector2 spawnAreaSize = new Vector2(40f, 40f); // 스폰 랜덤 범위 (가로, 세로)
    [Tooltip("레이캐스트 시작 높이 (현재 위치 기준)")]
    public float raycastHeight = 50f;
    [Tooltip("레이캐스트 탐색 거리")]
    public float raycastDistance = 100f;
    [Tooltip("바닥에서 띄울 높이")]
    public float spawnOffset = 0.1f;
    public LayerMask groundLayer; // 바닥 감지용 레이어

    [Header("Game Flow")]
    [Tooltip("게임 시작 시 자동으로 인트로 재생 (시작하자마자 떨어짐)")]
    public bool autoStartIntro = true;

    [Header("Reference")]
    [Tooltip("인트로 컨트롤러 (자동으로 못 찾으면 여기에 연결)")]
    public IntroSequenceController introController;
    [Tooltip("씬에 있는 덩굴 효과 컨트롤러 (플레이어 생성 시 자동 연결용)")]
    public VineGrowthController vineController;

    private GameObject currentSpawnUi; // 현재 생성된 스폰 UI 인스턴스
    private bool isSimulationActive = false; // 시뮬레이션 모드 활성화 여부
    private float currentDayTime = 0f; // 현재 시간 흐름

    private void Start()
    {
        // 게임 시작 시 초기 상태 강제 설정
        if (playerCamera != null) playerCamera.SetActive(true);
        if (simulationCamera != null) simulationCamera.SetActive(false);

        // UI 초기 상태 설정
        if (playerUI != null) playerUI.SetActive(true);
        if (simulationUI != null) simulationUI.SetActive(false);
        isSimulationActive = false;

        // 자동 시작 로직
        if (autoStartIntro)
        {
            // 이미 씬에 배치된 플레이어가 있다면 제거 (중복 방지)
            var existingPlayer = FindAnyObjectByType<PlayerLifeCycle>();
            if (existingPlayer != null) Destroy(existingPlayer.gameObject);

            // 랜덤 위치에서 리스폰(인트로) 시작
            // GetRandomPositionOnMap()이 안전하지 않을 수 있다면(Invoke 필요?), 여기서 바로 호출.
            // 하지만 Start에서도 동작해야 함.
            RespawnPlayer(GetRandomPositionOnMap());
        }
        else
        {
            // 자동 시작 아님: 씬에 있는 배치된 플레이어 사용
            var initialPlayer = FindAnyObjectByType<PlayerLifeCycle>();
            if (initialPlayer != null)
            {
                initialPlayer.onSprout.AddListener(EnableSimulationMode);
                // 기존 플레이어에게도 덩굴 연결 시도
                if (vineController != null)
                {
                    initialPlayer.onLifePhaseChanged.AddListener(vineController.SetGrowthTarget);
                }
            }
        }
    }

    private void Update()
    {
        // 시간 흐름 처리
        currentDayTime += Time.deltaTime;
        if (dayTimeSlider != null && dayCycleDuration > 0)
        {
            dayTimeSlider.value = (currentDayTime % dayCycleDuration) / dayCycleDuration;
        }

        // 리스폰 UI 빌보드 효과
        if (currentSpawnUi != null && simulationCamera != null)
        {
            currentSpawnUi.transform.rotation = simulationCamera.transform.rotation;
        }
    }

    // PlayerLifeCycle의 OnSprout 이벤트에 연결할 메서드
    public void EnableSimulationMode(Vector3 targetPosition)
    {
        Debug.Log($"[SimManager] EnableSimulationMode called at {targetPosition}");

        // 덩굴 효과 초기화 (부드럽게 사라지게 함)
        if (vineController != null)
        {
            vineController.SetGrowthTarget(0f);
        }

        // 1. 카메라 전환 (Sim 켜고 Player 끄기)
        if (playerCamera != null) playerCamera.SetActive(false);
        if (simulationCamera != null) simulationCamera.SetActive(true);

        // 2. UI 전환
        if (playerUI != null) playerUI.SetActive(false);
        if (simulationUI != null) simulationUI.SetActive(true);
        
        isSimulationActive = true; 

        // 3. 커서 잠금 해제
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. 랜덤 리스폰 UI 생성
        SpawnRandomRespawnUI();
    }

    private void SpawnRandomRespawnUI()
    {
        Debug.Log("[SimManager] Spawning Respawn UI...");
        if (spawnUiPrefab == null) 
        {
            Debug.LogError("[SimManager] spawnUiPrefab is NULL!");
            return;
        }

        if (currentSpawnUi != null) Destroy(currentSpawnUi);

        Vector3 randomPos = GetRandomPositionOnMap();

        currentSpawnUi = Instantiate(spawnUiPrefab, randomPos, Quaternion.identity);

        if (simulationCamera != null)
        {
            currentSpawnUi.transform.rotation = simulationCamera.transform.rotation;
        }

        Button btn = currentSpawnUi.GetComponentInChildren<Button>();
        if (btn != null)
        {
            Debug.Log("[SimManager] Button found in UI. Adding listener.");
            btn.onClick.AddListener(() => 
            {
                Debug.Log("[SimManager] Respawn Button Clicked!");
                RespawnPlayer(randomPos);
            });
        }
        else
        {
            Debug.LogError("[SimManager] Button component NOT found in spawnUiPrefab children!");
        }
    }

    private Vector3 GetRandomPositionOnMap()
    {
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomZ = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        
        Vector3 center = transform.position;
        Vector3 searchPos = new Vector3(center.x + randomX, center.y + raycastHeight, center.z + randomZ);

        if (Physics.Raycast(searchPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            return hit.point + Vector3.up * spawnOffset;
        }
        return new Vector3(center.x + randomX, center.y + spawnOffset, center.z + randomZ); 
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 boxCenter = new Vector3(center.x, center.y + raycastHeight - (raycastDistance * 0.5f), center.z);
        Gizmos.DrawWireCube(boxCenter, new Vector3(spawnAreaSize.x, raycastDistance, spawnAreaSize.y));
    }

    public void RespawnPlayer(Vector3 spawnPos)
    {
        Debug.Log($"[SimManager] RespawnPlayer called at {spawnPos}");
        if (introController == null)
        {
            introController = FindAnyObjectByType<IntroSequenceController>();
        }

        // 1. 플레이어 캐릭터 먼저 생성 (위치는 나중에 IntroController가 덮어씌움)
        if (playerPrefabs == null || playerPrefabs.Length == 0) return;
        int index = Mathf.Clamp(currentSeedIndex, 0, playerPrefabs.Length - 1);
        GameObject selectedPrefab = playerPrefabs[index];
        if (selectedPrefab == null) return;

        GameObject newPlayer = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

        // 2. 조작 및 물리 비활성화 (낙하 연출 중 조작 방지)
        var controller = newPlayer.GetComponent<TPSController>();
        var lifeCycle = newPlayer.GetComponent<PlayerLifeCycle>();
        var rb = newPlayer.GetComponent<Rigidbody>();

        if (controller != null) controller.enabled = false;
        if (lifeCycle != null) lifeCycle.enabled = false;
        if (rb != null) rb.isKinematic = true; // 물리 영향 받지 않도록 고정도 고려 (연출 스타일에 따라 다름)

        // 3. 인트로 시작
        if (introController != null)
        {
            // 인트로 카메라와 간섭 없도록 정리
            if (simulationCamera != null) simulationCamera.SetActive(false);
            if (simulationUI != null) simulationUI.SetActive(false);
            
            // 실제 플레이어 객체를 넘겨줌
            introController.PlayIntro(newPlayer, spawnPos, () => 
            {
                OnIntroFinished(newPlayer);
            });
        }
        else
        {
            // 인트로 없으면 바로 시작
            OnIntroFinished(newPlayer);
        }
    }

    // 인트로 종료 후 호출: 플레이어 조작 활성화 및 게임 시작
    private void OnIntroFinished(GameObject player)
    {
        if (player == null) return;

        // 1. 조작 및 물리 활성화
        var controller = player.GetComponent<TPSController>();
        var lifeCycle = player.GetComponent<PlayerLifeCycle>();
        var rb = player.GetComponent<Rigidbody>();

        if (rb != null) rb.isKinematic = false; // 물리 켜기
        if (controller != null) controller.enabled = true;
        if (lifeCycle != null) 
        {
            lifeCycle.enabled = true;
            // 중요: 사망 시 시뮬레이션 모드 전환 연결
            lifeCycle.onSprout.AddListener(EnableSimulationMode);

            // [NEW] 덩굴 효과 연결 (프리팹 문제를 해결하기 위해 여기서 코드로 연결)
            if (vineController != null)
            {
                // 이전 이벤트 리스너가 있다면 중복 방지가 어렵지만, 새 플레이어 객체이므로 괜찮음.
                lifeCycle.onLifePhaseChanged.AddListener(vineController.SetGrowthTarget);
            }
        }

        // 2. 플레이어 카메라 연결
        if (playerCamera != null)
        {
            // Unity 6 (Unity.Cinemachine) 또는 구버전 호환
            var vcam = playerCamera.GetComponent<CinemachineCamera>(); 
            if (vcam != null)
            {
                vcam.Follow = player.transform;
                vcam.LookAt = player.transform;
                // 필요하다면 여기서 SmartTerrainCamera 등의 설정을 갱신할 수도 있음
            }
        }

        // 3. 모드 정리 (Player Mode ON)
        if (simulationCamera != null) simulationCamera.SetActive(false);
        if (playerCamera != null) playerCamera.SetActive(true);
        if (simulationUI != null) simulationUI.SetActive(false);
        if (playerUI != null) playerUI.SetActive(true);
        isSimulationActive = false; 

        // 4. 커서 및 UI 정리
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (currentSpawnUi != null) Destroy(currentSpawnUi);
    }
}
