using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 다양한 조건(Start, Enable, Manual)에서 사운드를 재생, 정지하거나 이벤트를 실행하는 통합 컨트롤러입니다.
/// </summary>
public class SceneSoundStarter : MonoBehaviour
{
    // 트리거 조건 정의
    public enum TriggerCondition
    {
        OnStart,
        OnEnable,
        OnDisable,
        OnDestroy,
        ManualCallOnly // 외부에서 함수 호출로만 실행
    }

    // 수행할 동작 정의
    public enum AudioActionType
    {
        PlayBGM,
        PlaySFX,
        StopBGM,
        StopAllSFX, // (주의: SoundManager 구현에 따라 작동 안 할 수도 있음, 현재는 미구현)
        InvokeEvent // 다른 함수 실행
    }

    [System.Serializable]
    public class AudioCommand
    {
        [Tooltip("이 명령의 이름 (식별용)")]
        public string commandName = "New Command";

        public AudioActionType actionType = AudioActionType.PlayBGM;

        [Header("Settings")]
        [Tooltip("실행 전 대기 시간 (초)")]
        public float delay = 0f;

        [Header("Audio Data")]
        [Tooltip("재생할 사운드 데이터 (Play 경우 필수)")]
        public SoundData soundData;

        [Header("Event")]
        [Tooltip("ActionType이 InvokeEvent일 때 실행될 내용")]
        public UnityEvent onExecute;
    }

    [System.Serializable]
    public class TriggerGroup
    {
        public string groupName = "Trigger Group";
        public TriggerCondition condition = TriggerCondition.OnStart;
        public List<AudioCommand> commands = new List<AudioCommand>();
    }

    [Header("Configuration")]
    [Tooltip("트리거별 명령 그룹 리스트")]
    public List<TriggerGroup> triggerGroups = new List<TriggerGroup>();

    private void Start()
    {
        ExecuteByCondition(TriggerCondition.OnStart);
    }

    private void OnEnable()
    {
        ExecuteByCondition(TriggerCondition.OnEnable);
    }

    private void OnDisable()
    {
        ExecuteByCondition(TriggerCondition.OnDisable);
    }

    private void OnDestroy()
    {
        // OnDestroy에서 코루틴 실행은 불안정할 수 있으므로 즉시 실행 권장
        // 하지만 구조상 ExecuteByCondition을 호출
        ExecuteByCondition(TriggerCondition.OnDestroy);
    }

    /// <summary>
    /// 외부에서 수동으로 특정 그룹을 실행하고 싶을 때 사용
    /// </summary>
    public void ExecuteManual(string groupName)
    {
        foreach (var group in triggerGroups)
        {
            if (group.groupName == groupName)
            {
                StartCoroutine(ProcessGroup(group));
            }
        }
    }

    private void ExecuteByCondition(TriggerCondition cond)
    {
        foreach (var group in triggerGroups)
        {
            if (group.condition == cond)
            {
                StartCoroutine(ProcessGroup(group));
            }
        }
    }

    private IEnumerator ProcessGroup(TriggerGroup group)
    {
        foreach (var cmd in group.commands)
        {
            // 개별 커맨드 딜레이 처리
            if (cmd.delay > 0f)
            {
                yield return new WaitForSeconds(cmd.delay);
            }

            ExecuteCommand(cmd);
        }
    }

    private void ExecuteCommand(AudioCommand cmd)
    {
        switch (cmd.actionType)
        {
            case AudioActionType.PlayBGM:
                if (cmd.soundData != null)
                {
                    SoundManager.Instance.PlayBGM(cmd.soundData);
                }
                break;

            case AudioActionType.PlaySFX:
                if (cmd.soundData != null)
                {
                    SoundManager.Instance.PlaySFX(cmd.soundData);
                }
                break;

            case AudioActionType.StopBGM:
                SoundManager.Instance.StopBGM();
                break;

            case AudioActionType.InvokeEvent:
                cmd.onExecute?.Invoke();
                break;
        }
    }
}
