using UnityEngine;
using UnityEngine.Events; // UnityEvent 사용을 위해 추가

public class PlayerLifeCycle : MonoBehaviour
{
    // Inspector에서 Vector3 매개변수를 받는 이벤트를 보이기 위한 래퍼 클래스
    [System.Serializable]
    public class SproutEvent : UnityEvent<Vector3> { }

    [Header("Life Settings")]
    public float maxLifeTime = 24.0f; // 최대 생존 시간
    public GameObject deathSpawnPrefab; // 죽을 때 생성할 오브젝트
    public GameObject playerVisuals; // 플레이어 모델
    public float deathStopDamping = 5.0f; // 죽은 뒤 멈출 때 적용할 마찰력

    [Header("Events")]
    [Tooltip("싹이 트고 환경이 변하기 시작할 때 호출되는 이벤트")]
    public SproutEvent onSprout; // 위치 정보를 포함하여 이벤트를 호출하도록 변경

    private TPSController controller;
    private Rigidbody rb;
    private float currentLifeTime;
    private bool isDead = false;
    private bool hasSpawnedDeathObject = false;

    private void Awake()
    {
        controller = GetComponent<TPSController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentLifeTime = maxLifeTime;
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            HandleDeathPhysics();
            return;
        }

        // 컨트롤러가 있고 땅에 있을 때만 시간 감소
        if (controller != null && controller.IsGrounded)
        {
            currentLifeTime -= Time.fixedDeltaTime;
            if (currentLifeTime <= 0f)
            {
                Die();
            }
        }
    }

    // 사망 처리
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // 이동 컨트롤러 비활성화 (입력 및 이동 중단)
        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    // 사망 후 물리 처리 (멈출 때까지 감속 후 오브젝트 생성)
    private void HandleDeathPhysics()
    {
        // 죽었을 때 마찰력 적용
        rb.linearDamping = deathStopDamping;

        // 아직 오브젝트가 생성되지 않았고, 움직임이 거의 멈췄다면 생성
        if (!hasSpawnedDeathObject && rb.linearVelocity.sqrMagnitude < 0.01f && rb.angularVelocity.sqrMagnitude < 0.01f)
        {
            SpawnDeathObject();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SpawnDeathObject()
    {
        hasSpawnedDeathObject = true;

        if (deathSpawnPrefab != null)
        {
            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = Quaternion.identity; // 기본적으로 월드 정방향(위쪽)으로 설정

            // 바닥에 붙여서 생성하기 위해 레이캐스트 (TPSController의 설정을 가져오거나 직접 정의)
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, Mathf.Infinity))
            {
                spawnPosition = hit.point;
                // 선택 사항: 경사면에 맞춰 싹이 자라게 하려면 아래 코드 사용
                // spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            // 씨앗이 구르고 있으므로 transform.rotation 대신 정방향(spawnRotation) 사용
            Instantiate(deathSpawnPrefab, spawnPosition, spawnRotation);
            
            // 환경 변화 로직 실행 (예: 주변 땅의 메테리얼 변경, 나무 성장 시작 등)
            // 싹이 튼 위치(spawnPosition)를 이벤트와 함께 전달
            onSprout?.Invoke(spawnPosition);
        }

        // 플레이어 캐릭터 삭제 (시뮬레이션 모드 전환 후 불필요한 객체 제거)
        Destroy(gameObject);
    }
}
