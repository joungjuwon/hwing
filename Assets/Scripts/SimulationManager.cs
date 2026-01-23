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

    [Header("Respawn Settings")]
    [Tooltip("리스폰할 플레이어 프리팹")]
    public GameObject playerPrefab;
    [Tooltip("랜덤 위치에 생성될 UI 프리팹 (World Space Canvas 권장)")]
    public GameObject spawnUiPrefab;
    public Vector2 spawnAreaSize = new Vector2(40f, 40f); // 스폰 랜덤 범위 (가로, 세로)
    public LayerMask groundLayer; // 바닥 감지용 레이어

    private GameObject currentSpawnUi; // 현재 생성된 스폰 UI 인스턴스

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

        // 3. 커서 잠금 해제 (시뮬레이션 조작을 위해)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. 랜덤 위치에 리스폰 UI 생성
        SpawnRandomRespawnUI();
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
        
        // 하늘에서 아래로 레이를 쏘아 바닥 위치를 찾음
        Vector3 searchPos = new Vector3(randomX, 50f, randomZ);
        if (Physics.Raycast(searchPos, Vector3.down, out RaycastHit hit, 100f, groundLayer))
        {
            return hit.point;
        }
        return new Vector3(randomX, 0f, randomZ); // 바닥을 못 찾으면 높이 0 반환
    }

    public void RespawnPlayer(Vector3 spawnPos)
    {
        if (playerPrefab == null) return;

        // 플레이어 생성
        GameObject newPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

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

        // 커서 잠금 및 스폰 UI 제거
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (currentSpawnUi != null) Destroy(currentSpawnUi);
    }
}
