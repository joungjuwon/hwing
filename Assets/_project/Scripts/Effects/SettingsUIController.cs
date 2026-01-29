using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 환경 설정 UI를 관리하는 컨트롤러입니다.
/// ESC 키로 켜고 끌 수 있으며, 사운드 볼륨과 타이틀 이동 기능을 제공합니다.
/// </summary>
public class SettingsUIController : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("설정 창 패널 (Canvas Group 또는 GameObject)")]
    public GameObject settingsPanel;

    [Header("Sound Controls")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Scene Navigation")]
    [Tooltip("타이틀 씬 인덱스 (Build Settings 기준)")]
    public int titleSceneIndex = 0;

    private bool isVisible = false;

    private void Start()
    {
        // 초기화: 지정되지 않았다면 시작 시 비활성화
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isVisible = false;
        }

        // 슬라이더 이벤트 연결
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            // 초기값 설정 (저장된 값이 있다면 불러오는 로직이 필요하지만, 여기선 기본값)
            masterVolumeSlider.value = 1.0f; 
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            bgmVolumeSlider.value = 1.0f;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            sfxVolumeSlider.value = 1.0f;
        }
    }

    private void Update()
    {
        // New Input System: ESC 키 입력 감지
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("[SettingsUI] ESC Key Pressed (New Input System)");
            ToggleSettingsUI();
        }
    }

    /// <summary>
    /// 설정 창을 켜고 끕니다.
    /// </summary>
    public void ToggleSettingsUI()
    {
        if (settingsPanel == null)
        {
            Debug.LogError("[SettingsUI] Settings Panel is NOT assigned in Inspector!");
            return;
        }

        isVisible = !isVisible;
        settingsPanel.SetActive(isVisible);
        Debug.Log($"[SettingsUI] Toggled UI: {isVisible}");

        // UI가 켜지면 마우스 커서 보이게, 꺼지면 게임 상태에 따라 다름 (일단은 원복)
        if (isVisible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f; // 게임 일시 정지 (선택 사항)
        }
        else
        {
            // 원래 상태로 복구 (게임 특성에 따라 다름)
            Time.timeScale = 1f;
        }
    }

    // --- 볼륨 제어 핸들러 ---

    public void OnMasterVolumeChanged(float volume)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetMasterVolume(volume);
    }

    public void OnBGMVolumeChanged(float volume)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMVolume(volume);
    }

    public void OnSFXVolumeChanged(float volume)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSFXVolume(volume);
    }

    // --- 네비게이션 ---

    /// <summary>
    /// 타이틀 화면으로 돌아갑니다.
    /// </summary>
    public void ReturnToTitle()
    {
        Time.timeScale = 1f; // 시간 정지 해제
        SceneManager.LoadScene(titleSceneIndex);
    }

    /// <summary>
    /// 설정 창을 닫습니다. (패널 닫기)
    /// </summary>
    public void CloseSettings()
    {
        // ToggleSettingsUI를 호출하면 켜진 상태에서 꺼짐으로 전환됨
        if (isVisible)
        {
            ToggleSettingsUI();
        }
    }
}
