using UnityEngine;

public class WindArea : MonoBehaviour
{
    [Header("Wind Settings")]
    public float strength = 20f;        // 바람의 세기
    public Vector3 direction = Vector3.up; // 바람의 방향 (기본값: 위쪽)
    public bool isGlobal = false;       // 전역 바람 여부 (테스트용)

    private void OnTriggerStay(Collider other)
    {
        // 리지드바디가 있는 물체(플레이어 등)가 들어오면 힘을 가함
        if (other.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // 방향 정규화 * 세기
            Vector3 windForce = direction.normalized * strength;
            rb.AddForce(windForce, ForceMode.Force);
        }
    }

    // 에디터에서 바람 방향을 눈으로 보기 위한 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = GetComponent<BoxCollider>() ? transform.position + GetComponent<BoxCollider>().center : transform.position;
        
        // 바람 방향 화살표 표시
        Vector3 endPos = center + direction.normalized * 3f;
        Gizmos.DrawLine(center, endPos);
        Gizmos.DrawSphere(endPos, 0.2f);
    }
}
