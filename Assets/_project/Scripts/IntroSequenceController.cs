using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using Unity.Cinemachine; // Cinemachine 3.x namespace

/// <summary>
/// 게임 시작 전 연출(인트로)을 관리하는 컨트롤러입니다.
/// Cinemachine을 활용하여 씨앗을 따라가고, 이후 플레이어에게 카메라를 넘겨줍니다.
/// </summary>
public class IntroSequenceController : MonoBehaviour
{
    [Header("Cinemachine")]
    [Tooltip("인트로 연출에 사용할 시네머신 카메라")]
    public CinemachineCamera introCam; 

    [Header("Targets")]
    [Tooltip("연출용으로 사용할 씨앗 프리팹 리스트 (생애 주기에 따라 선택)")]
    public GameObject[] seedPrefabs;
    
    [Tooltip("현재 선택된 씨앗 인덱스 (외부에서 설정 가능)")]
    public int selectedSeedIndex = 0;

    [Tooltip("실제 조작할 플레이어 캐릭터 (처음엔 비활성화)")]
    public GameObject playerCharacter;

    [Header("Seed Animation Settings")]
    [Tooltip("낙하 시작 높이 (목표 지점 기준 상대 높이)")]
    public float dropHeight = 50f;
    [Tooltip("씨앗이 떨어지기 시작할 위치")]
    public Transform startPoint;
    [Tooltip("씨앗이 도달할 목표 위치(플레이어 스폰 위치)")]
    public Transform endPoint;
    [Tooltip("낙하 시간 (초)")]
    public float fallDuration = 3.0f;
    [Tooltip("낙하 곡선 (Ease In Out 등)")]
    public AnimationCurve fallCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("Game Control")]
    [Tooltip("인트로 재생 중 비활성화할 스크립트들 (예: 플레이어 이동, 카메라 컨트롤러)")]
    public MonoBehaviour[] scriptsToDisable;

    [Header("Events")]
    [Tooltip("연출이 끝난 후 실행될 이벤트 (게임 시작 등)")]
    public UnityEvent onIntroFinish;

    [Header("Cinematic Settings")]
    [Tooltip("낙하 시작 시 카메라 FOV (넓게 보여줌)")]
    public float startFOV = 60f;
    [Tooltip("낙하 종료 시 카메라 FOV (플레이어 시점과 비슷하게)")]
    public float endFOV = 30f;
    [Tooltip("시작 시 카메라 거리 (Intro Cam에 ThirdPersonFollow 필요)")]
    public float startDistance = 7f;
    [Tooltip("종료 시 카메라 거리 (가까워짐)")]
    public float endDistance = 2f;
    [Tooltip("카메라 회전 속도")]
    public float rotationSpeed = 100f;

    [Header("Debug")]
    [Tooltip("게임 시작 시 자동으로 테스트 연출을 재생할지 여부")]
    public bool playOnStart = false;

    [Header("Audio")]
    [Tooltip("낙하 시작 시 재생할 사운드 데이터 (ScriptableObject)")]
    public SoundData dropSoundData;

    // 내부 변수
    private GameObject currentSeedInstance;
    private System.Action callbackOnFinish; 
    private Coroutine currentIntroRoutine; // 현재 실행 중인 연출 코루틴 (중복 방지용) 

    private void Start()
    {
        // 자동 시작 방지 (테스트 모드일 땐 예외)
        if (playOnStart)
        {
            // 테스트를 위해 잠시 뒤 실행 (초기화 대기)
            Invoke(nameof(TestPlay), 0.5f);
        }
        else
        {
            // 이미 외부(Manager)에서 인트로를 실행 중일 수 있으므로 체크
            // (SimulationManager가 먼저 Start되어 PlayIntro를 호출했을 경우를 대비)
            if (currentIntroRoutine == null)
            {
                if (playerCharacter != null) playerCharacter.SetActive(false);
                if (introCam != null) introCam.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 에디터에서 우클릭으로 테스트 실행 가능
    /// </summary>
    [ContextMenu("Test Play Intro")]
    public void TestPlay()
    {
        if (playerCharacter == null)
        {
            Debug.LogError("[Intro] TestPlay requires 'Player Character' to be assigned in Inspector!");
            return;
        }

        Vector3 targetPos = Vector3.zero;
        if (endPoint != null) 
        {
            targetPos = endPoint.position;
        }

        Debug.Log($"[Intro] Testing Sequence with {playerCharacter.name}...");
        introCam.gameObject.SetActive(true); // Ensure cam is on for test
        
        PlayIntro(playerCharacter, targetPos, () => {
            Debug.Log("[Intro] Test Finished!");
        });
    }
    

    
    /// <summary>
    /// 외부(SimulationManager)에서 인트로 연출을 시작할 때 호출
    /// </summary>
    /// <param name="landingPosition">씨앗이 떨어질 목표 지점</param>
    /// <param name="onComplete">연출 종료 후 실행할 로직 (플레이어 스폰 등)</param>
    /// <summary>
    /// 외부(SimulationManager)에서 인트로 연출을 시작할 때 호출
    /// </summary>
    /// <param name="playerSubject">연출 대상이 될 플레이어 오브젝트</param>
    /// <param name="landingPosition">씨앗이 떨어질 목표 지점</param>
    /// <param name="onComplete">연출 종료 후 실행할 로직 (플레이어 조작 활성화 등)</param>
    public void PlayIntro(GameObject playerSubject, Vector3 landingPosition, System.Action onComplete)
    {
        // 콜백 저장
        callbackOnFinish = onComplete;
        currentSeedInstance = playerSubject; // 타겟 설정

        // 도착점 설정 (Transform 대신 Vector3 좌표 사용)
        if (endPoint != null) endPoint.position = landingPosition;
        
        // 떨어지는 시작점 계산 (목표지점 바로 위 하늘)
        Vector3 startPos = landingPosition + Vector3.up * dropHeight; 
        if (startPoint != null) startPoint.position = startPos;

        // 플레이어 위치를 시작점으로 이동
        if (currentSeedInstance != null)
        {
            currentSeedInstance.transform.position = startPos;
            currentSeedInstance.transform.rotation = Quaternion.identity;
        }

        // 시네머신 카메라 활성화 및 타겟 설정
        if (introCam != null)
        {
             introCam.gameObject.SetActive(true);
             introCam.Priority = 100; // 우선순위를 높여서 강제로 화면 전환
             if (currentSeedInstance != null)
             {
                 introCam.Follow = currentSeedInstance.transform;
             }
        }

        // 이전 코루틴이 돌고 있다면 정지 (중복 실행 방지)
        if (currentIntroRoutine != null) StopCoroutine(currentIntroRoutine);

        // 코루틴 시작
        currentIntroRoutine = StartCoroutine(PlayIntroSequence(startPos, landingPosition));
    }

    private IEnumerator PlayIntroSequence(Vector3 startPos, Vector3 finalPos)
    {
        // 사운드 재생 (SoundManager 사용)
        if (dropSoundData != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(dropSoundData);
        }

        // 게임플레이 스크립트 비활성화
        if (scriptsToDisable != null)
        {
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = false;
            }
        }

        // 노이즈 컴포넌트 가져오기 (카메라 흔들림용)
        CinemachineBasicMultiChannelPerlin noise = null;
        if (introCam != null)
        {
            noise = introCam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        float timer = 0f;

            // 3rd Person Follow 컴포넌트 가져오기 (거리 조절용)
            // Unity 6: CinemachineThirdPersonFollow
            CinemachineThirdPersonFollow thirdPerson = null;
            if (introCam != null)
            {
               thirdPerson = introCam.GetComponent<CinemachineThirdPersonFollow>();
            }

            // 1. 낙하 연출
            while (timer < fallDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / fallDuration);
                float heightParam = fallCurve.Evaluate(progress);

                // 씨앗 이동 및 회전
                if (currentSeedInstance != null)
                {
                    currentSeedInstance.transform.position = Vector3.Lerp(startPos, finalPos, heightParam);
                    currentSeedInstance.transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
                }

                // 카메라 연출 (FOV 변경, 거리 조절, 흔들림)
                if (introCam != null)
                {
                    // FOV
                    var lens = introCam.Lens;
                    lens.FieldOfView = Mathf.Lerp(startFOV, endFOV, heightParam); 
                    introCam.Lens = lens;

                    // Distance (거리가 멀었다가 가까워짐)
                    if (thirdPerson != null)
                    {
                        thirdPerson.CameraDistance = Mathf.Lerp(startDistance, endDistance, heightParam);
                    }

                    // Noise (낙하 속도가 빨라질수록 흔들림 증가)
                    if (noise != null)
                    {
                        // 예: 처음엔 0이었다가 중간~끝부분에서 흔들림 최대
                        noise.AmplitudeGain = Mathf.Lerp(0f, 2.0f, heightParam); 
                    }
                }

                yield return null;
            }

        // 연출 종료: 흔들림 제거
        if (noise != null) noise.AmplitudeGain = 0f;

        // 2. 연출 종료 및 정리
        // 낙하가 끝난 직후 잠시 대기 (착지감 부여 및 플레이어 스폰 전 텀)
        yield return new WaitForSeconds(0.5f);



        Debug.Log("[Intro] Sequence Finished. Calling Callback.");
        
        // **중요: 외부 콜백 실행 (여기에 실제 플레이어 생성/활성화 로직이 들어옴)**
        callbackOnFinish?.Invoke();
        
        // 기존 UnityEvent 실행 (호환성 유지)
        onIntroFinish?.Invoke();

        // 3. 게임플레이 스크립트 복구
        if (scriptsToDisable != null)
        {
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = true;
            }
        }
        
        // 인트로 카메라 끄기 -> 메인 카메라로 전환
        if (introCam != null) 
        {
            introCam.Priority = 0; // 우선순위 초기화
            introCam.gameObject.SetActive(false);
        }
    }
}
