using UnityEngine;
using UnityEngine.Audio; // 오디오 믹서를 사용하기 위해 추가

/// <summary>
/// 사운드 매니저 클래스
/// 싱글톤 패턴을 사용하여 게임 전체에서 유일한 인스턴스로 관리됩니다.
/// 배경음악(BGM)과 효과음(SFX) 재생 기능을 제공합니다.
/// AudioMixer와 SoundData를 지원합니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 어디서든 SoundManager.Instance로 접근 가능
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer Settings")]
    [Tooltip("메인 오디오 믹서")]
    public AudioMixer mainMixer;
    [Tooltip("배경음악용 믹서 그룹")]
    public AudioMixerGroup bgmGroup;
    [Tooltip("효과음용 믹서 그룹")]
    public AudioMixerGroup sfxGroup;

    // 배경음악용 오디오 소스
    private AudioSource bgmSource;
    // 효과음용 오디오 소스
    private AudioSource sfxSource;

    private void Awake()
    {
        // 싱글톤 초기화 로직
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않도록 설정
            InitializeSources();
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성된 객체 파괴
        }
    }

    // 오디오 소스 컴포넌트 초기화 및 설정
    private void InitializeSources()
    {
        // AudioSource 컴포넌트가 없으면 동적으로 추가
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // 믹서 그룹 연결 (인스펙터에서 할당된 경우)
        if (bgmGroup != null) bgmSource.outputAudioMixerGroup = bgmGroup;
        if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;

        // BGM 설정: 반복 재생됨
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // SFX 설정: 반복 재생 안 함
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// 배경음악을 재생합니다.
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volume">볼륨 (기본값 1.0)</param>
    public void PlayBGM(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    /// <summary>
    /// SoundData를 사용하여 배경음악을 재생합니다.
    /// </summary>
    /// <param name="data">사운드 데이터 ScriptableObject</param>
    public void PlayBGM(SoundData data)
    {
        if (data == null || data.clip == null) return;

        bgmSource.clip = data.clip;
        bgmSource.volume = data.volume;
        bgmSource.pitch = data.pitch;
        bgmSource.loop = data.loop; // 데이터 설정에 따름 (보통 true)
        bgmSource.Play();
    }

    /// <summary>
    /// 효과음을 재생합니다. (중첩 재생 가능)
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volume">볼륨 (기본값 1.0)</param>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        // PlayOneShot을 사용하여 여러 효과음이 겹쳐서 들릴 수 있게 함
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// SoundData를 사용하여 효과음을 재생합니다.
    /// </summary>
    /// <param name="data">사운드 데이터 ScriptableObject</param>
    public void PlaySFX(SoundData data)
    {
        if (data == null || data.clip == null) return;

        // 효과음의 피치 조절을 위해 일시적으로 설정을 변경할 수 있으나, 
        // PlayOneShot은 피치를 개별적으로 지원하지 않으므로 오디오 소스의 피치를 변경해야 합니다.
        // 다만 이렇게 하면 동시에 재생되는 다른 소리에도 영향이 갈 수 있어 주의가 필요합니다.
        // 완벽한 구현을 위해서는 효과음용 오디오 소스를 풀링(Pooling) 방식으로 여러 개 둬야 합니다.
        // 여기서는 간단하게 현재 소스의 피치를 변경하고 재생합니다.
        sfxSource.pitch = data.pitch;
        sfxSource.PlayOneShot(data.clip, data.volume);
        
        // 피치 복구 (다음에 재생될 소리를 위해) - 엄밀히는 딜레이를 줘야하지만 간단한 구현을 위해 생략하거나
        // 코루틴으로 처리해야 합니다. 현재 구조에서는 1.0으로 즉시 복구하면 소리가 이상해질 수 있습니다.
        // 일단 피치 변경 기능을 넣었으므로 복구 로직은 생략합니다. (계속 설정된 피치로 유지됨)
    }

    /// <summary>
    /// 배경음악을 정지합니다.
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // --- 볼륨 조절 기능 (AudioMixer 파라미터 제어) ---
    // 주의: AudioMixer에서 해당 파라미터(MasterVolume 등)를 Expose 해야 작동합니다.
    // 볼륨은 보통 로그 스케일(Logorithmic)로 조절해야 자연스럽습니다 (0.0001 ~ 1 -> -80dB ~ 0dB)

    public void SetMasterVolume(float volume)
    {
        if (mainMixer == null) return;
        // 슬라이더 값(0~1)을 데시벨(-80~0)로 변환
        float db = volume <= 0.001f ? -80f : Mathf.Log10(volume) * 20f;
        mainMixer.SetFloat("MasterVolume", db);
    }

    public void SetBGMVolume(float volume)
    {
        if (mainMixer == null) return;
        float db = volume <= 0.001f ? -80f : Mathf.Log10(volume) * 20f;
        mainMixer.SetFloat("BGMVolume", db);
    }

    public void SetSFXVolume(float volume)
    {
        if (mainMixer == null) return;
        float db = volume <= 0.001f ? -80f : Mathf.Log10(volume) * 20f;
        mainMixer.SetFloat("SFXVolume", db);
    }
}
