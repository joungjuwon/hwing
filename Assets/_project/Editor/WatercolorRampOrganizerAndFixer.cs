using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WatercolorRampOrganizerAndFixer
{
    private const string Root = "Assets/_project/Shaders/Watercolor";
    private const string TargetTextures = Root + "/Textures";
    private const string TargetRamps = TargetTextures + "/Ramps";
    private const string TargetLeafRamps = TargetTextures + "/Ramps_Leaves";

    // Known sources we created earlier in this project
    private static readonly string[] SourceFolders =
    {
        "Assets/_project/Textures/WatercolorRamps",
        Root + "/Ramps_Leaves",
    };

    [MenuItem("Tools/Watercolor/Organize & Fix All Ramp Textures")]
    public static void OrganizeAndFix()
    {
        EnsureFolder(TargetTextures);
        EnsureFolder(TargetRamps);
        EnsureFolder(TargetLeafRamps);

        // 1) Move ramps into the new folder structure
        int moved = 0;
        foreach (var src in SourceFolders)
        {
            if (!AssetDatabase.IsValidFolder(src))
                continue;

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { src });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fileName = Path.GetFileName(path);
                bool isLeaf = fileName.StartsWith("Leaves_", StringComparison.OrdinalIgnoreCase);

                string dstFolder = isLeaf ? TargetLeafRamps : TargetRamps;
                string dstPath = AssetDatabase.GenerateUniqueAssetPath(dstFolder + "/" + fileName);

                if (path == dstPath)
                    continue;

                var err = AssetDatabase.MoveAsset(path, dstPath);
                if (string.IsNullOrEmpty(err))
                    moved++;
            }
        }

        // 2) Apply import settings by classification
        int fixedCount = 0;
        fixedCount += FixFolder(TargetRamps);
        fixedCount += FixFolder(TargetLeafRamps);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Watercolor",
            $"Done. Moved {moved} ramp texture(s) into {TargetTextures} and updated import settings for {fixedCount} texture(s).",
            "OK");
    }

    [MenuItem("Tools/Watercolor/Fix Ramp Imports (Scan Target Folder)")]
    public static void FixOnly()
    {
        EnsureFolder(TargetTextures);
        EnsureFolder(TargetRamps);
        EnsureFolder(TargetLeafRamps);

        int fixedCount = 0;
        fixedCount += FixFolder(TargetRamps);
        fixedCount += FixFolder(TargetLeafRamps);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Watercolor", $"Done. Updated import settings for {fixedCount} texture(s).", "OK");
    }

    private static int FixFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return 0;

        int changed = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            var kind = Classify(fileName);

            bool dirty = false;

            bool wantSRGB = kind == RampKind.Color;
            if (importer.sRGBTexture != wantSRGB) { importer.sRGBTexture = wantSRGB; dirty = true; }

            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }

            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }

            // Ramps behave more predictably with Point.
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
            }
        }

        return changed;
    }

    private enum RampKind { Mask, Color }

    private static RampKind Classify(string fileName)
    {
        // Mask ramps
        if (fileName.IndexOf("LightingA", StringComparison.OrdinalIgnoreCase) >= 0) return RampKind.Mask;
        if (fileName.IndexOf("EdgeA", StringComparison.OrdinalIgnoreCase) >= 0) return RampKind.Mask;
        if (fileName.IndexOf("EdgeB", StringComparison.OrdinalIgnoreCase) >= 0) return RampKind.Mask;

        // Color ramps
        if (fileName.IndexOf("LightingB", StringComparison.OrdinalIgnoreCase) >= 0) return RampKind.Color;
        if (fileName.IndexOf("EdgeCol", StringComparison.OrdinalIgnoreCase) >= 0) return RampKind.Color;

        // Default to mask (safer)
        return RampKind.Mask;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        var parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
