using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
    // 인스펙터에서 활성화하고 싶은 스크립트들을 여기에 드래그해서 넣으세요.
    [SerializeField] private MonoBehaviour[] scriptsToEnable;

    void Start()
    {
        if (scriptsToEnable == null) return;

        foreach (var script in scriptsToEnable)
        {
            if (script != null)
            {
                script.enabled = true;
                Debug.Log($"{script.GetType().Name} 스크립트가 활성화되었습니다.");
            }
        }
    }
}