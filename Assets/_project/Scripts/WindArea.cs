using UnityEngine;

public class WindArea : MonoBehaviour
{
    [Header("Wind Settings")]
    public float strength = 20f;        // 바람의 세기
    public Vector3 direction = Vector3.up; // 바람의 방향 (기본값: 위쪽)
    public bool isGlobal = false;       // 전역 바람 여부 (테스트용)

    [Header("Sound Settings")]
    [Tooltip("플레이어가 진입하면 재생할 바람 소리 클립")]
    public AudioClip windClip;
    [Tooltip("바람 소리 볼륨 (기본값 1.0)")]
    [Range(0f, 1f)]
    public float windVolume = 1.0f;
    [Tooltip("바람 소리 피치 (기본값 1.0)")]
    [Range(0.1f, 3f)]
    public float windPitch = 1.0f;

    [Tooltip("소리 재생 쿨타임 (초) - 재진입 시 소리 방지")]
    public float playCooldown = 1.0f;
    [Tooltip("영역을 벗어날 때 소리를 즉시 끌지 여부 (Loop 사운드는 켜야 함)")]
    public bool stopSoundOnExit = true;

    private string loopId;
    private float lastPlayTime = -999f;

    private void Awake()
    {
        loopId = $"WindArea_{GetInstanceID()}";
    }

    private void OnTriggerEnter(Collider other)
    {
        // 쿨타임 체크
        if (Time.time < lastPlayTime + playCooldown) return;

        // 플레이어가 진입했을 때만 소리 재생
        if (other.GetComponent<TPSController>() != null)
        {
            Debug.Log($"[WindArea] Player entered: {other.name}");
            if (windClip != null)
            {
                lastPlayTime = Time.time; // 쿨타임 시작

                Debug.Log($"[WindArea] Playing sound clip: {windClip.name}");
                
                // SoundManager를 통해 재생 (AudioClip 직접 사용)
                // Loop 설정은 기본적으로 true로 가정 (바람 소리이므로)
                SoundManager.Instance.PlayLoop(windClip, loopId, 0.5f, false, windVolume, windPitch);
            }
            else
            {
                // 클립 없으면 아무것도 안 함 (Warning은 선택적)
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 나가면 소리 정지 (설정된 경우만)
        if (stopSoundOnExit && other.GetComponent<TPSController>() != null)
        {
            // Loop 사운드인 경우에만 정지 가능 (여기선 무조건 Loop로 가정)
             if (windClip != null)
            {
                SoundManager.Instance.StopLoop(loopId, 0.5f);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 리지드바디가 있는 물체(플레이어 등)가 들어오면 힘을 가함
        if (other.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // 방향 정규화 * 세기
            Vector3 windForce = direction.normalized * strength;
            rb.AddForce(windForce, ForceMode.Force);
        }
    }

    // 에디터에서 바람 방향을 눈으로 보기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = GetComponent<BoxCollider>() ? transform.position + GetComponent<BoxCollider>().center : transform.position;
        
        // 바람 방향 화살표 표시
        Vector3 endPos = center + direction.normalized * 3f;
        Gizmos.DrawLine(center, endPos);
        Gizmos.DrawSphere(endPos, 0.2f);
    }
}
