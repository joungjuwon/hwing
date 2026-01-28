using UnityEngine;

/// <summary>
/// 전역 바람 시스템을 관리하는 싱글톤 클래스입니다.
/// 모든 쉐이더가 공유하는 바람 관련 Global 변수를 업데이트합니다.
/// </summary>
public class GlobalWindManager : MonoBehaviour
{
    public static GlobalWindManager Instance { get; private set; }

    [Header("Wind Settings")]
    [Tooltip("바람의 방향 (X, Z)")]
    public Vector2 windDirection = new Vector2(1.0f, 0.5f);
    
    [Tooltip("바람의 세기")]
    [Range(0f, 5f)]
    public float windStrength = 1.0f;

    [Tooltip("바람의 속도")]
    [Range(0f, 5f)]
    public float windSpeed = 1.0f;

    private int windDirId;
    private int windStrengthId;
    private int globalWindTimeId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeShaderProperties();
    }

    private void InitializeShaderProperties()
    {
        windDirId = Shader.PropertyToID("_GlobalWindDir");
        windStrengthId = Shader.PropertyToID("_GlobalWindStrength");
        globalWindTimeId = Shader.PropertyToID("_GlobalWindTime");
    }

    private void Update()
    {
        UpdateShaderGlobals();
    }

    private void UpdateShaderGlobals()
    {
        Vector2 dir = windDirection.normalized;
        float windTime = Time.time * windSpeed;

        Shader.SetGlobalVector(windDirId, new Vector4(dir.x, dir.y, 0, 0));
        Shader.SetGlobalFloat(windStrengthId, windStrength);
        Shader.SetGlobalFloat(globalWindTimeId, windTime);
    }

    public void SetWindStrength(float strength)
    {
        windStrength = strength;
    }
}
