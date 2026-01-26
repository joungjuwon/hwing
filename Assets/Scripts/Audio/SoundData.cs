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

    [Tooltip("재생할 오디오 클립들 (다중, 설정 시 clip 무시하고 이 중 랜덤 재생)")]
    public AudioClip[] clips;

    [Header("Playback Settings")]
    [Range(0f, 1f)]
    [Tooltip("볼륨 (0.0 ~ 1.0)")]
    public float volume = 1f;

    [Range(0.1f, 3f)]
    [Tooltip("피치 (재생 속도/단조)")]
    public float pitch = 1f;

    [Tooltip("반복 재생 여부 (BGM 등)")]
    public bool loop = false;

    [Header("Random Settings")]
    [Tooltip("랜덤 효과(볼륨, 피치)를 적용할지 여부")]
    public bool useRandomVariance = false;

    [Tooltip("볼륨 변동 폭 (0.0 ~ 1.0) \n예: 0.1이면 (Volume - 0.05) ~ (Volume + 0.05) 범위에서 랜덤")]
    [Range(0f, 1f)]
    public float volumeVariance = 0.1f;

    [Tooltip("피치 변동 폭 (0.0 ~ 1.0) \n예: 0.2이면 (Pitch - 0.1) ~ (Pitch + 0.1) 범위에서 랜덤")]
    [Range(0f, 1f)]
    public float pitchVariance = 0.2f;

    [Header("Random Loop Settings")]
    [Tooltip("체크 시 SFX 재생 시 자동으로 랜덤 루프 모드로 동작 (별도 오브젝트 생성됨)")]
    public bool useRandomLoop = false;

    [Tooltip("루프 재생 간 대기 시간 (초)")]
    public float loopDelay = 0f;
}
