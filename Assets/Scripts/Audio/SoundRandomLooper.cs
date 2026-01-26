using System.Collections;
using UnityEngine;

/// <summary>
/// SoundData의 랜덤 기능(여러 Clip 중 랜덤 선택)을 활용하여
/// 끊김 없이(또는 일정 간격으로) 계속해서 다른 소리를 재생하는 스크립트입니다.
/// 예: 바람 소리, 빗소리, 환경음 등
/// </summary>
public class SoundRandomLooper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("재생할 사운드 데이터 (Clips에 여러 개 등록 권장)")]
    public SoundData soundData;

    [Tooltip("재생 간 추가 딜레이 (초) \n0이면 앞 소리가 끝나자마자 바로 다음 소리 재생")]
    public float extraDelay = 0f;

    [Tooltip("활성화 시 자동 재생 여부")]
    public bool playOnEnable = true;

    private Coroutine loopCoroutine;

    /// <summary>
    /// SoundManager에 의해 동적으로 생성될 때 초기화하는 메서드
    /// </summary>
    public void Init(SoundData data)
    {
        this.soundData = data;
        this.extraDelay = data.loopDelay;
        // playOnEnable = true; // 필요 시 설정
        PlayLoop();
    }

    private void OnEnable()
    {
        if (playOnEnable && soundData != null)
        {
            PlayLoop();
        }
    }

    private void OnDisable()
    {
        StopLoop();
    }

    /// <summary>
    /// 루프 재생을 시작합니다.
    /// </summary>
    public void PlayLoop()
    {
        StopLoop(); // 이미 돌고 있으면 재시작
        loopCoroutine = StartCoroutine(LoopRoutine());
    }

    /// <summary>
    /// 루프를 정지합니다.
    /// </summary>
    public void StopLoop()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }

    private IEnumerator LoopRoutine()
    {
        // 무한 루프
        while (true)
        {
            if (soundData == null)
            {
                Debug.LogWarning("SoundRandomLooper: SoundData is missing!");
                yield break;
            }

            // SoundManager를 통해 랜덤 재생하고, 선택된 클립 정보를 받아옴
            // 중요: forceOneShot=true로 호출하여 다시 루프가 실행되는 것을 방지
            AudioClip playedClip = SoundManager.Instance.PlaySFX(soundData, true);

            // 재생된 클립이 있다면 그 길이만큼 대기
            if (playedClip != null)
            {
                // 클립 길이만큼 대기 (피치 변화를 고려하면 좀 더 복잡하지만, 여기선 기본 길이 대기)
                // 만약 피치 변화가 크다면 (playedClip.length / pitch) 로 계산해야 함.
                // 현재 SoundData 구조상 피치는 랜덤하게 변할 수 있으나, 
                // 외부에서 최종 피치를 알기 어려우므로(SoundManager가 수정되지 않는 한)
                // 단순하게 클립 길이만큼 대기합니다. (보통 환경음은 겹쳐도 무방하므로)
                yield return new WaitForSeconds(playedClip.length);
            }
            else
            {
                // 재생 실패 시 잠시 대기 후 재시도 (무한 루프 방지)
                yield return new WaitForSeconds(1f);
            }

            // 추가 딜레이가 있다면 대기
            if (extraDelay > 0f)
            {
                yield return new WaitForSeconds(extraDelay);
            }
        }
    }
}
