using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 타이틀 화면의 흩날림 효과를 제어하는 컨트롤러입니다.
/// 셰이더의 Dissolve(사라짐)와 파티클 시스템(꽃잎 날림)을 동시에 실행합니다.
/// </summary>
public class TitleEffectController : MonoBehaviour
{
    [Header("Target UI")]
    [Tooltip("효과를 적용할 타이틀 이미지 (Material이 할당된 RawImage여야 함)")]
    public RawImage titleImage;

    [Tooltip("꽃잎 효과 파티클 시스템")]
    public ParticleSystem petalParticle;

    [Header("Shader Settings")]
    [Tooltip("셰이더의 Dissolve 파라미터 이름")]
    public string dissolveParamName = "_DissolveAmount";
    
    [Tooltip("파티클 생성 깊이 (Overlay 모드일 때 카메라로부터의 거리)")]
    public float particleDepth = 10.0f; 

    [Tooltip("사라지는 속도 (초 단위 Duration)")]
    public float duration = 2.0f;

    // 내부 변수
    private Material titleMaterial;
    private int dissolveParamId;

    [Tooltip("좌표 변환에 사용할 카메라 (비워두면 MainCamera 사용)")]
    public Camera targetCamera;

    // 디버깅용 변수
    private Vector3 debugStartPos;
    private Vector3 debugEndPos;
    private bool isPlaying = false;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (titleImage != null)
        {
            // 머티리얼 인스턴스 생성 (원본 보호)
            titleMaterial = titleImage.material;
            dissolveParamId = Shader.PropertyToID(dissolveParamName);

            // 초기화: 완전히 보이는 상태
            titleMaterial.SetFloat(dissolveParamId, 0f);
        }

        // 파티클 설정 강제 적용 (보이게 하기 위함)
        if (petalParticle != null)
        {
            var renderer = petalParticle.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.sortingOrder = 100; 
            var emission = petalParticle.emission;
            emission.enabled = false;
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
        // 셰이더 기반 통합 효과 (ScatterDissolve)
        
        // 1. 필요한 컴포넌트 확인 (UIMeshSplitter)
        var meshSplitter = titleImage.GetComponent<UIMeshSplitter>();
        if (meshSplitter == null)
        {
            Debug.LogWarning("[TitleEffect] UIMeshSplitter component missing! Adding dynamically.");
            meshSplitter = titleImage.gameObject.AddComponent<UIMeshSplitter>();
        }
        
        // Effect 시작
        if (titleMaterial != null)
        {
            titleMaterial.SetFloat(dissolveParamId, 0f);
        }

        // 2. 파티클 시스템 (사용 안 함 - 셰이더로만 표현)
        if (petalParticle != null)
        {
            petalParticle.gameObject.SetActive(false);
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            
            // 셰이더 파라미터 업데이트
            float curveValue = Mathf.SmoothStep(0f, 1.5f, progress);

            if (titleMaterial != null)
            {
                titleMaterial.SetFloat(dissolveParamId, curveValue);
            }

            yield return null;
        }

        if (titleMaterial != null) titleMaterial.SetFloat(dissolveParamId, 1.5f);
        
        if (petalParticle != null)
        {
            petalParticle.Stop();
        }
        
        isPlaying = false;
    }

    private void OnDrawGizmos()
    {
        if (isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(debugStartPos, 0.5f); // 시작점 (초록)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(debugEndPos, 0.5f);   // 끝점 (빨강)
            Gizmos.DrawLine(debugStartPos, debugEndPos); // 이동 경로
        }
    }
}
