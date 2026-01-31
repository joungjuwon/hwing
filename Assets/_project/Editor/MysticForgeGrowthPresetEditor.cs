using UnityEditor;
using UnityEngine;
using ProceduralTreeGeneratorByMysticForge;

[CustomEditor(typeof(MysticForgeGrowthPreset))]
public class MysticForgeGrowthPresetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply to selected HW_TreeGrowthController"))
        {
            ApplyToSelection();
        }
    }

    private void ApplyToSelection()
    {
        var preset = (MysticForgeGrowthPreset)target;
        int applied = 0;

        foreach (var obj in Selection.gameObjects)
        {
            var controller = obj.GetComponent<HW_TreeGrowthController>();
            if (controller == null) continue;

            Undo.RecordObject(controller, "Apply MysticForge Growth Preset");
            preset.ApplyTo(controller);
            EditorUtility.SetDirty(controller);
            applied++;
        }

        if (applied == 0)
        {
            EditorUtility.DisplayDialog("Apply Preset", "선택된 오브젝트에 MysticForgeGrowthController가 없습니다.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Apply Preset", $"적용 완료: {applied}개 오브젝트", "OK");
        }
    }
}
