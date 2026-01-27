using UnityEngine;
using UnityEngine.UI; // 버튼 기능을 위해 추가
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine; // Unity 6용 시네머신
#else
using Cinemachine; // 구버전 시네머신
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
    [Tooltip("리스폰할 플레이어 프리팹")]
    public GameObject playerPrefab;
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

    private GameObject currentSpawnUi; // 현재 생성된 스폰 UI 인스턴스
    private bool isSimulationActive = false; // 시뮬레이션 모드 활성화 여부
    private float currentDayTime = 0f; // 현재 시간 흐름

    private void Start()
    {
        // 게임 시작 시 초기 상태 강제 설정: 플레이어 카메라 활성화, 시뮬레이션 카메라 비활성화
        if (playerCamera != null) playerCamera.SetActive(true);
        if (simulationCamera != null) simulationCamera.SetActive(false);

        // UI 초기 상태 설정
        if (playerUI != null) playerUI.SetActive(true);
        if (simulationUI != null) simulationUI.SetActive(false);
        isSimulationActive = false;

        // 게임 시작 시 씬에 있는 초기 플레이어(씨앗)를 찾아 이벤트 연결
        // 이렇게 하면 인스펙터에서 일일이 연결하지 않아도 첫 번째 죽음 시 시뮬레이션 뷰로 전환됩니다.
        var initialPlayer = FindAnyObjectByType<PlayerLifeCycle>();
        if (initialPlayer != null)
        {
            initialPlayer.onSprout.AddListener(EnableSimulationMode);
        }
    }

    // PlayerLifeCycle의 OnSprout 이벤트에 연결할 메서드
    public void EnableSimulationMode(Vector3 targetPosition)
    {
        Debug.Log($"[SimulationManager] 시뮬레이션 모드 전환: 위치 {targetPosition}");

        // 1. 카메라 전환
        // Cinemachine은 활성화된 가상 카메라 중 우선순위가 높은 것을 사용하므로,
        // 플레이어 카메라를 끄고 시뮬레이션 카메라를 켜면 자연스럽게 전환됩니다.
        if (playerCamera != null) playerCamera.SetActive(false);
        if (simulationCamera != null) 
        {
            simulationCamera.SetActive(true);
            
            // 선택 사항: 시뮬레이션 카메라가 싹이 튼 위치를 바라보게 하거나 위치를 이동
            // 예: simulationCamera.transform.position = targetPosition + new Vector3(0, 10, -10);
        }

        // 2. UI 전환
        if (playerUI != null) playerUI.SetActive(false);
        if (simulationUI != null) simulationUI.SetActive(true);
        
        isSimulationActive = true; // 시뮬레이션 로직 활성화

        // 3. 커서 잠금 해제 (시뮬레이션 조작을 위해)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. 랜덤 위치에 리스폰 UI 생성
        SpawnRandomRespawnUI();
    }

    private void Update()
    {
        // 모드와 상관없이 시간 흐름 처리 (슬라이더 업데이트)
        currentDayTime += Time.deltaTime;
        if (dayTimeSlider != null && dayCycleDuration > 0)
        {
            // 0~1 사이 값으로 반복 (왼쪽 -> 오른쪽 이동)
            dayTimeSlider.value = (currentDayTime % dayCycleDuration) / dayCycleDuration;
        }

        // 리스폰 UI가 활성화되어 있을 때 항상 카메라를 바라보도록 설정 (빌보드 효과)
        if (currentSpawnUi != null && simulationCamera != null)
        {
            // World Space Canvas는 카메라와 회전값이 같을 때 정면을 보게 됨
            currentSpawnUi.transform.rotation = simulationCamera.transform.rotation;
        }
    }

    private void SpawnRandomRespawnUI()
    {
        if (spawnUiPrefab == null) return;

        // 기존 UI가 있다면 제거
        if (currentSpawnUi != null) Destroy(currentSpawnUi);

        // 랜덤 위치 계산 (바닥 높이 찾기)
        Vector3 randomPos = GetRandomPositionOnMap();

        // UI 생성
        currentSpawnUi = Instantiate(spawnUiPrefab, randomPos, Quaternion.identity);

        // 생성 즉시 카메라를 바라보도록 초기 회전 설정
        if (simulationCamera != null)
        {
            currentSpawnUi.transform.rotation = simulationCamera.transform.rotation;
        }

        // 버튼 클릭 이벤트 연결
        Button btn = currentSpawnUi.GetComponentInChildren<Button>();
        if (btn != null)
        {
            // 클릭 시 해당 위치에 리스폰하도록 람다식으로 연결
            btn.onClick.AddListener(() => RespawnPlayer(randomPos));
        }
    }

    private Vector3 GetRandomPositionOnMap()
    {
        // 설정된 범위 내에서 랜덤 좌표 생성
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomZ = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        
        // SimulationManager 오브젝트의 위치를 중심으로 랜덤 좌표 계산
        Vector3 center = transform.position;
        
        // 하늘(현재 높이 + raycastHeight)에서 아래로 레이를 쏘아 바닥 위치를 찾음
        Vector3 searchPos = new Vector3(center.x + randomX, center.y + raycastHeight, center.z + randomZ);
        if (Physics.Raycast(searchPos, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            return hit.point + Vector3.up * spawnOffset;
        }
        return new Vector3(center.x + randomX, center.y + spawnOffset, center.z + randomZ); // 바닥을 못 찾으면 기준 높이 반환
    }

    // 에디터에서 스폰 범위를 눈으로 확인하기 위한 기즈모 추가
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // 현재 위치를 중심으로 스폰 범위 박스 그리기 (높이는 raycastDistance로 표시하여 탐색 범위 시각화)
        Vector3 center = transform.position;
        Vector3 boxCenter = new Vector3(center.x, center.y + raycastHeight - (raycastDistance * 0.5f), center.z);
        Gizmos.DrawWireCube(boxCenter, new Vector3(spawnAreaSize.x, raycastDistance, spawnAreaSize.y));
    }

    public void RespawnPlayer(Vector3 spawnPos)
    {
        if (playerPrefab == null) return;

        // 플레이어 생성
        GameObject newPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        // 중요: 새로 생성된 플레이어의 사망(싹틔우기) 이벤트에 시뮬레이션 모드 전환 기능을 다시 연결합니다.
        // 이 코드가 없으면 리스폰된 플레이어가 죽었을 때 시뮬레이션 뷰로 돌아오지 않습니다.
        var lifeCycle = newPlayer.GetComponent<PlayerLifeCycle>();
        if (lifeCycle != null)
        {
            lifeCycle.onSprout.AddListener(EnableSimulationMode);
        }

        // 플레이어 카메라가 새 플레이어를 따라가도록 설정
        if (playerCamera != null)
        {
            // Unity 6 (Unity.Cinemachine) 또는 구버전 호환
            var vcam = playerCamera.GetComponent<CinemachineCamera>(); 
            if (vcam != null)
            {
                vcam.Follow = newPlayer.transform;
                vcam.LookAt = newPlayer.transform;
            }
        }

        // 시뮬레이션 모드 종료 및 플레이어 모드 복귀
        if (simulationCamera != null) simulationCamera.SetActive(false);
        if (playerCamera != null) playerCamera.SetActive(true);
        if (simulationUI != null) simulationUI.SetActive(false);
        if (playerUI != null) playerUI.SetActive(true);
        isSimulationActive = false; // 시뮬레이션 로직 비활성화

        // 커서 잠금 및 스폰 UI 제거
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (currentSpawnUi != null) Destroy(currentSpawnUi);
    }
}
