using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 타이틀 화면의 흩날림 효과를 제어하는 컨트롤러입니다.
/// 셰이더의 Dissolve(사라짐)와 파티클 시스템(꽃잎 날림)을 동시에 실행합니다.
/// </summary>
public class TitleEffectController : MonoBehaviour
{
    [Header("Target UI")]
    [Tooltip("효과를 적용할 타이틀 이미지 (Material이 할당된 RawImage여야 함 - UIMeshSplitter 필수)")]
    public RawImage titleImage;

    [Header("Shader Settings")]
    [Tooltip("셰이더의 Dissolve 파라미터 이름")]
    public string dissolveParamName = "_DissolveAmount";
    
    [Tooltip("사라지는 속도 (초 단위 Duration)")]
    public float duration = 2.0f;

    [Tooltip("디졸브 진행 곡선 (X: 0~1 시간, Y: 0~1 진행도)")]
    public AnimationCurve effectCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // 내부 변수
    private Material titleMaterial;
    private int dissolveParamId;

    private void Start()
    {
        if (titleImage != null)
        {
            // 머티리얼 인스턴스 생성 (원본 보호)
            titleMaterial = titleImage.material;
            dissolveParamId = Shader.PropertyToID(dissolveParamName);

            // 초기화: 완전히 보이는 상태
            titleMaterial.SetFloat(dissolveParamId, 0f);
        }
    }

    /// <summary>
    /// 흩날리는 효과를 시작합니다. (외부 호출용)
    /// </summary>
    [ContextMenu("Play Effect (Test)")]
    public void PlayEffect()
    {
        StopAllCoroutines(); // 중복 방지
        StartCoroutine(ProcessEffect());
    }

    private IEnumerator ProcessEffect()
    {
        // Effect 시작 초기화
        if (titleMaterial != null)
        {
            titleMaterial.SetFloat(dissolveParamId, 0f);
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            
            // 공통 커브 평가
            float curveValue = effectCurve.Evaluate(progress);

            // 셰이더 파라미터 업데이트
            if (titleMaterial != null)
            {
                // Dissolve는 넉넉하게 1.5까지 가야 완전히 화면 밖으로 날아감
                titleMaterial.SetFloat(dissolveParamId, curveValue * 1.5f);
            }

            yield return null;
        }

        // 종료 처리 (확실하게 사라지도록)
        if (titleMaterial != null) titleMaterial.SetFloat(dissolveParamId, 1.5f);
    }
}
