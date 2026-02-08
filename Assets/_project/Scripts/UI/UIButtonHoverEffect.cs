using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 같은 UI에 부착하면, 마우스를 올렸을 때 부드럽게 크기가 커지는 효과(Toggle/Hover)를 줍니다.
/// </summary>
public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    [Tooltip("마우스를 올렸을 때 커질 비율 (1.1 = 110%)")]
    public float hoverScale = 1.1f;
    [Tooltip("눌렀을 때 작아질 비율 (0.95 = 95%)")]
    public float clickScale = 0.95f;
    [Tooltip("크기 변화 속도")]
    public float transitionSpeed = 10f;
    
    [Header("References")]
    [Tooltip("크기를 변화시킬 타겟 (비워두면 자기 자신)")]
    public Transform targetTransform;

    private Vector3 originalScale;
    private Vector3 targetScale;
    
    private void Start()
    {
        if (targetTransform == null)
            targetTransform = transform;

        originalScale = targetTransform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        // 부드럽게 크기 보간 (Lerp)
        if (targetTransform != null)
        {
            targetTransform.localScale = Vector3.Lerp(targetTransform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    // 마우스가 들어갔을 때 -> 커짐
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    // 마우스가 나갔을 때 -> 원래대로
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    // 눌렀을 때 -> 살짝 작아짐 (클릭 피드백)
    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;
    }

    // 뗐을 때 -> 다시 커짐 (아직 마우스가 위에 있으니까)
    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    // 비활성화될 때 크기 원복 (안 그러면 커진 채로 남을 수 있음)
    private void OnDisable()
    {
        if (targetTransform != null)
            targetTransform.localScale = originalScale;
    }
}
