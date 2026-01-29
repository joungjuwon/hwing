using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 타이틀 화면에서 아무 키나 누르면 다음 씬으로 넘어가는 기능을 담당합니다.
/// </summary>
public class TitleInputController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("이동할 다음 씬의 빌드 인덱스")]
    public int nextSceneIndex = 1;

    [Tooltip("입력을 받기 시작할 때까지의 지연 시간 (초)")]
    public float inputDelay = 1.0f;

    private bool canInput = false;

    [Header("Effect Settings")]
    [Tooltip("연결할 타이틀 이펙트 컨트롤러 (없으면 즉시 이동)")]
    public TitleEffectController titleEffectController;

    private void Start()
    {
        // 씬 시작 후 바로 넘어가면 당황스러우므로 약간의 딜레이를 줌
        Invoke(nameof(EnableInput), inputDelay);
    }

    private void EnableInput()
    {
        canInput = true;
    }

    private void Update()
    {
        if (!canInput) return;

        // New Input System: 키보드나 마우스 입력 감지
        bool inputDetected = false;

        // 1. 키보드 입력 체크
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            inputDetected = true;
        }

        // 2. 마우스 클릭 체크
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputDetected = true;
        }

        if (inputDetected)
        {
            StartCoroutine(TransitionSequence());
        }
    }

    private System.Collections.IEnumerator TransitionSequence()
    {
        // 중복 입력 방지
        canInput = false;

        if (titleEffectController != null)
        {
            Debug.Log("[TitleInput] Playing Title Effect...");
            titleEffectController.PlayEffect();
            // 효과 지속 시간만큼 대기
            yield return new WaitForSeconds(titleEffectController.duration);
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        Debug.Log($"[TitleInput] Loading Scene Index: {nextSceneIndex}");
        SceneManager.LoadScene(nextSceneIndex);
    }
}
