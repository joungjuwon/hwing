using UnityEngine;

public class SpawnAreaTrigger : MonoBehaviour
{
    [Tooltip("플레이어가 이 구역에 들어오면 변경될 새로운 스폰 영역 콜라이더")]
    public Collider targetSpawnArea;

    [Tooltip("한 번만 작동할지 여부")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;

        // 플레이어인지 확인 (태그나 컴포넌트로 확인)
        if (other.CompareTag("Player") || other.GetComponent<TPSController>() != null)
        {
            SimulationManager simManager = FindAnyObjectByType<SimulationManager>();
            if (simManager != null && targetSpawnArea != null)
            {
                simManager.SetSpawnArea(targetSpawnArea);
                Debug.Log($"[SpawnAreaTrigger] Spawn area updated to: {targetSpawnArea.name}");
                hasTriggered = true;
            }
        }
    }
}
