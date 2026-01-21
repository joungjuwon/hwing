using UnityEngine;

/// <summary>
/// 사운드 데이터 에셋 (ScriptableObject)
/// 오디오 클립과 볼륨, 피치 등의 설정을 하나의 파일로 관리합니다.
/// </summary>
[CreateAssetMenu(fileName = "NewSoundData", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Header("Basic Settings")]
    [Tooltip("사운드 식별 이름 (코드에서 구분용)")]
    public string soundName;
    
    [Tooltip("재생할 오디오 클립")]
    public AudioClip clip;

    [Header("Playback Settings")]
    [Range(0f, 1f)]
    [Tooltip("볼륨 (0.0 ~ 1.0)")]
    public float volume = 1f;

    [Range(0.1f, 3f)]
    [Tooltip("피치 (재생 속도/단조)")]
    public float pitch = 1f;

    [Tooltip("반복 재생 여부 (BGM 등)")]
    public bool loop = false;
}
