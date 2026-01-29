using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 셰이더 그래프(또는 머티리얼)의 파라미터를 조절하여 
/// 덩굴이 자라나거나 사라지는 효과를 제어하는 스크립트입니다.
/// </summary>
public class VineGrowthController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("효과를 적용할 UI 이미지 (RawImage 권장)")]
    public RawImage vineImage;

    [Tooltip("셰이더의 Growth 파라미터 이름 (Shader Graph에서 만든 이름)")]
    public string growthParameterName = "_Growth";

    [Tooltip("성장 속도 (값이 클수록 빠름)")]
    public float growSpeed = 0.5f;

    // 현재 성장 상태 (0: 없음, 1: 꽉 참)
    [Range(0f, 1f)]
    public float currentGrowth = 0f;

    private Material vineMaterial;
    private int growthParamId;
    private float targetGrowth = 0f;

    private void Start()
    {
        if (vineImage != null)
        {
            // 원본 머티리얼을 건드리지 않기 위해 인스턴스 생성
            vineMaterial = vineImage.material;
            growthParamId = Shader.PropertyToID(growthParameterName);
            
            // 초기값 적용
            UpdateShader();
        }
    }

    private void Update()
    {
        // 목표값으로 부드럽게 이동
        if (Mathf.Abs(currentGrowth - targetGrowth) > 0.001f)
        {
            currentGrowth = Mathf.MoveTowards(currentGrowth, targetGrowth, Time.deltaTime * growSpeed);
            UpdateShader();
        }
    }

    // 인스펙터에서 값을 조절할 때 실시간 반영을 위해 추가
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            targetGrowth = currentGrowth;
            UpdateShader();
        }
    }

    private void UpdateShader()
    {
        if (vineMaterial != null)
        {
            vineMaterial.SetFloat(growthParamId, currentGrowth);
        }
    }

    /// <summary>
    /// 덩굴을 자라나게 합니다. (0 -> 1)
    /// </summary>
    [ContextMenu("Grow (Test)")]
    public void Grow()
    {
        targetGrowth = 1f;
    }

    /// <summary>
    /// 덩굴을 사라지게 합니다. (1 -> 0)
    /// </summary>
    [ContextMenu("Wither (Test)")]
    public void Wither()
    {
        targetGrowth = 0f;
    }

    /// <summary>
    /// 즉시 특정 상태로 설정합니다.
    /// </summary>
    public void SetImmediate(float value)
    {
        targetGrowth = value;
        currentGrowth = value;
        UpdateShader();
    }
}
