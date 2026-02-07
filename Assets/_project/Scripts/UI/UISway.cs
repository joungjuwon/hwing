using UnityEngine;

public class UISway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("흔들리는 속도")]
    public float speed = 1.0f;
    [Tooltip("흔들리는 각도 (좌우 범위)")]
    public float angleRange = 5.0f;
    [Tooltip("시간 오프셋 (여러 개일 때 다르게 움직이게 함)")]
    public float timeOffset = 0f;

    [Header("Pivot Override")]
    [Tooltip("피벗을 강제로 하단으로 설정할지 여부")]
    public bool forcePivotBottom = true;

    private Quaternion initialRotation;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialRotation = transform.localRotation;

        if (forcePivotBottom && rectTransform != null)
        {
            // 흔들릴 때 뿌리(아래)가 고정되도록 Pivot을 (0.5, 0)으로 변경
            SetPivot(new Vector2(0.5f, 0f));
        }
    }

    private void Update()
    {
        float time = Time.time * speed + timeOffset;
        float angle = Mathf.Sin(time) * angleRange;

        // 원래 회전값 + 흔들림 각도
        transform.localRotation = initialRotation * Quaternion.Euler(0, 0, angle);
    }

    // Pivot을 바꾸면서 위치가 튀지 않게 보정하는 함수
    private void SetPivot(Vector2 newPivot)
    {
        if (rectTransform == null) return;

        Vector2 size = rectTransform.rect.size;
        Vector2 deltaPivot = rectTransform.pivot - newPivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
        
        // Pivot 변경
        rectTransform.pivot = newPivot;
        // 위치 보정 (기존 위치 유지)
        rectTransform.localPosition -= deltaPosition;
    }
}
