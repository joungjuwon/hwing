using UnityEngine;

/// <summary>
/// 오디오 볼륨에 따라 Animator의 파라미터(Float)를 변경하는 스크립트입니다.
/// Blend Tree의 강도나 애니메이션 속도를 제어할 때 사용하세요.
/// </summary>
public class AudioAnimationVisualizer : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("연결할 AudioReactor")]
    public AudioReactor audioReactor;

    [Tooltip("제어할 Animator 컴포넌트")]
    public Animator animator;

    [Header("Animation Settings")]
    [Tooltip("변경할 Animator의 파라미터 이름 (Float 타입)")]
    public string parameterName = "Intensity";

    [Tooltip("소리가 없을 때의 파라미터 값")]
    public float minValue = 0f;

    [Tooltip("소리가 최대일 때의 파라미터 값")]
    public float maxValue = 1f;

    [Tooltip("값이 변하는 부드러움 정도")]
    public float lerpSpeed = 10f;

    // 파라미터 ID (성능 최적화용)
    private int parameterHash;
    private float currentParamValue;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        
        // 문자열 이름 대신 Hash ID를 사용하여 성능 최적화
        parameterHash = Animator.StringToHash(parameterName);
        currentParamValue = minValue;
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

    private void Update()
    {
        if (animator == null) return;

        // 부드럽게 값 적용
        float newValue = Mathf.Lerp(animator.GetFloat(parameterHash), currentParamValue, Time.deltaTime * lerpSpeed);
        animator.SetFloat(parameterHash, newValue);
    }

    // AudioReactor에서 호출
    public void OnUpdateVolume(float volume)
    {
        // 0~1 사이의 volume 값을 min~max 사이값으로 변환
        currentParamValue = Mathf.Lerp(minValue, maxValue, volume);
    }
}
