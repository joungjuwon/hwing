using UnityEngine;

/// <summary>
/// 씬이 시작될 때 자동으로 지정된 사운드를 재생하는 스크립트입니다.
/// 타이틀 화면, 게임 스테이지 등에서 배경음악이나 환경음을 재생할 때 사용하세요.
/// </summary>
public class SceneSoundStarter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("재생할 사운드 데이터")]
    public SoundData soundData;

    [Tooltip("체크하면 BGM으로 재생(반복), 해제하면 SFX로 재생(1회성)")]
    public bool playAsBGM = true;

    [Tooltip("시작 시 딜레이 (초)")]
    public float delay = 0f;

    private void Start()
    {
        if (delay > 0f)
        {
            Invoke(nameof(PlaySound), delay);
        }
        else
        {
            PlaySound();
        }
    }

    private void PlaySound()
    {
        if (soundData == null) return;

        if (playAsBGM)
        {
            SoundManager.Instance.PlayBGM(soundData);
        }
        else
        {
            SoundManager.Instance.PlaySFX(soundData);
        }
    }
}
