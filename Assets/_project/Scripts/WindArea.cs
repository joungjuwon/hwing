using UnityEngine;

public class WindArea : MonoBehaviour
{
    [Header("Wind Settings")]
    public float strength = 20f;        // 바람의 세기
    public Vector3 direction = Vector3.up; // 바람의 방향 (기본값: 위쪽)
    public bool isGlobal = false;       // 전역 바람 여부 (테스트용)

    [Header("Sound Settings")]
    [Tooltip("플레이어가 진입하면 재생할 바람 소리")]
    public SoundData windSound;
    [Tooltip("소리 재생 쿨타임 (초) - 재진입 시 소리 방지")]
    public float playCooldown = 1.0f;
    [Tooltip("영역을 벗어날 때 소리를 즉시 끌지 여부 (Loop 사운드는 켜야 함)")]
    public bool stopSoundOnExit = true;

    private AudioSource audioSource;
    private float lastPlayTime = -999f;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 쿨타임 체크
        if (Time.time < lastPlayTime + playCooldown) return;

        // 플레이어가 진입했을 때만 소리 재생
        if (other.GetComponent<TPSController>() != null)
        {
            Debug.Log($"[WindArea] Player entered: {other.name}");
            if (windSound != null)
            {
                lastPlayTime = Time.time; // 쿨타임 시작

                // 클립 결정 (랜덤 배열이 있으면 우선 선택, 없으면 단일 클립 사용)
                AudioClip clipToPlay = windSound.clip;
                if (windSound.clips != null && windSound.clips.Length > 0)
                {
                    clipToPlay = windSound.clips[Random.Range(0, windSound.clips.Length)];
                }

                if (clipToPlay != null)
                {
                    Debug.Log($"[WindArea] Playing sound: {windSound.soundName}");
                    audioSource.clip = clipToPlay;
                    audioSource.volume = windSound.volume;
                    audioSource.pitch = windSound.pitch;
                    audioSource.loop = windSound.loop; // Loop 설정 적용
                    audioSource.Play();
                }
                else
                {
                    Debug.LogWarning("[WindArea] SoundData is connected but has no AudioClip (both 'clip' and 'clips' are empty)!");
                }
            }
            else
            {
                Debug.LogWarning("[WindArea] SoundData is missing!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 나가면 소리 정지 (설정된 경우만)
        if (stopSoundOnExit && other.GetComponent<TPSController>() != null)
        {
            audioSource.Stop();
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
