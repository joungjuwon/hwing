using UnityEngine;

[ExecuteInEditMode] // 에디터에서도 실시간 확인 가능
public class SurfaceLogicTester : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Renderer _renderer;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        _renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // 로직 시각화를 위해 임시로 색상을 변경합니다.
        // 실제 게임에서는 이 계산이 쉐이더 내부에서 픽셀 단위로 일어납니다.
        
        // 1. 이 오브젝트의 윗면 방향(Up)이 월드의 하늘(Vector3.up)과 얼마나 일치하는지 계산
        // (오브젝트가 회전해 있을 수 있으므로 transform.up 사용)
        float slopeAngle = Vector3.Dot(transform.up, Vector3.up);

        // 2. 판별 로직
        if (slopeAngle > 0.95f) 
        {
            // 거의 평평함 -> 웅덩이(Puddle) 영역
            // 파란색으로 표시
            _renderer.sharedMaterial.color = Color.blue;
        }
        else if (slopeAngle > 0.5f)
        {
            // 적당한 경사 -> 물이 흐름(Flow) 영역
            // 초록색으로 표시
            _renderer.sharedMaterial.color = Color.green;
        }
        else
        {
            // 급경사 혹은 벽
            // 흰색(건조) 혹은 회색
            _renderer.sharedMaterial.color = Color.white;
        }

        // RainManager의 데이터가 잘 들어오는지 콘솔로 확인
        // if(Application.isPlaying && RainManager.Instance != null)
        // {
        //     // 실제 구현시엔 이 값을 쉐이더에 넣어주게 됩니다.
        //     // Debug.Log($"현재 젖음 정도: {RainManager.Instance.currentWetness}, 흐름 위치: {RainManager.Instance.flowOffset}");
        // }
    }
}