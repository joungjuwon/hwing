using UnityEngine;
using UnityEditor;

public static class WatercolorRockTool
{
    private const string DefaultSourceRockMatPath = "Assets/FullOpaqueWater&Waterfall/Materials/M_Rock.mat";
    private const string DefaultWatercolorTemplateMatPath = "Assets/_project/Shaders/Watercolor/Material/Mat_Tree_Watercolor.mat";
    private const string OutputMatPath = "Assets/_project/Shaders/Watercolor/Material/Mat_Rock_Watercolor.mat";

    public static void CreateRockWatercolorMaterial()
    {
        // 1) Load shader
        var shader = Shader.Find("Watercolor/URP/Watercolour");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Watercolor", "Shader not found: Watercolor/URP/Watercolour", "OK");
            return;
        }

        // 2) Load template (ramps/paper setup)
        var template = AssetDatabase.LoadAssetAtPath<Material>(DefaultWatercolorTemplateMatPath);

        // 3) Load source rock material (for textures)
        var src = AssetDatabase.LoadAssetAtPath<Material>(DefaultSourceRockMatPath);

        // 4) Create material
        var mat = new Material(shader);
        mat.name = "Mat_Rock_Watercolor";

        // Copy template properties if available (ramps/paper/strengths/etc.)
        if (template != null)
        {
            mat.CopyPropertiesFromMaterial(template);
            mat.shader = shader; // CopyProperties can overwrite shader in some cases
        }

        // Map rock textures (best-effort)
        if (src != null)
        {
            // Common candidates in the S_Rock shadergraph material
            var cliffCol = src.GetTexture("_Cliff_Color_Height");
            var cliffNrm = src.GetTexture("_Cliff_Normal");
            var topNrm = src.GetTexture("_Top_Normal");

            if (cliffCol != null)
                mat.SetTexture("_MainTex", cliffCol);

            var chosenNormal = cliffNrm != null ? cliffNrm : topNrm;
            if (chosenNormal != null)
                mat.SetTexture("_BumpMap", chosenNormal);

            // Try to carry over tiling from the rock material if it uses one
            // (ShaderGraph property names differ; so we only copy if present.)
            if (src.HasProperty("_MainTex"))
                mat.SetTextureScale("_MainTex", src.GetTextureScale("_MainTex"));
        }

        // 5) Save asset (overwrite if exists)
        var existing = AssetDatabase.LoadAssetAtPath<Material>(OutputMatPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mat, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mat, OutputMatPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = mat;
        }

        EditorUtility.DisplayDialog("Watercolor", $"Created/Updated: {OutputMatPath} with Triplanar enabled.\n\nAssign this material to rock prefabs/meshes (including procedural/baked rock meshes).", "OK");
    }

    public static void ApplyRockWatercolorToSelection()
    {
        ApplyToInternal(Selection.gameObjects);
    }

    public static void ApplyRockWatercolorToAllInScene()
    {
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        var rocks = new System.Collections.Generic.List<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Rock", System.StringComparison.OrdinalIgnoreCase))
            {
                rocks.Add(obj);
            }
        }
        ApplyToInternal(rocks.ToArray());
    }

    private static void ApplyToInternal(GameObject[] targets)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(OutputMatPath);
        if (mat == null)
        {
            EditorUtility.DisplayDialog("Watercolor", $"Material not found. Run: {HwingMenuPaths.WatercolorMaterials}/Create Rock Material", "OK");
            return;
        }

        if (targets == null || targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Watercolor", "No GameObjects selected or found.", "OK");
            return;
        }

        int count = 0;
        Undo.RecordObjects(targets, "Apply Rock Watercolor Material");

        foreach (var go in targets)
        {
            if (go == null) continue;
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                Undo.RecordObject(r, "Apply Rock Watercolor Material");
                r.sharedMaterial = mat;
                EditorUtility.SetDirty(r);
                count++;
            }
        }

        EditorUtility.DisplayDialog("Watercolor", $"Applied Mat_Rock_Watercolor to {count} renderer(s).", "OK");
    }
}
