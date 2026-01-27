using UnityEngine;

public class RainController : MonoBehaviour
{
    [Header("설정")]
    public Material targetMaterial;
    
    [Space]
    [Range(0, 1)] 
    public float rainIntensity = 0f; 
    
    public float drySpeed = 0.2f;
    public float wetSpeed = 0.5f;

    // 추가: 최대 흐름 속도 조절 변수
    public float maxFlowSpeed = 2.0f; 

    [Header("현재 상태")]
    [SerializeField] private float currentWaterLevel = 0f;

    private readonly int WaterLevelID = Shader.PropertyToID("_WaterLevel");
    private readonly int FlowSpeedID = Shader.PropertyToID("_FlowSpeed");

    void Start()
    {
        // 시작하자마자 확실하게 0으로 초기화
        currentWaterLevel = 0f;
        if (targetMaterial != null)
        {
            targetMaterial.SetFloat(WaterLevelID, 0f);
            targetMaterial.SetFloat(FlowSpeedID, 0f); // 속도도 0으로 시작
        }
    }

    void Update()
    {
        // 1. 수위 조절 (이전과 동일)
        if (rainIntensity > 0)
            currentWaterLevel = Mathf.MoveTowards(currentWaterLevel, 1f, wetSpeed * rainIntensity * Time.deltaTime);
        else
            currentWaterLevel = Mathf.MoveTowards(currentWaterLevel, 0f, drySpeed * Time.deltaTime);

        // 2. 값 적용
        if (targetMaterial != null)
        {
            targetMaterial.SetFloat(WaterLevelID, currentWaterLevel);

            // [수정된 부분]
            // 기본값 1.0f을 없앴습니다.
            // 비가 안 오면(0) -> 속도 0 (멈춤)
            // 비가 최대로 오면(1) -> 속도 maxFlowSpeed
            float currentSpeed = rainIntensity * maxFlowSpeed;
            targetMaterial.SetFloat(FlowSpeedID, currentSpeed);
        }
    }
}