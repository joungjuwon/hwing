using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 타이틀 화면에서 아무 키나 누르면 다음 씬으로 넘어가는 기능을 담당합니다.
/// </summary>
public class TitleInputController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("이동할 다음 씬의 빌드 인덱스")]
    public int nextSceneIndex = 1;

    [Tooltip("입력을 받기 시작할 때까지의 지연 시간 (초)")]
    public float inputDelay = 1.0f;

    private bool canInput = false;

    [Header("Effect Settings")]
    [Tooltip("연결할 타이틀 이펙트 컨트롤러 (없으면 즉시 이동)")]
    public TitleEffectController titleEffectController;

    [Tooltip("이펙트 재생 후 씬 전환 전까지 추가 대기 시간 (초)")]
    public float transitionDelay = 2.0f;

    private void Start()
    {
        // 타이틀 진입 시 (또는 씬 로드 시) 터레인/날씨 초기화: 잔디 제거, 레이어 1번, 위치 원복
        if (TerrainManager.Instance != null) TerrainManager.Instance.ApplyTitleStyle();
        if (WeatherManager.Instance != null) WeatherManager.Instance.ResetForTitle();

        // 씬 시작 후 바로 넘어가면 당황스러우므로 약간의 딜레이를 줌
        Invoke(nameof(EnableInput), inputDelay);
    }

    private void EnableInput()
    {
        canInput = true;
    }

    [Header("Events & Effects")]
    [Tooltip("씬 전환 시작 시 발생할 이벤트 (사운드 재생 등)")]
    public UnityEvent onTransitionStart;
    [Tooltip("클릭 시 재생할 파티클 시스템 (미리 배치 필요)")]
    public ParticleSystem clickFeedbackParticle;

    private void Update()
    {
        if (!canInput) return;

        // New Input System: 키보드나 마우스 입력 감지
        bool inputDetected = false;

        // 1. 키보드 입력 체크
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            inputDetected = true;
        }

        // 2. 마우스 클릭 체크
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
        }

        if (inputDetected)
        {
            // 1. 이벤트 실행
            onTransitionStart?.Invoke();

            // 2. 파티클 재생
            if (clickFeedbackParticle != null)
            {
                clickFeedbackParticle.Play();
            }

            StartCoroutine(TransitionSequence());
        }
    }

    [Header("Seamless Transition")]
    [Tooltip("다음 씬을 로드하지 않고, 즉시 인트로 연출을 시작할지 여부")]
    public bool seamlessMode = true;
    [Tooltip("Seamless 모드일 때 연결할 시뮬레이션 매니저")]
    public SimulationManager simulationManager;
    [Tooltip("Seamless 모드일 때 첫 시작 위치 (비워두면 Random)")]
    public Transform firstStartPoint;

    private System.Collections.IEnumerator TransitionSequence()
    {
        // 중복 입력 방지
        canInput = false;

        if (titleEffectController != null)
        {
            Debug.Log("[TitleInput] Playing Title Effect...");
            titleEffectController.PlayEffect();
            
            // 효과 지속 시간 + 추가 대기 시간(n초) 만큼 대기
            float totalWaitTime = titleEffectController.duration + transitionDelay;
            yield return new WaitForSeconds(totalWaitTime);
        }
        else
        {
             yield return new WaitForSeconds(transitionDelay);
        }

        if (seamlessMode)
        {
            Debug.Log("[TitleInput] Seamless Mode: Switching to Simulation...");
            
            if (simulationManager != null)
            {
                // 시작 위치가 지정되어 있으면 그곳으로, 아니면 랜덤(Vector3.zero 등은 RespawnPlayer 내부 로직에 따라 처리됨)
                Vector3 startPos = (firstStartPoint != null) ? firstStartPoint.position : Vector3.zero;
                
                // 아직 랜덤 위치 로직이 SimulationManager 내부에 있다면 Vector3.zero를 줬을 때 0,0,0에 떨어질 수 있음.
                // SimulationManager가 '랜덤'을 처리해주길 기대하거나, 여기서 랜덤을 계산해야 함.
                // 편의상 SimulationManager의 GetRandomPositionOnMap은 private이므로, 
                // 여기서는 simulationManager가 스스로 처리하게끔 유도하거나, 그냥 호출.
                // SimulationManager.RespawnPlayer는 위치를 매개변수로 받음.
                
                // 만약 firstStartPoint가 없다면 시뮬레이션 매니저에게 "알아서 랜덤 위치에 해줘"라고 할 수 있으면 좋겠지만
                // 현재 RespawnPlayer는 Vector3를 받음. 
                // 임시: 그냥 0,0,0 또는 랜덤값 전달.
                if (firstStartPoint == null) 
                {
                    startPos = new Vector3(Random.Range(-10,10), 50, Random.Range(-10,10)); // 임시 랜덤
                }

                simulationManager.RespawnPlayer(startPos);
            }
            else
            {
                Debug.LogError("Seamless Mode is ON but SimulationManager is NOT assigned!");
            }

            // 타이틀 UI 비활성화 (씬 전환 효과)
            gameObject.SetActive(false);
        }
        else
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        Debug.Log($"[TitleInput] Loading Scene Index: {nextSceneIndex}");
        SceneManager.LoadScene(nextSceneIndex);
    }
}
