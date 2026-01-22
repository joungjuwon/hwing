using UnityEngine;
using UnityEngine.InputSystem;

public class TPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6.0f;
    public float maxSpeed = 15.0f; // 최대 속도 제한
    public float maxRotationSpeed = 15.0f; // 최대 회전 속도 제한
    public float rotationCorrectionSpeed = 5.0f; // 회전 보정 속도 (옆으로 구르기 방지)
    public float rotationSpeed = 10.0f;
    public float jumpForce = 7.0f;
    public float moveInterval = 1.0f; // 이동 입력 간격 (초)
    public float moveDamping = 1.0f;  // 이동 중일 때의 저항 (낮을수록 잘 미끄러짐)
    public float stopDamping = 5.0f;  // 멈출 때의 저항 (높을수록 빨리 멈춤)
    public float groundCheckDistance = 1.1f; // 캡슐 높이의 절반 + 여유값
    public LayerMask groundLayer;

    [Header("References")]
    public Transform cameraTransform; // 메인 카메라의 Transform

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isGrounded;
    public bool IsGrounded => isGrounded; // 외부에서 접근 가능하도록 프로퍼티 추가
    private float nextMoveTime; // 다음 이동 가능 시간
    private Vector3 currentForward; // 캐릭터의 정방향 (이동 방향)

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Input System 초기화
        inputActions = new PlayerInputActions();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed += ctx => Jump();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Start()
    {
        // 마우스 커서 숨기기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // 물리 회전을 위해 회전 잠금 해제 (넘어질 수 있게 함)
        rb.freezeRotation = false;
        
        // 초기 정방향 설정
        currentForward = transform.forward;
    }

    private void FixedUpdate()
    {
        CheckGround();

        ControlDrag();
        CalculateForward(); // 정방향 계산
        Move();
        LimitSpeed();
        LimitRotationSpeed();
        CorrectRotation();
    }

    private void CalculateForward()
    {
        // 입력이 있으면 입력 방향을 정방향으로 설정
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            currentForward = (forward * moveInput.y + right * moveInput.x).normalized;
        }
        // 입력이 없으면 현재 속도 방향을 정방향으로 유지 (자연스럽게 굴러가도록)
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.sqrMagnitude > 0.1f)
            {
                currentForward = flatVel.normalized;
            }
        }
    }

    private void Move()
    {
        // 쿨타임 체크: 지정된 시간이 지나야 다시 힘을 가함
        if (Time.time < nextMoveTime) return;

        if (moveInput.magnitude < 0.1f) return;

        // 2. 캐릭터 회전 (굴러다니게 수정)
        // 이동 방향의 수직 벡터(회전축)를 구하여 토크를 가함
        Vector3 rotationAxis = Vector3.Cross(Vector3.up, currentForward);
        rb.AddTorque(rotationAxis * rotationSpeed, ForceMode.Impulse); // 순간적인 회전력 적용

        // 3. 이동 (힘으로 이동하도록 수정)
        // 속도를 직접 제어하지 않고, 순간적인 힘(Impulse)을 가하여 튕겨나가듯 이동
        rb.AddForce(currentForward * moveSpeed, ForceMode.Impulse);

        // 다음 이동 시간 설정
        nextMoveTime = Time.time + moveInterval;
    }

    private void ControlDrag()
    {
        // 입력 여부에 따라 마찰력(Damping) 조절
        // Unity 6에서는 drag 대신 linearDamping 사용
        rb.linearDamping = (moveInput.magnitude < 0.1f) ? stopDamping : moveDamping;
    }

    private void LimitSpeed()
    {
        // 수평 속도 제한 (낙하 속도인 Y축은 건드리지 않음)
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void LimitRotationSpeed()
    {
        // 회전 속도 제한
        if (rb.angularVelocity.magnitude > maxRotationSpeed)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxRotationSpeed;
        }
    }

    private void CorrectRotation()
    {
        // 정방향(currentForward)을 기준으로 회전 축을 정렬하여 옆으로 구르는 현상 방지
        
        // 정방향의 오른쪽 벡터 (올바른 회전 축)
        Vector3 desiredRotationAxis = Vector3.Cross(Vector3.up, currentForward).normalized;

        // 현재 회전 속도를 올바른 축에 투영 (원하는 방향의 회전 성분만 남김)
        Vector3 projectedAV = Vector3.Project(rb.angularVelocity, desiredRotationAxis);

        // 현재 회전 속도를 정방향 회전 속도로 부드럽게 보정
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, projectedAV, Time.fixedDeltaTime * rotationCorrectionSpeed);
    }

    private void Jump()
    {
        if (isGrounded)
        {
            // 즉각적인 힘을 가해 점프 (ForceMode.Impulse)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void CheckGround()
    {
        // 캐릭터 중심에서 아래로 레이를 쏘아 바닥 감지
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
    }
    
    // 바닥 체크 시각화 (디버깅용)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance);
    }
}