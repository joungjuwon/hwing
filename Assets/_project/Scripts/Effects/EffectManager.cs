using UnityEngine;

/// <summary>
/// 게임 내 모든 이펙트를 중앙에서 관리하는 매니저입니다.
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("Controllers")]
    public TitleEffectController titleEffectController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayTitleSequence()
    {
        if (titleEffectController != null)
        {
            titleEffectController.PlayEffect();
        }
    }

    public void SetGlobalWindStrength(float strength)
    {
        if (GlobalWindManager.Instance != null)
        {
            GlobalWindManager.Instance.SetWindStrength(strength);
        }
    }
}
