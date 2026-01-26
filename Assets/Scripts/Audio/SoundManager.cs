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

    // 배경음악용 오디오 소스 (외부 접근을 위해 프로퍼티로 제공하거나 메서드로 제공)
    // AudioReactor 등에서 접근할 수 있도록 Get 메서드 추가
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    public AudioSource GetBGMSource() => bgmSource;
    public AudioSource GetSFXSource() => sfxSource;

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
    /// <param name="forceOneShot">true면 루프 설정이 있어도 1회만 재생 (무한 재귀 방지용)</param>
    /// <returns>재생된 Audio Clip (없으면 null)</returns>
    public AudioClip PlaySFX(SoundData data, bool forceOneShot = false)
    {
        if (data == null) return null;

        // 랜덤 루프 설정이 켜져 있고, 강제 1회 재생이 아니라면 -> 루퍼 실행
        if (data.useRandomLoop && !forceOneShot)
        {
            // 헬퍼 오브젝트 생성
            GameObject looperObj = new GameObject($"Loop_{data.soundName}");
            SoundRandomLooper looper = looperObj.AddComponent<SoundRandomLooper>();
            looper.Init(data);
            return null; // 루퍼가 알아서 재생하므로 여기선 null 반환
        }
        
        // 재생할 클립 결정 (배열이 있으면 배열에서 랜덤, 없으면 단일 클립)
        AudioClip playClip = data.clip;
        if (data.clips != null && data.clips.Length > 0)
        {
            playClip = data.clips[Random.Range(0, data.clips.Length)];
        }

        if (playClip == null) return null;

        // 랜덤 변수 설정
        float finalVolume = data.volume;
        float finalPitch = data.pitch;

        if (data.useRandomVariance)
        {
            // 볼륨 랜덤: Variance/2 만큼 빼고 더하는 범위
            float volVar = data.volumeVariance * 0.5f;
            finalVolume += Random.Range(-volVar, volVar);
            
            // 피치 랜덤: Variance/2 만큼 빼고 더하는 범위
            float pitchVar = data.pitchVariance * 0.5f;
            finalPitch += Random.Range(-pitchVar, pitchVar);
        }

        // 효과음의 피치 조절
        // 주의: PlayOneShot은 오디오 소스의 피치에 영향을 받으므로,
        // 동시에 여러 소리가 재생될 때 피치 변경이 다른 소리에도 즉시 영향을 줄 수 있는 한계가 있습니다.
        // 완벽한 구현을 위해서는 AudioSource Pooling 또는 Instantiate(Prefab) 방식이 필요합니다.
        // 현재는 간단한 구현을 위해 그대로 적용합니다.
        sfxSource.pitch = finalPitch;
        sfxSource.PlayOneShot(playClip, finalVolume);
        
        // 피치는 상태를 유지하므로 다음 재생 시 데이터에 의해 다시 덮어씌워져야 정상 작동합니다.
        // (항상 SoundData를 통해 재생한다면 문제없음)

        return playClip;
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
