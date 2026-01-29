using UnityEngine;

/// <summary>
/// 오디오 볼륨에 따라 오브젝트의 크기(Scale)를 변화시키는 예제 클래스
/// AudioReactor의 이벤트를 구독하여 동작합니다.
/// </summary>
public class AudioScaleVisualizer : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("연결할 AudioReactor")]
    public AudioReactor audioReactor;

    [Header("Scale Settings")]
    [Tooltip("기본 크기 (소리가 없을 때)")]
    public Vector3 minScale = Vector3.one;

    [Tooltip("최대 크기 (소리가 클 때)")]
    public Vector3 maxScale = Vector3.one * 1.5f;

    [Tooltip("부드러운 움직임을 위한 보간 속도")]
    public float lerpSpeed = 10f;

    // 적용할 목표 크기
    private Vector3 targetScale;

    private void Start()
    {
        // 초기화: 현재 크기를 최소 크기로 시작
        targetScale = minScale;
        transform.localScale = minScale;
    }

    private void OnEnable()
    {
        // 이벤트 구독
        if (audioReactor != null)
        {
            audioReactor.OnUpdateVolume.AddListener(OnUpdateVolume);
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (audioReactor != null)
        {
            audioReactor.OnUpdateVolume.RemoveListener(OnUpdateVolume);
        }
    }

    private void Update()
    {
        // 매 프레임 부드럽게 크기 변경 (Linear Interpolation)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * lerpSpeed);
    }

    // AudioReactor에서 호출되는 메서드
    public void OnUpdateVolume(float currentVolume)
    {
        // 볼륨 값(0~1)을 기준으로 최소~최대 크기 사이의 값을 계산
        targetScale = Vector3.Lerp(minScale, maxScale, currentVolume);
    }
}
