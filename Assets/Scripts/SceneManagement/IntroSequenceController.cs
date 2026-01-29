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
    [Tooltip("연출용으로 사용할 가짜 씨앗 오브젝트")]
    public Transform introSeed;
    [Tooltip("실제 조작할 플레이어 캐릭터 (처음엔 비활성화)")]
    public GameObject playerCharacter;

    [Header("Seed Animation Settings")]
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

    private void Start()
    {
        // 시작 시 초기화
        if (playerCharacter != null) playerCharacter.SetActive(false);
        if (introSeed != null) 
        {
            introSeed.gameObject.SetActive(true);
            if (startPoint != null) introSeed.position = startPoint.position;
            
            // 시네머신 타겟 설정 (씨앗)
            if (introCam != null)
            {
                 introCam.Follow = introSeed;
                 introCam.LookAt = null; // 필요하면 설정
            }
        }

        // 인트로 시작
        StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        // 게임플레이 스크립트 비활성화
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        float timer = 0f;
        Vector3 startPos = startPoint != null ? startPoint.position : introSeed.position;
        Vector3 finalPos = endPoint != null ? endPoint.position : Vector3.zero;

        // 1. 낙하 연출
        while (timer < fallDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fallDuration);
            float heightParam = fallCurve.Evaluate(progress);

            if (introSeed != null)
            {
                // 선형 보간 + 곡선
                introSeed.position = Vector3.Lerp(startPos, finalPos, heightParam);
                
                // 회전 연출 (선택)
                introSeed.Rotate(Vector3.up * 100 * Time.deltaTime);
            }
            yield return null;
        }

        // 2. 전환 연출 (씨앗 -> 플레이어)
        if (introSeed != null) introSeed.gameObject.SetActive(false);

        if (playerCharacter != null)
        {
            // 플레이어를 목표 위치에 배치하고 활성화
            playerCharacter.transform.position = finalPos; 
            playerCharacter.SetActive(true);
            
            // 카메라 타겟 변경 (플레이어)
            if (introCam != null)
            {
                introCam.Follow = playerCharacter.transform;
            }
        }

        Debug.Log("[Intro] Sequence Finished. Game Start.");
        onIntroFinish?.Invoke();
        
        // 게임플레이 스크립트 다시 활성화
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }
        
        // 중요: 연출 카메라의 역할이 끝났다면 꺼주어야, 메인 게임플레이 카메라(우선순위가 낮거나 같은 다른 VCam)가 동작할 수 있음.
        // 만약 이 카메라를 그대로 게임 카메라로 쓴다면 끄지 않아도 됨.
        // 여기서는 "연출용"이라고 가정하고 끔 (Priority 조절 방식이 더 좋지만 간단하게 Active Toggle)
        // if (introCam != null) introCam.gameObject.SetActive(false);
    }
}

