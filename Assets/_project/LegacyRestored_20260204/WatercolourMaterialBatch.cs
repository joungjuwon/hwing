using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WatercolourMaterialBatch
{
    private const string TargetShaderName = "Custom/Watercolour";
    private static readonly string[] ExcludedRoots =
    {
        "Assets/FullOpaqueWater&Waterfall",
        "Assets/FullOpaqueGrass",
    };

    [MenuItem("Tools/Watercolour/Apply Watercolour Shader (Exclude Water/Grass)")]
    public static void ApplyWatercolourShader()
    {
        Shader targetShader = Shader.Find(TargetShaderName);
        if (targetShader == null)
        {
            Debug.LogError($"[Watercolour] Shader not found: {TargetShaderName}");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        int updated = 0;
        int skipped = 0;
        List<string> changed = new List<string>();

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsExcluded(path))
            {
                skipped++;
                continue;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                skipped++;
                continue;
            }

            if (mat.shader == targetShader)
            {
                continue;
            }

            Undo.RecordObject(mat, "Apply Watercolour Shader");
            mat.shader = targetShader;
            EditorUtility.SetDirty(mat);
            updated++;
            changed.Add(path);
        }

        if (updated > 0)
        {
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[Watercolour] Updated {updated} materials, skipped {skipped}. Excluded roots: {string.Join(", ", ExcludedRoots)}");
        if (changed.Count > 0)
        {
            foreach (string path in changed)
            {
                Debug.Log($"[Watercolour] Updated: {path}");
            }
        }
    }

    private static bool IsExcluded(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath)) return true;
        string normalized = assetPath.Replace('\\', '/');

        foreach (string root in ExcludedRoots)
        {
            if (normalized.StartsWith(root, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
