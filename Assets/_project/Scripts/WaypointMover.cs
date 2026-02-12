using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("이동할 경로를 구성하는 웨이포인트들 (빈 게임오브젝트 등)")]
    public Transform[] waypoints;

    [Header("Movement Settings")]
    [Tooltip("이동 속도")]
    public float moveSpeed = 5.0f;
    [Tooltip("회전 속도 (이동 방향 바라보기)")]
    public float rotationSpeed = 5.0f;
    [Tooltip("이동 방향을 바라볼지 여부")]
    public bool lookAtTarget = true;
    [Tooltip("웨이포인트 도착 시 대기 시간")]
    public float waitTime = 0f;

    [Header("Collision Settings")]
    [Tooltip("플레이어와 충돌 시 멈출 시간")]
    public float collisionPauseDuration = 1.0f;
    [Tooltip("충돌 시 플레이어를 위로 밀어낼 힘")]
    public float pushUpForce = 10.0f;
    [Tooltip("플레이어 태그")]
    public string playerTag = "Player";

    private int currentTargetIndex = 0;
    private bool isMovingForward = true;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isPausedFromCollision = false;
    private float collisionPauseTimer = 0f;

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("Waypoints are not assigned in WaypointMover.");
            enabled = false;
            return;
        }

        // 시작 시 오브젝트를 첫 번째 웨이포인트 위치로 이동시킵니다.
        transform.position = waypoints[0].position;
        
        // 다음 목표는 두 번째 웨이포인트(인덱스 1)입니다.
        if (waypoints.Length > 1)
        {
            currentTargetIndex = 1;
        }
    }

    private void Update()
    {
        if (waypoints.Length < 2) return;

        // 충돌로 인한 일시 정지 처리
        if (isPausedFromCollision)
        {
            collisionPauseTimer += Time.deltaTime;
            if (collisionPauseTimer >= collisionPauseDuration)
            {
                isPausedFromCollision = false;
                collisionPauseTimer = 0f;
            }
            return;
        }

        // 대기 중이면 타이머 작동
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                SetNextWaypoint();
            }
            return;
        }

        Move();
    }

    private void Move()
    {
        Transform target = waypoints[currentTargetIndex];
        if (target == null) return;

        // 1. 이동
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        // 2. 회전 (이동 방향 바라보기)
        if (lookAtTarget)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // 3. 도착 확인 (거리가 매우 가까우면 도착으로 간주)
        if (Vector3.Distance(transform.position, target.position) < 0.01f)
        {
            // 대기 시간이 있으면 대기 상태로 전환, 없으면 바로 다음 목표 설정
            if (waitTime > 0f)
            {
                isWaiting = true;
            }
            else
            {
                SetNextWaypoint();
            }
        }
    }

    private void SetNextWaypoint()
    {
        // 정방향 이동 중
        if (isMovingForward)
        {
            currentTargetIndex++;
            // 배열의 끝에 도달하면
            if (currentTargetIndex >= waypoints.Length)
            {
                // 끝 바로 전 인덱스로 설정하고 역방향으로 전환
                currentTargetIndex = waypoints.Length - 2;
                isMovingForward = false;
            }
        }
        // 역방향 이동 중
        else
        {
            currentTargetIndex--;
            // 배열의 시작(0)보다 작아지면 (즉, 0에 도착하고 다음을 찾을 때)
            if (currentTargetIndex < 0)
            {
                // 1번 인덱스로 설정하고 정방향으로 전환
                currentTargetIndex = 1;
                isMovingForward = true;
            }
        }
        
        // 인덱스 안전 장치 (배열 크기가 2보다 작을 때 등 예외 처리)
        currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, waypoints.Length - 1);
    }
    
    // 에디터 씬 뷰에서 경로를 선으로 그려줍니다.
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i+1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
                Gizmos.DrawSphere(waypoints[i].position, 0.3f);
            }
        }
        // 마지막 점 표시
        if (waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.DrawSphere(waypoints[waypoints.Length - 1].position, 0.3f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            // 이동 일시 정지
            isPausedFromCollision = true;
            collisionPauseTimer = 0f;

            // 플레이어를 위로 밀어내기
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // 수직 속도 초기화 (확실한 반동을 위해)
                Vector3 velocity = playerRb.linearVelocity;
                velocity.y = 0f;
                playerRb.linearVelocity = velocity;

                playerRb.AddForce(Vector3.up * pushUpForce, ForceMode.Impulse);
            }
        }
    }
}
