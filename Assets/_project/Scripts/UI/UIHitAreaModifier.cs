using UnityEngine;

/// <summary>
/// UI 요소(Button 등)의 클릭 판정 범위를 줄여주는 스크립트입니다.
/// 이미지 크기는 유지하면서, 실제 클릭되는 범위만 축소할 수 있습니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIHitAreaModifier : MonoBehaviour, ICanvasRaycastFilter
{
    [Header("Hit Area Scale")]
    [Tooltip("가로(좌우) 판정 범위 비율 (1.0 = 100%, 0.5 = 50%)")]
    [Range(0.1f, 1.0f)]
    public float hitWidthExample = 0.8f; // 예시: 80%만 클릭됨 (좌우 10%씩 줄어듦)

    [Tooltip("세로(상하) 판정 범위 비율 (1.0 = 100%, 0.5 = 50%)")]
    [Range(0.1f, 1.0f)]
    public float hitHeightExample = 0.8f;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 실제 클릭 좌표가 유효한지 검사합니다. (ICanvasRaycastFilter 구현)
    /// </summary>
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (rectTransform == null) return true;

        Vector2 localPoint;
        // 스크린 좌표를 로컬 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out localPoint))
        {
            Rect rect = rectTransform.rect;

            // 축소된 영역 계산
            float targetWidth = rect.width * hitWidthExample;
            float targetHeight = rect.height * hitHeightExample;

            // 중심을 기준으로 축소된 사각형 생성
            Rect hitRect = new Rect(
                rect.center.x - targetWidth * 0.5f,
                rect.center.y - targetHeight * 0.5f,
                targetWidth,
                targetHeight
            );

            // 클릭 지점이 축소된 사각형 안에 있는지 확인
            return hitRect.Contains(localPoint);
        }

        return false;
    }
}
