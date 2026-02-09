using UnityEditor;
using UnityEngine;

public class WatercolorDebugTool : EditorWindow
{
    private static readonly string DebugProp = "_WC_DebugView";

    public static void Open()
    {
        GetWindow<WatercolorDebugTool>("Watercolor Debug");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Applies _WC_DebugView on selected Materials.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(6);

        DrawButton(0, "Off");
        DrawButton(1, "BaseMap");
        DrawButton(2, "LightIntensity");
        DrawButton(3, "RampB");
        DrawButton(4, "EdgeMask");
        DrawButton(5, "EdgeColor");
        DrawButton(6, "Raw");
        DrawButton(7, "Tonemapped");
        DrawButton(8, "Final");

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("If nothing changes, make sure the material uses Watercolor/URP/Watercolour (or a shader that has _WC_DebugView).", MessageType.Info);
    }

    private static void DrawButton(float v, string label)
    {
        if (!GUILayout.Button(label)) return;

        var mats = Selection.GetFiltered<Material>(SelectionMode.Assets);
        if (mats == null || mats.Length == 0)
        {
            EditorUtility.DisplayDialog("Watercolor Debug", "Select one or more Materials in the Project window first.", "OK");
            return;
        }

        Undo.RecordObjects(mats, "Set Watercolor Debug View");
        foreach (var m in mats)
        {
            if (m == null) continue;
            if (m.HasProperty(DebugProp))
                m.SetFloat(DebugProp, v);
        }

        EditorUtility.SetDirty(mats[0]);
        AssetDatabase.SaveAssets();
    }
}
