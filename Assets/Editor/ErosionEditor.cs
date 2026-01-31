using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ErosionManager))]
public class ErosionEditor : Editor {
    
    void OnSceneGUI() {
        ErosionManager manager = (ErosionManager)target;
        
        // 메쉬가 없으면 붓질 불가
        if (manager.GetComponent<MeshFilter>().sharedMesh == null) return;

        Event e = Event.current;
        
        // 마우스 왼쪽 드래그 또는 클릭
        if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0) {
            
            // Alt키를 누르지 않았을 때만 (Alt는 화면 회전)
            if (!e.alt) {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;

                // MeshCollider를 향해 레이 발사
                if (Physics.Raycast(ray, out hit)) {
                    if (hit.transform == manager.transform) {
                        
                        // Shift 키: 지우개, 그냥: 그리기
                        if (e.shift) manager.EraseVertex(hit.point);
                        else manager.PaintVertex(hit.point);
                        
                        // 데이터 저장 알림
                        EditorUtility.SetDirty(manager);
                        
                        // 이벤트 사용 처리 (드래그로 화면 선택되는 것 방지)
                        e.Use();
                    }
                }
            }
        }
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        ErosionManager manager = (ErosionManager)target;

        GUILayout.Space(10);
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Initialize Mesh / Reset", GUILayout.Height(30))) {
            manager.InitializeTerrain();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(5);
        if (manager.GetComponent<MeshFilter>().sharedMesh == null) {
            EditorGUILayout.HelpBox("먼저 위의 'Initialize Mesh' 버튼을 눌러 지형을 생성하세요!", MessageType.Warning);
        } else {
            EditorGUILayout.HelpBox("Scene View에서 마우스로 드래그하여 침식 영역을 칠하세요.\n(Shift + 드래그: 지우기)", MessageType.Info);
        }
    }
}