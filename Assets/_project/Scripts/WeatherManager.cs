using UnityEngine;
using System.Collections;

public enum WeatherState
{
    Sunny,
    Rain
}

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Current Weather")]
    public WeatherState currentState = WeatherState.Sunny;

    [Header("References")]
    [Tooltip("씬의 주 광원 (Directional Light)")]
    public Light mainLight;
    [Tooltip("비 파티클 시스템 (미리 배치해두고 켜고 끄기)")]
    public ParticleSystem rainParticleSystem;
    [Tooltip("빗소리 오디오 소스")]
    public AudioSource rainAudioSource;
    [Tooltip("전역 바람 영역 (선택 사항)")]
    public WindArea globalWind;

    [Header("Sunny Settings")]
    public float sunnyLightIntensity = 1.5f;
    public Color sunnyFogColor = new Color(0.6f, 0.7f, 0.8f);
    public float sunnyFogDensity = 0.005f;
    public float sunnyWindStrength = 5f;

    [Header("Rain Settings")]
    public float rainLightIntensity = 0.5f;
    public Color rainFogColor = new Color(0.3f, 0.35f, 0.4f);
    public float rainFogDensity = 0.02f;
    public float rainWindStrength = 20f;
    [Range(0f, 1f)] public float rainVolume = 1.0f;

    [Header("Transition")]
    public float transitionDuration = 2.0f;

    private float targetLightIntensity;
    private Color targetFogColor;
    private float targetFogDensity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 안개 활성화
        RenderSettings.fog = true;

        // 초기 상태 적용 (즉시)
        ApplyWeather(currentState, true);
    }

    private void Update()
    {
        // 조명 및 안개 부드러운 전환
        if (mainLight != null)
        {
            mainLight.intensity = Mathf.Lerp(mainLight.intensity, targetLightIntensity, Time.deltaTime / transitionDuration);
        }

        if (RenderSettings.fog)
        {
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, Time.deltaTime / transitionDuration);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, Time.deltaTime / transitionDuration);
        }
    }

    public void SetWeather(WeatherState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        ApplyWeather(currentState, false);
    }

    private void ApplyWeather(WeatherState state, bool immediate)
    {
        Debug.Log($"[WeatherManager] Changing weather to {state}");

        switch (state)
        {
            case WeatherState.Sunny:
                targetLightIntensity = sunnyLightIntensity;
                targetFogColor = sunnyFogColor;
                targetFogDensity = sunnyFogDensity;

                if (rainParticleSystem != null) rainParticleSystem.Stop();
                if (rainAudioSource != null) StartCoroutine(FadeOutSound(rainAudioSource, transitionDuration));
                
                if (globalWind != null) globalWind.strength = sunnyWindStrength;
                break;

            case WeatherState.Rain:
                targetLightIntensity = rainLightIntensity;
                targetFogColor = rainFogColor;
                targetFogDensity = rainFogDensity;

                if (rainParticleSystem != null) rainParticleSystem.Play();
                if (rainAudioSource != null) 
                {
                    rainAudioSource.gameObject.SetActive(true);
                    rainAudioSource.Play();
                    StartCoroutine(FadeInSound(rainAudioSource, rainVolume, transitionDuration));
                }

                if (globalWind != null) globalWind.strength = rainWindStrength;
                break;
        }

        if (immediate)
        {
            if (mainLight != null) mainLight.intensity = targetLightIntensity;
            RenderSettings.fogColor = targetFogColor;
            RenderSettings.fogDensity = targetFogDensity;
            
            StopAllCoroutines();
            if (rainAudioSource != null)
            {
                if (state == WeatherState.Rain) rainAudioSource.volume = rainVolume;
                else 
                {
                    rainAudioSource.volume = 0f;
                    rainAudioSource.Stop();
                }
            }
        }
    }

    private IEnumerator FadeOutSound(AudioSource audio, float duration)
    {
        float startVol = audio.volume;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            audio.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        audio.Stop();
        audio.volume = startVol; 
    }

    private IEnumerator FadeInSound(AudioSource audio, float targetVol, float duration)
    {
        audio.volume = 0f;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            audio.volume = Mathf.Lerp(0f, targetVol, t / duration);
            yield return null;
        }
        audio.volume = targetVol;
    }

    [ContextMenu("Toggle Weather")]
    public void ToggleWeather()
    {
        SetWeather(currentState == WeatherState.Sunny ? WeatherState.Rain : WeatherState.Sunny);
    }
}