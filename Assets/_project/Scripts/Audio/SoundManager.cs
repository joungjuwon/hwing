using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

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
    // 배경음악용 오디오 소스 (더블 버퍼링을 위해 2개 사용)
    private AudioSource bgmSourceMain;
    private AudioSource bgmSourceSecondary;
    private bool isUsingMainBGM = true;
    
    private AudioSource sfxSource;

    private Coroutine bgmFadeCoroutine;

    // 활성화된 반복 재생 사운드들을 관리하는 딕셔너리 (ID, AudioSource)
    private Dictionary<string, AudioSource> activeLoops = new Dictionary<string, AudioSource>();

    // 활성화된 랜덤 루프 코루틴 관리
    private Dictionary<int, Coroutine> activeRandomLoops = new Dictionary<int, Coroutine>();
    private int nextLoopId = 0;

    public AudioSource GetBGMSource() => isUsingMainBGM ? bgmSourceMain : bgmSourceSecondary;
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
        bgmSourceMain = gameObject.AddComponent<AudioSource>();
        bgmSourceSecondary = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // 믹서 그룹 연결 (인스펙터에서 할당된 경우)
        if (bgmGroup != null) 
        {
            bgmSourceMain.outputAudioMixerGroup = bgmGroup;
            bgmSourceSecondary.outputAudioMixerGroup = bgmGroup;
        }
        if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;

        // BGM 설정: 반복 재생됨
        bgmSourceMain.loop = true;
        bgmSourceMain.playOnAwake = false;
        
        bgmSourceSecondary.loop = true;
        bgmSourceSecondary.playOnAwake = false;

        // SFX 설정: 반복 재생 안 함
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// 배경음악을 재생합니다.
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volume">볼륨 (기본값 1.0)</param>
    /// <summary>
    /// 배경음악을 재생합니다. (Cross-Fade 지원)
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volume">목표 볼륨</param>
    /// <param name="loop">반복 여부</param>
    /// <param name="fadeDuration">전환 시간 (초). 0이면 즉시 전환.</param>
    public void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true, float fadeDuration = 1.0f)
    {
        if (clip == null) return;

        // 현재 재생 중인 소스 가져오기
        AudioSource currentSource = isUsingMainBGM ? bgmSourceMain : bgmSourceSecondary;
        AudioSource nextSource = isUsingMainBGM ? bgmSourceSecondary : bgmSourceMain;

        // 이미 같은 클립이 재생 중이라면
        if (currentSource.isPlaying && currentSource.clip == clip)
        {
            // 볼륨/루프 상태만 업데이트하고 페이드 효과 없이 유지
            // (필요 시 볼륨 페이드는 따로 구현 가능하나 여기선 즉시 적용)
            currentSource.volume = volume;
            currentSource.loop = loop;
            return;
        }

        // 기존 페이드 코루틴 중단
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);

        if (fadeDuration > 0f)
        {
            // Cross-Fade 시작
            bgmFadeCoroutine = StartCoroutine(CrossFadeRoutine(currentSource, nextSource, clip, volume, loop, fadeDuration));
            // 활성 소스 플래그 토글
            isUsingMainBGM = !isUsingMainBGM;
        }
        else
        {
            // 즉시 전환
            currentSource.Stop();
            
            nextSource.clip = clip;
            nextSource.volume = volume;
            nextSource.loop = loop;
            nextSource.Play();
            
            isUsingMainBGM = !isUsingMainBGM;
        }
    }

    private System.Collections.IEnumerator CrossFadeRoutine(AudioSource current, AudioSource next, AudioClip newClip, float targetVolume, bool loop, float duration)
    {
        float timer = 0f;
        float startVolume = current.volume;

        // 다음 곡 준비 및 재생 시작 (볼륨 0부터)
        next.clip = newClip;
        next.loop = loop;
        next.volume = 0f;
        next.Play();

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 현재 곡 Fade Out
            current.volume = Mathf.Lerp(startVolume, 0f, t);
            // 다음 곡 Fade In
            next.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        current.Stop();
        current.volume = startVolume; // 볼륨 복구 (재활용 위해)
        next.volume = targetVolume;
    }

    /// <summary>
    /// SoundData를 사용하여 배경음악을 재생합니다.
    /// </summary>
    /// <param name="data">사운드 데이터 ScriptableObject</param>
    public void PlayBGM(SoundData data)
    {
        if (data == null || data.clip == null) return;
        
        // SoundData에는 fade 정보가 없으므로 기본값(1.0f) 사용
        // 피치 조절은 PlayBGM main에서 지원하지 않으므로(AudioSource 자체 피치 변경), 
        // 필요하다면 active source를 가져와서 설정해야 함.
        PlayBGM(data.clip, data.volume, data.loop, 1.0f);
        
        // 현재 활성화된 소스에 피치 적용
        AudioSource activeSource = GetBGMSource();
        if (activeSource != null) activeSource.pitch = data.pitch;
    }

    /// <summary>
    /// 효과음을 재생합니다. (중첩 재생 가능)
    /// </summary>
    /// <param name="clip">재생할 오디오 클립</param>
    /// <param name="volume">볼륨 (기본값 1.0)</param>
    /// <param name="pitch">피치 (기본값 1.0)</param>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        // PlayOneShot을 사용하여 여러 효과음이 겹쳐서 들릴 수 있게 함
        // 단, pitch 조절이 필요하므로, sfxSource의 피치를 일시적으로 변경 (동시다발적 소리에는 적합하지 않을 수 있음)
        // 더 나은 방법: 임시 AudioSource 생성 또는 풀링 사용
        // 여기서는 간단히 sfxSource 사용 (일반 2D 사운드)
        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
        // 피치 복구는 즉시 하면 안됨 (PlayOneShot은 현재 상태를 캡쳐하지 않음?) 
        // -> PlayOneShot은 호출 시점의 pitch를 사용합니다. 
        // 하지만 다음 프레임에 바로 복구하면 소리가 들리기 전에 바뀔 수도 있음. 
        // 안전하게는 코루틴이나 별도 소스 사용 권장. 
        // 여기서는 편의상 복구하되, 0.1초 정도 딜레이를 주거나, 
        // 사실 PlayOneShot은 'Global/2D' 용도라 피치 변경이 빈번하면 별도 소스가 낫음.
        // *수정*: 피치 변경이 필요한 경우 별도의 오디오 소스를 생성해서 재생하고 파괴하는 방식으로 변경합니다.
        
        if (Mathf.Abs(pitch - 1f) > 0.01f)
        {
            PlayClipAtPoint(clip, Vector3.zero, volume, pitch, 0f); // 2D (Spatial Blend 0)
        }
        else
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// 3D 공간에서 효과음을 재생합니다.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="position">재생 위치</param>
    /// <param name="volume">볼륨</param>
    /// <param name="pitch">피치</param>
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        PlayClipAtPoint(clip, position, volume, pitch, 1f); // 3D (Spatial Blend 1)
    }

    // 내부 헬퍼: 임시 오디오 소스 생성 및 재생
    private void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume, float pitch, float spatialBlend)
    {
        GameObject go = new GameObject("TempSFX_" + clip.name);
        go.transform.position = position;
        
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend; // 0 = 2D, 1 = 3D
        source.outputAudioMixerGroup = sfxGroup;
        source.dopplerLevel = 0f; // 도플러 효과 끔 (필요시 조정)
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1f;
        source.maxDistance = 50f; // 적절한 거리 설정

        source.Play();
        Destroy(go, clip.length * (Time.timeScale < 0.01f ? 0.01f : 1f / pitch) + 0.1f); // 피치 고려하여 파괴 시간 설정
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
        bgmSourceMain.Stop();
        bgmSourceSecondary.Stop();
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
    }

    /// <summary>
    /// 반복 재생되는 사운드(예: 빗소리, 환경음)를 재생하고 관리합니다.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="id">사운드 식별 ID (나중에 끄기 위해 필요)</param>
    /// <param name="isBgm">true면 BGM 그룹, false면 SFX 그룹 사용 (환경음은 BGM으로 취급될 수 있음)</param>
    public void PlayLoop(AudioClip clip, string id, float fadeDuration = 0f, bool isBgm = false)
    {
        if (clip == null) return;
        
        // 이미 같은 ID로 재생 중인 소스가 있다면
        if (activeLoops.ContainsKey(id))
        {
            // 클립이 다르면 교체, 같으면 무시
            if (activeLoops[id].clip != clip)
            {
                StopLoop(id);
            }
            else
            {
                return; 
            }
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.outputAudioMixerGroup = isBgm ? bgmGroup : sfxGroup; // 플래그에 따라 그룹 설정
        
        if (fadeDuration > 0f)
        {
            source.volume = 0f;
            source.Play();
            StartCoroutine(FadeIn(source, 1f, fadeDuration));
        }
        else
        {
            source.volume = 1f;
            source.Play();
        }

        activeLoops.Add(id, source);
    }

    /// <summary>
    /// ID로 지정된 반복 사운드를 정지합니다.
    /// </summary>
    public void StopLoop(string id, float fadeDuration = 0f)
    {
        if (activeLoops.ContainsKey(id))
        {
            AudioSource source = activeLoops[id];
            activeLoops.Remove(id);

            if (source != null)
            {
                if (fadeDuration > 0f)
                {
                    StartCoroutine(FadeOutAndDestroy(source, fadeDuration));
                }
                else
                {
                    Destroy(source); // 컴포넌트 제거
                }
            }
        }
    }

    // --- 내부 유틸리티 ---
    private System.Collections.IEnumerator FadeIn(AudioSource source, float targetVol, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (source == null) yield break;
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVol, t / duration);
            yield return null;
        }
        if (source != null) source.volume = targetVol;
    }

    private System.Collections.IEnumerator FadeOutAndDestroy(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float t = 0f;
        while (t < duration)
        {
            if (source == null) yield break;
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        if (source != null) Destroy(source);
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

    // --- Random Loop Management ---
    public int RegisterRandomLoop(SoundData data, float minDelay, float maxDelay)
    {
        int id = nextLoopId++;
        Coroutine routine = StartCoroutine(RandomLoopRoutine(id, data, minDelay, maxDelay));
        activeRandomLoops.Add(id, routine);
        return id;
    }

    public void UnregisterRandomLoop(int id)
    {
        if (activeRandomLoops.ContainsKey(id))
        {
            if (activeRandomLoops[id] != null) StopCoroutine(activeRandomLoops[id]);
            activeRandomLoops.Remove(id);
        }
    }

    private System.Collections.IEnumerator RandomLoopRoutine(int id, SoundData data, float minDelay, float maxDelay)
    {
        while (true)
        {
            if (data == null)
            {
                if (activeRandomLoops.ContainsKey(id)) activeRandomLoops.Remove(id);
                yield break;
            }

            // 재생 (SoundManager의 PlaySFX 사용)
            AudioClip playedClip = PlaySFX(data, true);

            // 클립 길이만큼 대기
            if (playedClip != null)
                yield return new WaitForSeconds(playedClip.length);
            else
                yield return new WaitForSeconds(1f);

            // 랜덤 딜레이 적용
            float delay = Random.Range(minDelay, maxDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// 시퀀스 연출용 SFX 재생 (Intro 등) - 의미론적 래퍼
    /// </summary>
    public void PlaySequenceSFX(SoundData data)
    {
        PlaySFX(data, true);
    }

    // ========== SoundEmitter Management ==========

    private List<SoundEmitter> registeredEmitters = new List<SoundEmitter>();

    /// <summary>
    /// SoundEmitter를 등록합니다. (SoundEmitter.OnEnable에서 자동 호출)
    /// </summary>
    public void RegisterEmitter(SoundEmitter emitter)
    {
        if (emitter != null && !registeredEmitters.Contains(emitter))
        {
            registeredEmitters.Add(emitter);
        }
    }

    /// <summary>
    /// SoundEmitter를 해제합니다. (SoundEmitter.OnDisable에서 자동 호출)
    /// </summary>
    public void UnregisterEmitter(SoundEmitter emitter)
    {
        if (emitter != null)
        {
            registeredEmitters.Remove(emitter);
        }
    }

    /// <summary>
    /// ID로 등록된 SoundEmitter를 찾습니다.
    /// </summary>
    public SoundEmitter GetEmitter(string id)
    {
        for (int i = 0; i < registeredEmitters.Count; i++)
        {
            if (registeredEmitters[i] != null && registeredEmitters[i].emitterId == id)
                return registeredEmitters[i];
        }
        return null;
    }

    /// <summary>
    /// ID로 지정된 SoundEmitter를 재생합니다.
    /// </summary>
    public void PlayEmitter(string id)
    {
        var emitter = GetEmitter(id);
        if (emitter != null) emitter.Play();
    }

    /// <summary>
    /// ID로 지정된 SoundEmitter를 정지합니다.
    /// </summary>
    public void StopEmitter(string id)
    {
        var emitter = GetEmitter(id);
        if (emitter != null) emitter.Stop();
    }

    /// <summary>
    /// 등록된 모든 SoundEmitter를 재생합니다.
    /// </summary>
    public void PlayAllEmitters()
    {
        for (int i = 0; i < registeredEmitters.Count; i++)
        {
            if (registeredEmitters[i] != null) registeredEmitters[i].Play();
        }
    }

    /// <summary>
    /// 등록된 모든 SoundEmitter를 정지합니다.
    /// </summary>
    public void StopAllEmitters()
    {
        for (int i = 0; i < registeredEmitters.Count; i++)
        {
            if (registeredEmitters[i] != null) registeredEmitters[i].Stop();
        }
    }

    /// <summary>
    /// 현재 등록된 SoundEmitter 목록을 반환합니다. (에디터/디버그용)
    /// </summary>
    public List<SoundEmitter> GetRegisteredEmitters()
    {
        return registeredEmitters;
    }
}
