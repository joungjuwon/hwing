using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum WeatherState
{
    Sunny,
    Rain,
    RainStop,
    Windy
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
    [Tooltip("빗소리 오디오 클립")]
    public AudioClip rainClip;
    [Tooltip("바람소리 오디오 클립 (써니 상태일 때 재생)")]
    public AudioClip windClip;
    [Tooltip("전역 바람 영역 (선택 사항)")]
    public WindArea globalWind;

    [Header("BGM Settings (Phases)")]
    public AudioClip rainBGM;
    public AudioClip rainStopBGM;
    public AudioClip windBGM;

    [Header("3D Audio Settings")]
    [Tooltip("비가 올 때 물 오브젝트에서 재생할 3D 오디오 클립")]
    public AudioClip waterRainClip;
    [Range(0f, 1f)] public float waterRainVolume = 1.0f;
    public float waterRainMaxDistance = 20.0f;

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

    [Header("RainStop Settings")]
    public float rainStopLightIntensity = 1.0f;
    public Color rainStopFogColor = new Color(0.5f, 0.6f, 0.7f);
    public float rainStopFogDensity = 0.01f;

    [Header("Windy Settings")]
    public float windyLightIntensity = 1.2f;
    public Color windyFogColor = new Color(0.6f, 0.65f, 0.7f);
    public float windyFogDensity = 0.008f;
    public float windyWindStrength = 30f;

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
            // DontDestroyOnLoad(gameObject); // WeatherManager는 보통 씬에 포함되므로 필요 없음
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

        // 초기 상태 적용 (즉시, 오디오 재생 안 함 - 타이틀 BGM 유지)
        ApplyWeather(currentState, true, false);
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
        ApplyWeather(currentState, false, true); // 날씨 변경 시에는 오디오 재생
    }

    private void ApplyWeather(WeatherState state, bool immediate, bool playAudio = true)
    {
        Debug.Log($"[WeatherManager] Changing weather to {state}");

        switch (state)
        {
            case WeatherState.Sunny:
                targetLightIntensity = sunnyLightIntensity;
                targetFogColor = sunnyFogColor;
                targetFogDensity = sunnyFogDensity;

                if (rainParticleSystem != null) rainParticleSystem.Stop();
                
                // Sunny는 보통 조용한 상태이거나 초기 상태 (BGM 정지 또는 타이틀 BGM)
                // 필요하다면 여기서도 BGM 재생 가능
                 if (playAudio)
                {
                    // Sunny 진입 시 효과음 정리
                    if (rainClip != null) SoundManager.Instance.StopLoop("Rain", transitionDuration);
                    if (windClip != null) SoundManager.Instance.StopLoop("Wind", transitionDuration);
                }

                SetWaterAudio(false); // 물 소리 끄기
                
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

                // 사운드 전환: Rain BGM + Rain Loop
                if (playAudio)
                {
                    if (rainBGM != null) SoundManager.Instance.PlayBGM(rainBGM, 1f, true, transitionDuration);

                    if (windClip != null) SoundManager.Instance.StopLoop("Wind", transitionDuration);
                    if (rainClip != null) SoundManager.Instance.PlayLoop(rainClip, "Rain", transitionDuration, true);
                }

                SetWaterAudio(true); // 물 소리 켜기

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

            case WeatherState.RainStop: // 3페이즈: 비 그침
                targetLightIntensity = rainStopLightIntensity;
                targetFogColor = rainStopFogColor;
                targetFogDensity = rainStopFogDensity;

                if (rainParticleSystem != null) rainParticleSystem.Stop();

                 if (playAudio)
                {
                    if (rainStopBGM != null) SoundManager.Instance.PlayBGM(rainStopBGM, 1f, true, transitionDuration);
                    
                    // 비 루프 소리 서서히 끄기
                    if (rainClip != null) SoundManager.Instance.StopLoop("Rain", transitionDuration);
                    if (windClip != null) SoundManager.Instance.StopLoop("Wind", transitionDuration);
                }

                SetWaterAudio(false); // 물 소리 끄기

                if (globalWind != null) globalWind.strength = sunnyWindStrength; // 바람은 다시 약하게

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate)
                    {
                        if (prop != null) prop.SetActive(false); // 비 관련 오브젝트 끄기
                    }
                }

                // 물/터레인은 그대로 유지 (비가 그쳤다고 바로 물이 빠지는건 아닐 수 있음, 필요시 복구 로직 추가)
                break;

            case WeatherState.Windy: // 4페이즈: 바람
                targetLightIntensity = windyLightIntensity;
                targetFogColor = windyFogColor;
                targetFogDensity = windyFogDensity;

                if (rainParticleSystem != null) rainParticleSystem.Stop();

                if (playAudio)
                {
                    if (windBGM != null) SoundManager.Instance.PlayBGM(windBGM, 1f, true, transitionDuration);

                    // 바람 루프 소리 켜기
                    if (rainClip != null) SoundManager.Instance.StopLoop("Rain", transitionDuration);
                    if (windClip != null) SoundManager.Instance.PlayLoop(windClip, "Wind", transitionDuration, true);
                }

                SetWaterAudio(false); // 물 소리 끄기

                if (globalWind != null) globalWind.strength = windyWindStrength; // 강한 바람

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate)
                    {
                        if (prop != null) prop.SetActive(false);
                    }
                }
                break;
        }

        // 이동 시작 전 현재 위치 저장 및 타이머 초기화 (터레인/물 이동 필요 시 동작)
        if (state == WeatherState.Rain || state == WeatherState.Sunny) // RainStop/Windy에서는 위치 이동이 없다면 조건문 조정 필요
        {
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
        }
       

        if (immediate)
        {
            if (mainLight != null) mainLight.intensity = targetLightIntensity;
            RenderSettings.fogColor = targetFogColor;
            RenderSettings.fogDensity = targetFogDensity;
            
            StopAllCoroutines();
            if (playAudio && (rainClip != null || windClip != null))
            {
                if (state == WeatherState.Rain)
                {
                    // Rain 즉시 재생, Wind 정지
                    if (rainClip != null) SoundManager.Instance.PlayLoop(rainClip, "Rain", 1f, true);
                    SoundManager.Instance.StopLoop("Wind", 0f);
                    SetWaterAudio(true);
                }
                else 
                {
                    // Wind 즉시 재생, Rain 정지
                    if (windClip != null) SoundManager.Instance.PlayLoop(windClip, "Wind", 1f, true);
                    SoundManager.Instance.StopLoop("Rain", 0f);
                    SetWaterAudio(false);
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

    private void SetWaterAudio(bool enable)
    {
        if (waterObjectsToRaise == null) return;

        foreach (var t in waterObjectsToRaise)
        {
            if (t == null) continue;

            AudioSource source = t.GetComponent<AudioSource>();
            if (enable)
            {
                if (source == null) source = t.gameObject.AddComponent<AudioSource>();
                
                if (!source.isPlaying || source.clip != waterRainClip)
                {
                    source.clip = waterRainClip;
                    source.loop = true;
                    source.spatialBlend = 1.0f; // 3D Sound
                    source.minDistance = 1.0f;
                    source.maxDistance = waterRainMaxDistance;
                    source.rolloffMode = AudioRolloffMode.Logarithmic;
                    source.volume = waterRainVolume;
                    source.Play();
                }
            }
            else
            {
                if (source != null && source.isPlaying && source.clip == waterRainClip)
                {
                    source.Stop();
                }
            }
        }
    }

    [ContextMenu("Next Weather")]
    public void NextWeather()
    {
        int next = (int)currentState + 1;
        if (next > (int)WeatherState.Windy) next = 0;
        SetWeather((WeatherState)next);
    }
}