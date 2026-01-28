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
        isPlaying = true;
        Debug.Log("[TitleEffect] Effect Started.");

        // UI 요소의 너비와 높이 가져오기
        RectTransform rectTransform = titleImage.GetComponent<RectTransform>();
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // ★ 편의성 개선: 파티클이 UI의 자식으로 있다면 "로컬 좌표"만 쓰면 됨 (복잡한 변환 불필요)
        bool isChild = petalParticle.transform.parent == titleImage.transform;

        Vector3 startPos, endPos;

        if (isChild)
        {
            // 2D UI 모드: 그냥 왼쪽 끝에서 오른쪽 끝으로 로컬 이동
            // RectTransform 기준: (0,0)이 중심 가정 (Pivot 0.5, 0.5)
            startPos = new Vector3(-width * 0.5f, 0, 0); 
            endPos = new Vector3(width * 0.5f, 0, 0);
            
            // 파티클의 Z값을 0으로 (UI와 딱 붙음)
            // 필요하다면 -10 등으로 앞으로 뺄 수 있음
            startPos.z = -10; 
            endPos.z = -10;

            Debug.Log("[TitleEffect] Using Local Space (Easy Mode)");
        }
        else
        {
            // 기존 월드 좌표 로직 (UI와 파티클이 분리된 경우)
             if (targetCamera == null) targetCamera = Camera.main;
            
             // ... (기존 변환 로직 유지 또는 간소화)
             // 여기서 복잡한 계산을 다시 하기보다 위 로직을 권장함. 
             // 호환성을 위해 유지하되 간단히 처리
             
             startPos = titleImage.transform.position;
             endPos = titleImage.transform.position; // Fallback
             
             if (rectTransform != null)
             {
                 Vector3[] corners = new Vector3[4];
                 rectTransform.GetWorldCorners(corners);
                 Vector3 centerLeft = (corners[0] + corners[1]) * 0.5f;
                 Vector3 centerRight = (corners[2] + corners[3]) * 0.5f;
                 
                 Canvas canvas = titleImage.canvas;
                 if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && targetCamera != null)
                 {
                     startPos = targetCamera.ScreenToWorldPoint(new Vector3(centerLeft.x, centerLeft.y, particleDepth));
                     endPos = targetCamera.ScreenToWorldPoint(new Vector3(centerRight.x, centerRight.y, particleDepth));
                 }
                 else
                 {
                     startPos = centerLeft;
                     endPos = centerRight;
                 }
             }
        }

        // 1. 파티클 재생 준비
        if (petalParticle != null)
        {
            if (isChild) petalParticle.transform.localPosition = startPos;
            else petalParticle.transform.position = startPos;

            // ★ Shape: 얇은 수직 선으로 형태 변경
            var shape = petalParticle.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.1f, height, 1f); // UI 높이만큼
            shape.rotation = new Vector3(0, 0, 0); 

            var emission = petalParticle.emission;
            emission.enabled = true;
            
            // 모래 효과를 위해 밀도 높임
            if (emission.rateOverTime.constant < 10)
            {
               var rate = emission.rateOverTime;
               rate.constant = 50; 
               emission.rateOverTime = rate;
            }

            petalParticle.Stop(); 
            petalParticle.Play();
        }

        // 2. 셰이더 Dissolve & 이미터 이동 진행
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float curveValue = Mathf.SmoothStep(0f, 1f, progress);

            if (titleMaterial != null)
            {
                titleMaterial.SetFloat(dissolveParamId, curveValue);
            }

            if (petalParticle != null)
            {
                if (isChild)
                    petalParticle.transform.localPosition = Vector3.Lerp(startPos, endPos, curveValue);
                else
                    petalParticle.transform.position = Vector3.Lerp(startPos, endPos, curveValue);
            }

            yield return null;
        }

        if (titleMaterial != null) titleMaterial.SetFloat(dissolveParamId, 1f);
        if (petalParticle != null)
        {
            var emission = petalParticle.emission;
            emission.enabled = false;
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
