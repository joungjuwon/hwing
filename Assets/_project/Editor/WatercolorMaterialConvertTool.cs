using System.IO;
using UnityEditor;
using UnityEngine;

public static class WatercolorMaterialConvertTool
{
    private const string GrassShaderName = "Watercolor/URP/Grass";

    public static void ConvertSelectedToWatercolorGrass()
    {
        var mat = Selection.activeObject as Material;
        if (mat == null)
        {
            EditorUtility.DisplayDialog("Watercolor", "Select a Material asset in the Project window.", "OK");
            return;
        }

        var shader = Shader.Find(GrassShaderName);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Watercolor", $"Shader not found: {GrassShaderName}\nMake sure WatercolourGrass.shader is imported.", "OK");
            return;
        }

        string srcPath = AssetDatabase.GetAssetPath(mat);
        if (string.IsNullOrEmpty(srcPath))
        {
            EditorUtility.DisplayDialog("Watercolor", "Could not resolve selected material path.", "OK");
            return;
        }

        string dir = Path.GetDirectoryName(srcPath)?.Replace('\\', '/') ?? "Assets";
        string file = Path.GetFileNameWithoutExtension(srcPath);
        string dstPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{file}_WatercolorGrass.mat");

        if (!AssetDatabase.CopyAsset(srcPath, dstPath))
        {
            EditorUtility.DisplayDialog("Watercolor", "Failed to duplicate material.", "OK");
            return;
        }

        var newMat = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
        if (newMat == null)
        {
            EditorUtility.DisplayDialog("Watercolor", "Failed to load duplicated material.", "OK");
            return;
        }

        Undo.RecordObject(newMat, "Convert to Watercolor Grass");
        newMat.shader = shader;

        // Best-effort mapping from common ShaderGraph names
        // Base texture
        CopyTextureIfExists(mat, newMat, "_BaseMap", "_MainTex");
        CopyTextureIfExists(mat, newMat, "_MainTex", "_MainTex");
        CopyColorIfExists(mat, newMat, "_BaseColor", "_BaseColor");

        // Alpha cutoff
        CopyFloatIfExists(mat, newMat, "_Cutoff", "_Cutoff");
        CopyFloatIfExists(mat, newMat, "_AlphaClipThreshold", "_Cutoff");

        // If the source had named grass colors, map a couple into top/bottom as a starting point
        CopyColorIfExists(mat, newMat, "Grass_HighLight", "_TopColor");
        CopyColorIfExists(mat, newMat, "Grass_Shadow", "_BottomColor");

        // Auto-assign default ramps if we can find them
        TryAssignRamp(newMat, "_RampLightingA", FindRamp("RampLightingA"));
        TryAssignRamp(newMat, "_RampLightingB", FindRamp("RampLightingB"));
        TryAssignRamp(newMat, "_RampEdgeA", FindRamp("RampEdgeA"));
        TryAssignRamp(newMat, "_RampEdgeB", FindRamp("RampEdgeB"));
        TryAssignRamp(newMat, "_RampEdgeCol", FindRamp("RampEdgeCol"));

        EditorUtility.SetDirty(newMat);
        AssetDatabase.SaveAssets();

        Selection.activeObject = newMat;
        EditorGUIUtility.PingObject(newMat);

        Debug.Log($"[Watercolor] Created {dstPath} and switched shader to {GrassShaderName}. Verify ramps/paper textures.");
    }

    private static void CopyTextureIfExists(Material src, Material dst, string srcProp, string dstProp)
    {
        if (!src.HasProperty(srcProp) || !dst.HasProperty(dstProp)) return;
        var tex = src.GetTexture(srcProp);
        if (tex != null) dst.SetTexture(dstProp, tex);
    }

    private static void CopyColorIfExists(Material src, Material dst, string srcProp, string dstProp)
    {
        if (!src.HasProperty(srcProp) || !dst.HasProperty(dstProp)) return;
        dst.SetColor(dstProp, src.GetColor(srcProp));
    }

    private static void CopyFloatIfExists(Material src, Material dst, string srcProp, string dstProp)
    {
        if (!src.HasProperty(srcProp) || !dst.HasProperty(dstProp)) return;
        dst.SetFloat(dstProp, src.GetFloat(srcProp));
    }

    private static void TryAssignRamp(Material mat, string prop, Texture tex)
    {
        if (tex == null) return;
        if (!mat.HasProperty(prop)) return;
        mat.SetTexture(prop, tex);
    }

    private static Texture FindRamp(string contains)
    {
        // Scan the new target folders first
        string[] searchFolders =
        {
            "Assets/_project/Shaders/Watercolor/Textures/Ramps",
            "Assets/_project/Shaders/Watercolor/Textures/Ramps_Leaves",
            "Assets/_project/Textures/WatercolorRamps",
            "Assets/_project/Shaders/Watercolor/Ramps_Leaves",
        };

        foreach (var folder in searchFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                continue;

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                if (name != null && name.Contains(contains))
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        return null;
    }
}
