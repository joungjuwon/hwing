using UnityEditor;
using UnityEngine;
using ProceduralTreeGeneratorByMysticForge;

[CustomEditor(typeof(HW_TreeGrowthController))]
public class HW_TreeGrowthControllerEditor : Editor
{
    private MysticForgeGrowthPreset preset;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
        preset = (MysticForgeGrowthPreset)EditorGUILayout.ObjectField("Preset Asset", preset, typeof(MysticForgeGrowthPreset), false);

        using (new EditorGUI.DisabledScope(preset == null))
        {
            if (GUILayout.Button("Apply Preset to This Controller"))
            {
                var controller = (HW_TreeGrowthController)target;
                Undo.RecordObject(controller, "Apply HW Tree Growth Preset");
                preset.ApplyTo(controller);
                EditorUtility.SetDirty(controller);
                EditorUtility.DisplayDialog("Apply Preset", "현재 컨트롤러에 프리셋을 적용했습니다.", "OK");
            }
        }
    }
}
