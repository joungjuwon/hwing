using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

/// <summary>
/// 지형(Slope, Ground)에 카메라가 파묻히는 것을 방지하는 스크립트입니다.
/// 기존 Collider처럼 카메라를 앞으로 당기지(Zoom) 않고, 위로 들어올립니다(Lift).
/// </summary>
[ExecuteInEditMode]
[SaveDuringPlay]
[AddComponentMenu("")] // Hide from Add Component menu (Use Extensions)
public class SmartTerrainCamera : CinemachineExtension
{
    [Header("Terrain Detection")]
    [Tooltip("바닥 감지를 위한 레이어 (Ground, Wall 등)")]
    public LayerMask collideAgainst = 1; // Default
    
    [Tooltip("감지할 구의 반경 (카메라 크기)")]
    public float collideRadius = 0.5f;

    [Tooltip("바닥 감지 시 카메라를 들어올리는 반응 속도")]
    public float liftSmoothness = 10f;

    [Tooltip("카메라가 바닥에서 유지하려는 최소 높이")]
    public float minHeightFromGround = 0.5f;
    
    [Tooltip("카메라를 들어올리는 최대 높이 제한")]
    public float maxLiftHeight = 5.0f;

    private float currentLiftOffset = 0f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // 카메라의 최종 위치(Aim)가 결정된 후 처리를 위해 Aim 단계 이후에 실행
        if (stage != CinemachineCore.Stage.Body) return;

        // 1. 현재 카메라 위치 확인
        Vector3 camPos = state.RawPosition;
        
        // 2. 바닥 감지 (SphereCast Down/Back)
        // 카메라 위치에서 아래로 쏘는 것보다는, '카메라가 파묻혔는지' 확인이 중요.
        // Check 1: 카메라 위치 자체가 파묻혔는지 확인 (Physics.CheckSphere)
        // Check 2: 카메라 바로 아래에 땅이 있는지 확인 (Raycast)
        
        float targetLift = 0f;
        
        // 카메라 위치에서 아래로 레이캐스트
        RaycastHit hit;
        Vector3 castStart = camPos + Vector3.up * maxLiftHeight; // 위에서 아래로 쏨
        float castDistance = maxLiftHeight + minHeightFromGround; 

        // 카메라 위치 주변의 지형을 탐색
        if (Physics.SphereCast(camPos + Vector3.up * 2.0f, collideRadius, Vector3.down, out hit, 5.0f, collideAgainst))
        {
            // 땅을 발견함.
            // 땅 표면 위치: hit.point
            // 카메라가 위치해야 할 최소 높이: hit.point.y + minHeightFromGround
            
            float groundY = hit.point.y;
            float desiredY = groundY + minHeightFromGround;

            // 현재 카메라가 이 높이보다 낮다면, 들어올려야 함
            if (camPos.y < desiredY)
            {
                targetLift = desiredY - camPos.y;
            }
        }

        // 3. 부드럽게 보정값 적용
        if (deltaTime >= 0)
        {
            currentLiftOffset = Mathf.Lerp(currentLiftOffset, targetLift, deltaTime * liftSmoothness);
        }
        else
        {
            currentLiftOffset = targetLift; // Editor mode instant update
        }

        // 4. 카메라 위치 수정 (위로 들어올리기)
        if (currentLiftOffset > 0.001f)
        {
            state.PositionCorrection += Vector3.up * currentLiftOffset;
        }
    }
}
