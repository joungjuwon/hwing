using UnityEngine;
using UnityEngine.InputSystem;

public class TPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6.0f;
    public float rotationSpeed = 10.0f;
    public float jumpForce = 7.0f;
    public float groundCheckDistance = 1.1f; // 캡슐 높이의 절반 + 여유값
    public LayerMask groundLayer;

    [Header("References")]
    public Transform cameraTransform; // 메인 카메라의 Transform

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isGrounded;

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
    }

    private void FixedUpdate()
    {
        CheckGround();
        Move();
    }

    private void Move()
    {
        if (moveInput.magnitude < 0.1f) return;

        // 1. 카메라가 바라보는 방향 기준으로 이동 방향 계산
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Y축 회전 영향 제거 (땅 위에서만 이동)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

        // 2. 캐릭터 회전 (부드럽게)
        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        // 3. 이동 (Rigidbody 속도 제어 - 무게감을 유지하면서 이동)
        // 기존 Y축 속도(낙하/점프)는 유지하고 X, Z 속도만 변경
        Vector3 currentVelocity = rb.linearVelocity; // Unity 6에서는 velocity 대신 linearVelocity 권장
        Vector3 targetVelocity = moveDir * moveSpeed;
        
        rb.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
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