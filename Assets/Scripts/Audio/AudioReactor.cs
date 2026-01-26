using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 오디오 반응형 시스템의 핵심 로직 클래스 (AudioReactor)
/// AudioSource의 파형을 분석하여 현재 볼륨(RMS) 값을 계산하고 이벤트를 발생시킵니다.
/// </summary>
public class AudioReactor : MonoBehaviour
{
    public enum ReactTarget { BGM, SFX, Custom }

    [Header("Settings")]
    [Tooltip("어떤 소리에 반응할지 선택")]
    public ReactTarget reactTarget = ReactTarget.BGM;

    [Tooltip("분석할 오디오 소스 (Custom 모드일 때만 사용)")]
    public AudioSource audioSource; // 변수명 유지하거나 customSource로 변경 가능하지만 기존 연결 유지를 위해 유지

    // 호환성을 위해 남겨둠 (이제 reactTarget이 Custom이 아니면 무시됨)
    // [Tooltip("SoundManager의 BGM 소스와 자동 연결할지 여부")]
    // public bool autoConnectToBGM = true; 

    [Tooltip("볼륨 값 갱신 주기 (초 단위, 성능 최적화용)")]
    public float updateInterval = 0.05f;

    [Tooltip("분석할 샘플 데이터 크기 (64 ~ 8192, 2의 제곱수)")]
    private int sampleSize = 256;

    [Tooltip("소리 감도 증폭 계수")]
    [Range(0.1f, 10f)]
    public float sensitivity = 2.0f;

    // 현재 볼륨 값 (0.0 ~ 1.0)
    private float currentVolume;
    
    // 내부 타이머
    private float timer;

    // 볼륨 변화 이벤트 (매 프레임 또는 주기마다 발생)
    // float 파라미터는 현재 정규화된 볼륨 값입니다.
    public UnityEvent<float> OnUpdateVolume;

    private void Start()
    {
        TryConnectSource();
    }

    private void Update()
    {
        // 연결된 소스가 없으면 지속적으로 연결 시도
        if (audioSource == null)
        {
            TryConnectSource();
            if (audioSource == null) return;
        }

        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            timer = 0f;
            AnalyzeAudio();
        }
    }

    // 오디오 데이터 분석 메서드
    private void AnalyzeAudio()
    {
        if (audioSource == null || !audioSource.isPlaying)
        {
            currentVolume = 0f;
            OnUpdateVolume?.Invoke(currentVolume);
            return;
        }

        float[] samples = new float[sampleSize];
        // 현재 재생 중인 채널의 출력 데이터를 가져옵니다.
        audioSource.GetOutputData(samples, 0);

        float sum = 0f;
        foreach (var x in samples)
        {
            sum += x * x; // 제곱의 합
        }

        // RMS 계산
        float rms = Mathf.Sqrt(sum / sampleSize);
        currentVolume = Mathf.Clamp01(rms * sensitivity);
        OnUpdateVolume?.Invoke(currentVolume);
    }

    private void TryConnectSource()
    {
        if (SoundManager.Instance == null) return;

        switch (reactTarget)
        {
            case ReactTarget.BGM:
                audioSource = SoundManager.Instance.GetBGMSource();
                break;
            case ReactTarget.SFX:
                audioSource = SoundManager.Instance.GetSFXSource();
                break;
            case ReactTarget.Custom:
                // Custom은 인스펙터에서 할당된 audioSource를 그대로 사용
                break;
        }
    }
}
