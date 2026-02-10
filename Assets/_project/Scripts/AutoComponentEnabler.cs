using UnityEngine;
// HW_BioTreeRuntime이 정의된 네임스페이스를 참조해야 합니다.
using MysticForgeRuntime; 

public class AutoComponentEnabler : MonoBehaviour
{
    void Start()
    {
        EnableAllTargetComponents();
    }

    private void EnableAllTargetComponents()
    {
        // 1. 모든 MonoBehaviour(스크립트) 자동 감지 및 활성화
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var script in scripts)
        {
            if (script != this)
            {
                script.enabled = true;
            }
        }

        // 2. 모든 SkinnedMeshRenderer 자동 감지 및 활성화
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in renderers)
        {
            smr.enabled = true;
        }

        // =================================================================
        // [특수 처리] MysticForgeRuntime 트리 생성 강제 실행
        // =================================================================
        // 트리가 활성화된 직후, 메쉬와 뼈대를 생성하는 GenerateTree()를 호출해야만 눈에 보입니다.
        HW_BioTreeRuntime treeScript = GetComponentInChildren<HW_BioTreeRuntime>();
        if (treeScript != null)
        {
            // 이미 위에서 enabled = true가 되었지만, 확실하게 하기 위해 체크 후 생성
            if (treeScript.enabled)
            {
                treeScript.GenerateTree();
                Debug.Log("[AutoEnabler] HW_BioTreeRuntime의 GenerateTree()를 강제로 실행했습니다.");
            }
        }
        // =================================================================

        Debug.Log($"[AutoEnabler] {scripts.Length - 1}개의 스크립트와 {renderers.Length}개의 렌더러를 깨웠습니다.");
    }
}