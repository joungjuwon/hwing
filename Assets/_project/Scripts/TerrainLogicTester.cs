using UnityEngine;

public class TerrainLogicTester : MonoBehaviour
{
    public float rayDistance = 2.0f; // 바닥 탐지 거리
    private Renderer _renderer;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // 1. 내 위치에서 아래로 레이저를 쏜다
        Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);
        RaycastHit hit;

        // 2. 무언가(터레인 등)에 부딪혔다면?
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            // hit.normal : 부딪힌 지점의 표면이 향하는 방향 (경사도)
            // Vector3.up : 하늘 방향
            
            // 내적(Dot Product) 계산
            float slopeDot = Vector3.Dot(hit.normal, Vector3.up);

            // 3. 로직 판별 (아까와 동일한 논리)
            if (slopeDot > 0.95f)
            {
                // 평지 (웅덩이) -> 파란색
                _renderer.material.color = Color.blue;
                Debug.DrawLine(transform.position, hit.point, Color.blue); // 씬 뷰에서 선 그리기
            }
            else if (slopeDot > 0.5f)
            {
                // 경사면 (흐르는 물) -> 초록색
                _renderer.material.color = Color.green;
                Debug.DrawLine(transform.position, hit.point, Color.green);
            }
            else
            {
                // 급경사/절벽 -> 흰색
                _renderer.material.color = Color.white;
            }
        }
        else
        {
            // 공중에 떠 있음 -> 빨간색
            _renderer.material.color = Color.red;
        }
    }
}