using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    [Tooltip("비가 올 때 활성화할 오브젝트들")]
    public GameObject[] rainPropsToActivate;
    [Tooltip("비가 올 때 Y축을 내릴 터레인")]
    public Terrain rainTargetTerrain;
    [Tooltip("터레인을 내릴 높이")]
    public float terrainLowerAmount = 5f;
    [Tooltip("비가 올 때 Y축을 올릴 물 오브젝트들")]
    public Transform[] waterObjectsToRaise;
    [Tooltip("물을 올릴 높이")]
    public float waterRaiseAmount = 5f;
    [Tooltip("터레인이 이동하는 데 걸리는 시간")]
    public float terrainTransitionDuration = 2.0f;
    [Tooltip("물이 이동하는 데 걸리는 시간")]
    public float waterTransitionDuration = 2.0f;

    [Header("Transition")]
    public float transitionDuration = 2.0f; // 조명 및 안개용

    private float targetLightIntensity;
    private Color targetFogColor;
    private float targetFogDensity;

    private Vector3 originalTerrainPosition;
    private Vector3 startTerrainPosition; // 이동 시작 위치
    private Vector3 targetTerrainPosition;
    private float currentTerrainTime; // 터레인 이동 경과 시간
    private bool terrainPositionBackedUp = false;

    private List<Vector3> originalWaterPositions = new List<Vector3>();
    private List<Vector3> startWaterPositions = new List<Vector3>(); // 이동 시작 위치들
    private List<Vector3> targetWaterPositions = new List<Vector3>();
    private float currentWaterTime; // 물 이동 경과 시간
    private bool waterPositionsBackedUp = false;

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

        // 터레인 및 물 높이 부드러운 전환
        if (terrainPositionBackedUp && rainTargetTerrain != null && currentTerrainTime < terrainTransitionDuration)
        {
            currentTerrainTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentTerrainTime / terrainTransitionDuration);
            rainTargetTerrain.transform.position = Vector3.Lerp(startTerrainPosition, targetTerrainPosition, t);
        }

        if (waterPositionsBackedUp && waterObjectsToRaise != null && currentWaterTime < waterTransitionDuration)
        {
            currentWaterTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentWaterTime / waterTransitionDuration);

            for (int i = 0; i < waterObjectsToRaise.Length; i++)
            {
                if (waterObjectsToRaise[i] != null && i < startWaterPositions.Count && i < targetWaterPositions.Count)
                {
                    // 시작 위치에서 목표 위치로 시간(t)에 따라 이동
                    waterObjectsToRaise[i].position = Vector3.Lerp(startWaterPositions[i], targetWaterPositions[i], t);
                }
            }
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

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate)
                    {
                        if (prop != null) prop.SetActive(false);
                    }
                }

                // 터레인은 비가 그쳐도 다시 올라오지 않도록 복구 로직 제거
                // if (terrainPositionBackedUp && rainTargetTerrain != null)
                // {
                //     targetTerrainPosition = originalTerrainPosition;
                // }
                if (waterPositionsBackedUp && waterObjectsToRaise != null)
                {
                    targetWaterPositions = new List<Vector3>(originalWaterPositions);
                }
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

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate)
                    {
                        if (prop != null) prop.SetActive(true);
                    }
                }

                if (rainTargetTerrain != null)
                {
                    if (!terrainPositionBackedUp)
                    {
                        originalTerrainPosition = rainTargetTerrain.transform.position;
                        terrainPositionBackedUp = true;
                    }
                    targetTerrainPosition = originalTerrainPosition - new Vector3(0, terrainLowerAmount, 0);
                }

                if (waterObjectsToRaise != null && waterObjectsToRaise.Length > 0)
                {
                    if (!waterPositionsBackedUp)
                    {
                        originalWaterPositions.Clear();
                        foreach (var water in waterObjectsToRaise)
                        {
                            if (water != null) originalWaterPositions.Add(water.position);
                        }
                        waterPositionsBackedUp = true;
                    }

                    targetWaterPositions.Clear();
                    foreach (var originalPos in originalWaterPositions)
                    {
                        targetWaterPositions.Add(originalPos + new Vector3(0, waterRaiseAmount, 0));
                    }
                }
                break;
        }

        // 이동 시작 전 현재 위치 저장 및 타이머 초기화
        if (rainTargetTerrain != null)
        {
            startTerrainPosition = rainTargetTerrain.transform.position;
            currentTerrainTime = 0f;
        }

        if (waterObjectsToRaise != null)
        {
            startWaterPositions.Clear();
            foreach (var water in waterObjectsToRaise)
            {
                if (water != null) startWaterPositions.Add(water.position);
            }
            currentWaterTime = 0f;
        }

        if (immediate)
        {
            if (mainLight != null) mainLight.intensity = targetLightIntensity;
            RenderSettings.fogColor = targetFogColor;
            RenderSettings.fogDensity = targetFogDensity;
            
            StopAllCoroutines();
            if (rainAudioSource != null)
            {
                if (state == WeatherState.Rain)
                {
                    rainAudioSource.volume = rainVolume;
                    if (!rainAudioSource.isPlaying) rainAudioSource.Play();
                }
                else 
                {
                    rainAudioSource.volume = 0f;
                    rainAudioSource.Stop();
                }
            }

            if (rainPropsToActivate != null)
            {
                foreach (var prop in rainPropsToActivate)
                {
                    if (prop != null) prop.SetActive(state == WeatherState.Rain);
                }
            }

            if (terrainPositionBackedUp && rainTargetTerrain != null)
            {
                rainTargetTerrain.transform.position = targetTerrainPosition;
                currentTerrainTime = terrainTransitionDuration; // 즉시 완료 처리
            }
            if (waterPositionsBackedUp && waterObjectsToRaise != null)
            {
                int posIndex = 0;
                for (int i = 0; i < waterObjectsToRaise.Length; i++)
                {
                    if (waterObjectsToRaise[i] != null)
                    {
                        if (posIndex < targetWaterPositions.Count)
                        {
                            waterObjectsToRaise[i].position = targetWaterPositions[posIndex];
                            posIndex++;
                        }
                    }
                }
                currentWaterTime = waterTransitionDuration; // 즉시 완료 처리
            }
        }
    }

    private void OnDestroy()
    {
        RestoreEnvironment();
    }

    private void OnApplicationQuit()
    {
        RestoreEnvironment();
    }

    private void RestoreEnvironment()
    {
        if (terrainPositionBackedUp && rainTargetTerrain != null)
        {
            rainTargetTerrain.transform.position = originalTerrainPosition;
        }

        if (waterPositionsBackedUp && waterObjectsToRaise != null)
        {
            for (int i = 0; i < waterObjectsToRaise.Length; i++)
            {
                if (waterObjectsToRaise[i] != null && i < originalWaterPositions.Count)
                {
                    waterObjectsToRaise[i].position = originalWaterPositions[i];
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