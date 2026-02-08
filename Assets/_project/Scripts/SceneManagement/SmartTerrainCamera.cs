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
// [AddComponentMenu("")] // Hide from Add Component menu (Use Extensions)
public class SmartTerrainCamera : CinemachineExtension
{
    [Header("Terrain Detection")]
    [Tooltip("바닥 감지를 위한 레이어 (Ground, Wall 등)")]
    public LayerMask collideAgainst = 1; // Default
    
    [Tooltip("감지할 구의 반경 (카메라 크기)")]
    public float collideRadius = 0.5f;

    [Tooltip("바닥 감지 시 카메라를 들어올리는 반응 속도")]
    public float liftSmoothness = 10f;

    [Tooltip("체크 시 부드럽게(Lerp), 해제 시 즉시(Hard) 들어올립니다.")]
    public bool enableSmoothness = false; // 기본값을 false로 하여 즉각적인 반응 유도

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
        // 카메라 위치(Aim)가 결정된 후 처리를 위해 Body 단계 이후에 실행
        // (CinemachineCollider와 충돌하지 않도록 Body 단계에서 처리)
        if (stage != CinemachineCore.Stage.Body) return;

        Vector3 camPos = state.RawPosition;
        float targetLift = 0f;

        // 카메라 위쪽에서 아래로 SphereCast를 쏴서 바닥을 감지합니다.
        // (단순 Raycast보다 넓은 범위를 감지하여 안정적)
        RaycastHit hit;
        Vector3 castOrigin = camPos + Vector3.up * 2.0f; // 카메라보다 2m 위에서 시작
        float castDist = 2.0f + maxLiftHeight; // 아래로 쏠 거리

        if (Physics.SphereCast(castOrigin, collideRadius, Vector3.down, out hit, castDist, collideAgainst))
        {
            // 바닥(Ground)을 감지했습니다.
            float groundY = hit.point.y;
            float desiredY = groundY + minHeightFromGround;

            // 현재 카메라 높이가 지면보다 낮거나, 최소 높이보다 낮다면 들어올려야 함
            if (camPos.y < desiredY)
            {
                targetLift = desiredY - camPos.y;
            }
        }

        // 부드럽게 보정값 적용 (Damping)
        if (deltaTime >= 0 && enableSmoothness)
        {
            // 내려갈 때는 천천히, 올라갈 때는 빠르게 반응하도록 설정 가능하지만,
            // 여기서는 균일하게 Smooth Damp를 적용합니다.
            currentLiftOffset = Mathf.Lerp(currentLiftOffset, targetLift, deltaTime * liftSmoothness);
        }
        else
        {
            currentLiftOffset = targetLift; // 즉시 반영 (Hard Clamp)
        }

        // 최종 위치 보정
        if (currentLiftOffset > 0.001f)
        {
            state.PositionCorrection += Vector3.up * currentLiftOffset;
        }
    }
}
