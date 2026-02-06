using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

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
    public SproutEvent onSprout; 

    // 외부(SimulationManager)에서 접근 가능한 생존율 속성 (0.0 ~ 1.0)
    public float LifeRatio => Mathf.Clamp01(currentLifeTime / maxLifeTime);

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

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    private void HandleDeathPhysics()
    {
        rb.linearDamping = deathStopDamping;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 2f);

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
            Quaternion spawnRotation = Quaternion.identity; 

            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, Mathf.Infinity))
            {
                spawnPosition = hit.point;
            }
            
            Instantiate(deathSpawnPrefab, spawnPosition, spawnRotation);
            onSprout?.Invoke(spawnPosition);
        }

        Destroy(gameObject);
    }
}
