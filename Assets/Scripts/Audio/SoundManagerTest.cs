using UnityEngine;
using UnityEngine.InputSystem; // New Input System Namespace

public class SoundManagerTest : MonoBehaviour
{
    [Header("Sound Data (ScriptableObject)")]
    [SerializeField] private SoundData bgmData;
    [SerializeField] private SoundData sfxData;

    private void Update()
    {
        // Check if Keyboard is available to avoid errors
        if (Keyboard.current == null) return;

        // 4~5: SoundData 테스트
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("Testing BGM Play (SoundData)");
            SoundManager.Instance.PlayBGM(bgmData);
        }
        else if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Debug.Log("Testing SFX Play (SoundData)");
            SoundManager.Instance.PlaySFX(sfxData);
        }

        // 6~7: 볼륨 조절 테스트 (MasterVolume)
        else if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            Debug.Log("Master Volume Down (0.5)");
            SoundManager.Instance.SetMasterVolume(0.5f);
        }
        else if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            Debug.Log("Master Volume Up (1.0)");
            SoundManager.Instance.SetMasterVolume(1.0f);
        }
    }
}
