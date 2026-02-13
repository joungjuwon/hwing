using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Enum Removed as requested. Using int 1, 2, 3, 4.
// Phase 1: Sunny
// Phase 2: Rain
// Phase 3: RainStop
// Phase 4: Windy

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Current Phase")]
    [Tooltip("현재 날씨 페이즈 (1:Sunny, 2:Rain, 3:RainStop, 4:Windy)")]
    [Range(1, 4)]
    public int currentPhase = 1;

    [Header("References")]
    public Light mainLight;
    public ParticleSystem rainParticleSystem;
    public WindArea globalWind;

    [Header("Phase 1 Settings (Sunny)")]
    public float phase1LightIntensity = 1.5f;
    public Color phase1FogColor = new Color(0.6f, 0.7f, 0.8f);
    public float phase1FogDensity = 0.005f;
    public float phase1WindStrength = 5f;
    public AudioClip phase1BGM;
    public AudioClip phase1Ambient;

    [Header("Phase 2 Settings (Rain)")]
    public float phase2LightIntensity = 0.5f;
    public Color phase2FogColor = new Color(0.3f, 0.35f, 0.4f);
    public float phase2FogDensity = 0.02f;
    public float phase2WindStrength = 20f;
    public AudioClip phase2BGM;
    public AudioClip phase2Ambient; // Rain Loop
    [Tooltip("Phase 2(Rain)에서 재생할 물 전용 오디오 클립")]
    public AudioClip phase2WaterClip;
    [Range(0f, 1f)] public float waterRainVolume = 1.0f;
    public float waterRainMaxDistance = 20.0f;
    
    [Tooltip("비가 올 때 활성화할 오브젝트들")]
    public GameObject[] rainPropsToActivate;
    [Tooltip("비가 올 때 Y축을 내릴 터레인")]
    public Terrain rainTargetTerrain;
    public float terrainLowerAmount = 5f;
    [Tooltip("비가 올 때 Y축을 올릴 물 오브젝트들")]
    public Transform[] waterObjectsToRaise;
    public float waterRaiseAmount = 5f;
    public float terrainTransitionDuration = 2.0f;
    public float waterTransitionDuration = 2.0f;

    [Header("Phase 3 Settings (RainStop)")]
    public float phase3LightIntensity = 1.0f;
    public Color phase3FogColor = new Color(0.5f, 0.6f, 0.7f);
    public float phase3FogDensity = 0.01f;
    public float phase3WindStrength = 5f; // 보통 서니와 비슷하거나 약한 바람
    public AudioClip phase3BGM;
    public AudioClip phase3Ambient;

    [Header("Phase 4 Settings (Windy)")]
    public float phase4LightIntensity = 1.2f;
    public Color phase4FogColor = new Color(0.6f, 0.65f, 0.7f);
    public float phase4FogDensity = 0.008f;
    public float phase4WindStrength = 30f;
    public AudioClip phase4BGM;
    public AudioClip phase4Ambient; // Wind Loop

    [Header("Transition Settings")]
    public float transitionDuration = 2.0f;

    // Internal State
    private float targetLightIntensity;
    private Color targetFogColor;
    private float targetFogDensity;

    private Vector3 originalTerrainPosition;
    private Vector3 startTerrainPosition;
    private Vector3 targetTerrainPosition;
    private float currentTerrainTime;
    private bool terrainPositionBackedUp = false;

    private List<Vector3> originalWaterPositions = new List<Vector3>();
    private List<Vector3> startWaterPositions = new List<Vector3>();
    private List<Vector3> targetWaterPositions = new List<Vector3>();
    private float currentWaterTime;
    private bool waterPositionsBackedUp = false;

    // Backup for original water audio clips (to restore in Phase 3)
    // Key: InstanceID of GameObject, Value: Original AudioClip
    private Dictionary<int, AudioClip> originalWaterClips = new Dictionary<int, AudioClip>();
    private bool waterAudioBackedUp = false;

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
        RenderSettings.fog = true;
        
        // 시작 시 Audio Backup 수행 (Water Objects)
        BackupWaterAudio();

        // 초기 상태 적용 (즉시, 오디오 재생 안 함 - 타이틀 BGM 유지 또는 씬 초기 설정 따름)
        ApplyPhase(currentPhase, true, false);
    }

    private void BackupWaterAudio()
    {
        if (waterObjectsToRaise == null) return;
        
        originalWaterClips.Clear();
        foreach (var t in waterObjectsToRaise)
        {
            if (t == null) continue;
            AudioSource source = t.GetComponent<AudioSource>();
            if (source != null)
            {
                // 원본 클립 저장 (null일 수도 있음)
                originalWaterClips[t.gameObject.GetInstanceID()] = source.clip;
            }
            else
            {
                // 오디오 소스가 없었다면 null로 기록
                originalWaterClips[t.gameObject.GetInstanceID()] = null;
            }
        }
        waterAudioBackedUp = true;
    }

    private void Update()
    {
        // Smooth Transition for Light & Fog
        if (mainLight != null)
        {
            mainLight.intensity = Mathf.Lerp(mainLight.intensity, targetLightIntensity, Time.deltaTime / transitionDuration);
        }

        if (RenderSettings.fog)
        {
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, Time.deltaTime / transitionDuration);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, Time.deltaTime / transitionDuration);
        }

        // Smooth Transition for Terrain
        if (terrainPositionBackedUp && rainTargetTerrain != null && currentTerrainTime < terrainTransitionDuration)
        {
            currentTerrainTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentTerrainTime / terrainTransitionDuration);
            rainTargetTerrain.transform.position = Vector3.Lerp(startTerrainPosition, targetTerrainPosition, t);
        }

        // Smooth Transition for Water
        if (waterPositionsBackedUp && waterObjectsToRaise != null && currentWaterTime < waterTransitionDuration)
        {
            currentWaterTime += Time.deltaTime;
            float t = Mathf.Clamp01(currentWaterTime / waterTransitionDuration);

            for (int i = 0; i < waterObjectsToRaise.Length; i++)
            {
                if (waterObjectsToRaise[i] != null && i < startWaterPositions.Count && i < targetWaterPositions.Count)
                {
                    waterObjectsToRaise[i].position = Vector3.Lerp(startWaterPositions[i], targetWaterPositions[i], t);
                }
            }
        }
    }

    public void SetPhase(int newPhase)
    {
        if (currentPhase == newPhase) return;
        
        // 1~4 범위 제한
        if (newPhase < 1) newPhase = 1;
        if (newPhase > 4) newPhase = 4;

        currentPhase = newPhase;
        ApplyPhase(currentPhase, false, true);
    }

    private void ApplyPhase(int phase, bool immediate, bool playAudio = true)
    {
        Debug.Log($"[WeatherManager] Changing Phase to {phase}");

        AudioClip targetBGM = null;
        AudioClip targetAmbient = null;
        string ambientID = "WeatherAmbient";

        switch (phase)
        {
            case 1: // Sunny
                targetLightIntensity = phase1LightIntensity;
                targetFogColor = phase1FogColor;
                targetFogDensity = phase1FogDensity;
                if (globalWind != null) globalWind.strength = phase1WindStrength;

                if (rainParticleSystem != null) rainParticleSystem.Stop();
                
                SetWaterAudioToOriginal(); // 물 소리 원상복구

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate) if (prop != null) prop.SetActive(false);
                }

                // Restore/Reset positions Logic
                // (이전 로직과 동일하게 유지: Rain에서 벗어나면 Water만 백업에서 복구)
                if (waterPositionsBackedUp && waterObjectsToRaise != null)
                {
                    targetWaterPositions = new List<Vector3>(originalWaterPositions);
                }

                targetBGM = phase1BGM;
                targetAmbient = phase1Ambient;
                break;

            case 2: // Rain
                targetLightIntensity = phase2LightIntensity;
                targetFogColor = phase2FogColor;
                targetFogDensity = phase2FogDensity;
                if (globalWind != null) globalWind.strength = phase2WindStrength;

                if (rainParticleSystem != null) rainParticleSystem.Play();

                SetWaterAudioToRain(); // 2페이즈 전용 물 소리

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate) if (prop != null) prop.SetActive(true);
                }

                // Move Logic
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
                        foreach (var water in waterObjectsToRaise) if (water != null) originalWaterPositions.Add(water.position);
                        waterPositionsBackedUp = true;
                    }
                    targetWaterPositions.Clear();
                    foreach (var originalPos in originalWaterPositions)
                    {
                        targetWaterPositions.Add(originalPos + new Vector3(0, waterRaiseAmount, 0));
                    }
                }

                targetBGM = phase2BGM;
                targetAmbient = phase2Ambient;
                break;

            case 3: // RainStop
                targetLightIntensity = phase3LightIntensity;
                targetFogColor = phase3FogColor;
                targetFogDensity = phase3FogDensity;
                if (globalWind != null) globalWind.strength = phase3WindStrength;

                if (rainParticleSystem != null)
                {
                    rainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                SetWaterAudioToOriginal(); // 물 소리 원상복구

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate) if (prop != null) prop.SetActive(false);
                }

                // Restore/Reset positions Logic (All rain off)
                if (waterPositionsBackedUp && waterObjectsToRaise != null)
                {
                    targetWaterPositions = new List<Vector3>(originalWaterPositions);
                }
                
                if (terrainPositionBackedUp && rainTargetTerrain != null)
                {
                    // targetTerrainPosition = originalTerrainPosition; // User requested to NOT restore terrain in Phase 3
                }

                targetBGM = phase3BGM;
                targetAmbient = phase3Ambient;
                break;

            case 4: // Windy
                targetLightIntensity = phase4LightIntensity;
                targetFogColor = phase4FogColor;
                targetFogDensity = phase4FogDensity;
                if (globalWind != null) globalWind.strength = phase4WindStrength;

                if (rainParticleSystem != null) rainParticleSystem.Stop();

                SetWaterAudioToOriginal(); // 물 소리 원상복구

                if (rainPropsToActivate != null)
                {
                    foreach (var prop in rainPropsToActivate) if (prop != null) prop.SetActive(false);
                }
                
                targetBGM = phase4BGM;
                targetAmbient = phase4Ambient;
                break;
        }

        // 이동 애니메이션 초기화 (위치 변경이 필요한 Phase 1, 2인 경우 등)
        // (간단화를 위해 항상 초기화 시도, 변화 없으면 Lerp가 제자리 유지)
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


        // Immediate Application
        if (immediate)
        {
            if (mainLight != null) mainLight.intensity = targetLightIntensity;
            RenderSettings.fogColor = targetFogColor;
            RenderSettings.fogDensity = targetFogDensity;
            
            // Audio Immediate
             if (playAudio)
            {
                if (targetBGM != null) SoundManager.Instance.PlayBGM(targetBGM);
                // else: Do not stop BGM if target is null (continue playing previous)

                if (targetAmbient != null) SoundManager.Instance.PlayLoop(targetAmbient, ambientID);
                else SoundManager.Instance.StopLoop(ambientID);
            }

            // Transform Immediate
            if (terrainPositionBackedUp && rainTargetTerrain != null)
            {
                rainTargetTerrain.transform.position = targetTerrainPosition;
                currentTerrainTime = terrainTransitionDuration;
            }
            if (waterPositionsBackedUp && waterObjectsToRaise != null)
            {
                for (int i = 0; i < waterObjectsToRaise.Length; i++)
                {
                    if (waterObjectsToRaise[i] != null && i < targetWaterPositions.Count)
                    {
                        waterObjectsToRaise[i].position = targetWaterPositions[i];
                    }
                }
                currentWaterTime = waterTransitionDuration;
            }
        }
        else if (playAudio)
        {
            // Smooth Audio Transition
            // useCrossFade: false (FadeOut -> FadeIn) as requested for Phase Transition
            if (targetBGM != null) SoundManager.Instance.PlayBGM(targetBGM, 1f, true, transitionDuration, false);
            // Ambient는 CrossFade가 SoundManager에 있으면 좋지만, 여기서는 Stop -> Play (with fade support from SoundManager?)
            // SoundManager.StopLoop가 fade 지원하면 좋음. 지원함.

            // 기존 앰비언트와 새로운 앰비언트가 다르면 교체
            // (SoundManager가 같은 ID라도 클립 다르면 교체 처리함)
            if (targetAmbient != null)
            {
                SoundManager.Instance.PlayLoop(targetAmbient, ambientID, transitionDuration);
            }
            else
            {
                SoundManager.Instance.StopLoop(ambientID, transitionDuration);
            }
        }
    }

    private void SetWaterAudioToRain()
    {
        if (waterObjectsToRaise == null || phase2WaterClip == null) return;

        foreach (var t in waterObjectsToRaise)
        {
            if (t == null) continue;
            AudioSource source = t.GetComponent<AudioSource>();
            if (source == null) source = t.gameObject.AddComponent<AudioSource>();

            // Rain Water Sound Play
            if (source.clip != phase2WaterClip || !source.isPlaying)
            {
                source.clip = phase2WaterClip;
                source.loop = true;
                source.spatialBlend = 1.0f;
                source.minDistance = 1.0f;
                source.maxDistance = waterRainMaxDistance;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.volume = waterRainVolume;
                source.Play();
            }
        }
    }

    private void SetWaterAudioToOriginal()
    {
        if (waterObjectsToRaise == null || !waterAudioBackedUp) return;

        foreach (var t in waterObjectsToRaise)
        {
            if (t == null) continue;
            int id = t.gameObject.GetInstanceID();
            
            if (originalWaterClips.ContainsKey(id))
            {
                AudioClip originalClip = originalWaterClips[id];
                AudioSource source = t.GetComponent<AudioSource>();
                
                if (originalClip == null)
                {
                    // 원래 오디오가 없던 녀석 -> 소리 끄기
                    if (source != null) source.Stop();
                }
                else
                {
                    // 원래 오디오가 있던 녀석 -> 원래 클립 복구 및 재생 (원래 loop였는지 등은 저장 안했으나 보통 loop 환경음일 것)
                    if (source == null) source = t.gameObject.AddComponent<AudioSource>();

                    if (source.clip != originalClip)
                    {
                        source.clip = originalClip;
                        source.Play(); // 설정에 따라 자동 재생이 아닐 수도 있으니 주의 (여기선 일단 재생)
                    }
                }
            }
        }
    }

    /// <summary>
    /// 타이틀 화면 진입 시 날씨와 지형/물 위치를 초기화합니다.
    /// (Phase 3 설정과 무관하게 강제로 원복)
    /// </summary>
    public void ResetForTitle()
    {
        // 1. 지형 및 물 위치 강제 복구 (Lerp 없이 즉시 적용)
        if (terrainPositionBackedUp && rainTargetTerrain != null)
        {
            // 목표 위치도 원복하여 Update에서 다시 움직이지 않도록 함
            targetTerrainPosition = originalTerrainPosition;
            startTerrainPosition = originalTerrainPosition; // 시작점도 원복
            rainTargetTerrain.transform.position = originalTerrainPosition;
            
            // Lerp 로직이 더 이상 돌지 않도록 Time을 Max로 설정
            currentTerrainTime = terrainTransitionDuration + 1f; 
        }

        if (waterPositionsBackedUp && waterObjectsToRaise != null)
        {
            // 리스트 크기 맞춤 (Start/Target)
            startWaterPositions = new List<Vector3>(originalWaterPositions);
            targetWaterPositions = new List<Vector3>(originalWaterPositions);

            for (int i = 0; i < waterObjectsToRaise.Length; i++)
            {
                if (waterObjectsToRaise[i] != null && i < originalWaterPositions.Count)
                {
                    waterObjectsToRaise[i].transform.position = originalWaterPositions[i];
                }
            }
            
            // Lerp 중지
            currentWaterTime = waterTransitionDuration + 1f;
        }

        // 2. 비 효과 정지
        if (rainParticleSystem != null)
        {
            rainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (rainPropsToActivate != null)
        {
            foreach (var prop in rainPropsToActivate) if (prop != null) prop.SetActive(false);
        }

        // 3. 오디오 복구
        SetWaterAudioToOriginal();

        // 4. 날씨 상태 초기화 (Phase 1로 간주하거나, 리셋)
        // 타이틀에서는 보통 맑은 상태나 별도 연출을 따르므로 여기선 효과만 끕니다.
        
        Debug.Log("[WeatherManager] Reset for Title (Positions Force Restored, Rain Stopped).");
    }

    [ContextMenu("Next Phase")]
    public void NextPhase()
    {
        int next = currentPhase + 1;
        if (next > 4) next = 1;
        SetPhase(next);
    }
}