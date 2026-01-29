using UnityEngine;

/// <summary>
/// 꽃이나 풀이 바람에 흔들리는 효과를 주는 스크립트.
/// AudioReactor와 연동하면 소리에 반응하여 더 격렬하게 흔들립니다.
/// </summary>
public class FlowerSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("중요: 오브젝트의 피벗(중심축)이 '밑동'에 있어야 자연스럽습니다.\n피벗이 중앙/상단에 있다면 빈 오브젝트를 부모로 만들고 그 부모를 흔드세요.")]
    public float swayAngle = 10f;

    [Tooltip("흔들리는 속도")]
    public float swaySpeed = 2f;

    [Tooltip("흔들릴 회전 축 (보통 앞뒤는 X, 좌우는 Z)")]
    public Vector3 swayAxis = new Vector3(0, 0, 1);

    [Header("Audio Interaction")]
    [Tooltip("연결할 AudioReactor (선택 사항)")]
    public AudioReactor audioReactor;

    [Tooltip("소리에 반응하는 강도 (기본 각도 * (1 + 볼륨 * 이 값))")]
    public float reactionMultiplier = 2f;

    [Tooltip("소리에 반응할 때의 부드러움 (값이 클수록 민감, 작을수록 부드러움)")]
    public float smoothness = 5f;

    [Tooltip("체크하면 한 방향(0~1)으로만 흔들립니다. (바람이 한쪽에서 불 때 유용)")]
    public bool unidirectional = false;

    // 내부 변수
    private Quaternion startRotation;
    private float randomOffset;
    private float currentAudioVolume = 0f;
    private float smoothedVolume = 0f;
    private float currentPhase = 0f;

    private void Start()
    {
        startRotation = transform.localRotation;
        randomOffset = Random.Range(0f, 100f);
        currentPhase = randomOffset;
    }

    private void OnEnable()
    {
        if (audioReactor != null)
        {
            audioReactor.OnUpdateVolume.AddListener(OnUpdateVolume);
        }
    }

    private void OnDisable()
    {
        if (audioReactor != null)
        {
            audioReactor.OnUpdateVolume.RemoveListener(OnUpdateVolume);
        }
    }

    private void OnUpdateVolume(float volume)
    {
        currentAudioVolume = volume;
    }

    private void Update()
    {
        // 1. 볼륨 값을 부드럽게 보간
        smoothedVolume = Mathf.Lerp(smoothedVolume, currentAudioVolume, Time.deltaTime * smoothness);

        // 2. 오디오에 따른 증폭 계수
        float angleMultiplier = 1f + (smoothedVolume * reactionMultiplier);

        // 3. 위상 누적
        currentPhase += Time.deltaTime * swaySpeed;

        // 4. Sin 계산
        float t = Mathf.Sin(currentPhase);

        // 한 방향 모드일 경우: -1~1 범위를 0~1 범위로 변환
        if (unidirectional)
        {
            t = (t + 1f) * 0.5f;
        }

        // 5. 최종 회전 적용
        transform.localRotation = startRotation * Quaternion.Euler(swayAxis * t * swayAngle * angleMultiplier);
    }
}
