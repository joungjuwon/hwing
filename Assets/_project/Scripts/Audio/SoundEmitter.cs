using UnityEngine;
using System.Collections;

/// <summary>
/// 오브젝트에 부착하여 SoundManager가 중앙에서 관리할 수 있는 사운드 이미터.
/// 자체 AudioSource를 가지며, 클립/루프/딜레이 설정을 인스펙터에서 지정합니다.
/// OnEnable 시 SoundManager에 자동 등록, OnDisable 시 해제됩니다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    [Header("Identification")]
    [Tooltip("SoundManager에서 이 이미터를 식별하는 ID (비워두면 오브젝트 이름 사용)")]
    public string emitterId;

    [Header("Clip Settings")]
    [Tooltip("재생할 오디오 클립 (단일)")]
    public AudioClip clip;

    [Tooltip("여러 클립 등록 시 매번 랜덤 선택 (이 배열이 있으면 clip 무시)")]
    public AudioClip[] clips;

    [Header("Playback Settings")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Tooltip("랜덤 피치 사용 여부")]
    public bool useRandomPitch = false;

    [Tooltip("랜덤 피치 최소값")]
    [Range(0.1f, 3f)]
    public float minPitch = 0.9f;

    [Tooltip("랜덤 피치 최대값")]
    [Range(0.1f, 3f)]
    public float maxPitch = 1.1f;

    [Tooltip("반복 재생 여부")]
    public bool loop = false;

    [Tooltip("활성화 시 자동 재생")]
    public bool playOnEnable = true;

    [Header("Random Delay (Loop 모드 전용)")]
    [Tooltip("루프 사이에 랜덤 딜레이를 적용할지 여부")]
    public bool useRandomDelay = false;

    [Tooltip("딜레이 최소값 (초)")]
    [Min(0f)]
    public float minDelay = 1f;

    [Tooltip("딜레이 최대값 (초)")]
    [Min(0f)]
    public float maxDelay = 5f;

    [Header("3D Sound Settings")]
    [Range(0f, 1f)]
    [Tooltip("0 = 2D, 1 = 3D")]
    public float spatialBlend = 1f;

    // 런타임 상태
    [Header("Runtime Status (Read Only)")]
    [SerializeField] private bool _isPlaying = false;
    public bool IsPlaying => _isPlaying;

    private AudioSource audioSource;
    private Coroutine delayLoopCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // ID 자동 설정
        if (string.IsNullOrEmpty(emitterId))
            emitterId = gameObject.name;
    }

    private void OnEnable()
    {
        SetupAudioSource();

        // SoundManager에 등록
        if (SoundManager.Instance != null)
            SoundManager.Instance.RegisterEmitter(this);

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();

        // SoundManager에서 해제
        if (SoundManager.Instance != null)
            SoundManager.Instance.UnregisterEmitter(this);
    }

    /// <summary>
    /// AudioSource 설정을 인스펙터 값으로 동기화합니다.
    /// </summary>
    public void SetupAudioSource()
    {
        if (audioSource == null) return;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        
        // 랜덤 피치가 아닐 때만 기본 피치 적용 (랜덤일 땐 Play 시점에 결정)
        if (!useRandomPitch)
            audioSource.pitch = pitch;
            
        audioSource.spatialBlend = spatialBlend;
        audioSource.dopplerLevel = 0f;

        // SFX 믹서 그룹 연결 (SoundManager가 있으면)
        if (SoundManager.Instance != null && SoundManager.Instance.sfxGroup != null)
            audioSource.outputAudioMixerGroup = SoundManager.Instance.sfxGroup;

        // 일반 루프는 AudioSource 자체 루프 사용 (랜덤 딜레이 없을 때)
        audioSource.loop = loop && !useRandomDelay;
    }

    /// <summary>
    /// 재생을 시작합니다.
    /// </summary>
    public void Play()
    {
        if (audioSource == null) return;

        AudioClip playClip = GetClip();
        if (playClip == null) return;

        audioSource.clip = playClip;
        SetupAudioSource(); // 기본 설정 적용

        // 피치 결정
        float currentPitch = useRandomPitch ? Random.Range(minPitch, maxPitch) : pitch;
        audioSource.pitch = currentPitch;

        if (loop && useRandomDelay)
        {
            // 랜덤 딜레이 루프 → 코루틴으로 관리
            if (delayLoopCoroutine != null) StopCoroutine(delayLoopCoroutine);
            delayLoopCoroutine = StartCoroutine(RandomDelayLoopRoutine());
        }
        else
        {
            audioSource.Play();
        }

        _isPlaying = true;
    }

    /// <summary>
    /// 재생을 정지합니다.
    /// </summary>
    public void Stop()
    {
        if (delayLoopCoroutine != null)
        {
            StopCoroutine(delayLoopCoroutine);
            delayLoopCoroutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        _isPlaying = false;
    }

    /// <summary>
    /// 현재 클립을 선택합니다 (배열이 있으면 랜덤).
    /// </summary>
    private AudioClip GetClip()
    {
        if (clips != null && clips.Length > 0)
            return clips[Random.Range(0, clips.Length)];
        return clip;
    }

    /// <summary>
    /// 랜덤 딜레이로 반복 재생하는 코루틴
    /// </summary>
    private IEnumerator RandomDelayLoopRoutine()
    {
        while (true)
        {
            AudioClip playClip = GetClip();
            if (playClip == null) yield break;

            audioSource.clip = playClip;
            
            // 매번 랜덤 피치 적용
            float currentPitch = useRandomPitch ? Random.Range(minPitch, maxPitch) : pitch;
            audioSource.pitch = currentPitch;
            
            audioSource.Play();

            // 클립이 끝날 때까지 대기
            yield return new WaitForSeconds(playClip.length / Mathf.Max(currentPitch, 0.01f));

            // 랜덤 딜레이
            float delay = Random.Range(minDelay, maxDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// 인스펙터에서 값 변경 시 AudioSource에 즉시 반영
    /// </summary>
    private void OnValidate()
    {
        if (maxDelay < minDelay) maxDelay = minDelay;
        if (string.IsNullOrEmpty(emitterId) && gameObject != null)
            emitterId = gameObject.name;
    }
}
