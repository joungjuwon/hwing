using UnityEngine;

public class PlayerLifeCycle : MonoBehaviour
{
    [Header("Life Settings")]
    public float maxLifeTime = 24.0f; // 최대 생존 시간
    public GameObject deathSpawnPrefab; // 죽을 때 생성할 오브젝트
    public GameObject playerVisuals; // 플레이어 모델
    public float deathStopDamping = 5.0f; // 죽은 뒤 멈출 때 적용할 마찰력

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
            // 바닥에 붙여서 생성하기 위해 레이캐스트 (TPSController의 설정을 가져오거나 직접 정의)
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, Mathf.Infinity))
            {
                spawnPosition = hit.point;
            }
            Instantiate(deathSpawnPrefab, spawnPosition, transform.rotation);
        }

        if (playerVisuals != null)
        {
            playerVisuals.SetActive(false);
        }
        else
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
        }
    }
}
