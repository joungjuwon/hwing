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

    private int loopId = -1;

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
        if (SoundManager.Instance != null && soundData != null)
        {
            loopId = SoundManager.Instance.RegisterRandomLoop(soundData, extraDelay);
        }
    }

    /// <summary>
    /// 루프를 정지합니다.
    /// </summary>
    public void StopLoop()
    {
        if (loopId != -1 && SoundManager.Instance != null)
        {
            SoundManager.Instance.UnregisterRandomLoop(loopId);
            loopId = -1;
        }
    }

    // LoopRoutine은 SoundManager로 이관됨
}
