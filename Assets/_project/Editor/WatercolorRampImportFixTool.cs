using UnityEditor;
using UnityEngine;

public class WatercolorRampImportFixTool : EditorWindow
{
    private enum RampKind { Mask, Color }

    private RampKind kind = RampKind.Mask;
    private bool setClamp = true;
    private bool disableMipMaps = true;
    private FilterMode filterMode = FilterMode.Point;
    private bool setCompressionNone = true;

    public static void Open()
    {
        GetWindow<WatercolorRampImportFixTool>("Fix Ramp Imports");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fix import settings for selected ramp textures (recommended for 512x1 ramps).", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(8);

        kind = (RampKind)EditorGUILayout.EnumPopup("Ramp Kind", kind);
        setClamp = EditorGUILayout.ToggleLeft("Wrap Mode = Clamp", setClamp);
        disableMipMaps = EditorGUILayout.ToggleLeft("Disable MipMaps", disableMipMaps);
        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);
        setCompressionNone = EditorGUILayout.ToggleLeft("Compression = None", setCompressionNone);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Apply to Selected Textures"))
        {
            ApplyToSelection();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Select ramp textures in Project window first.\n" +
            "Mask ramps: sRGB OFF (LightingA, EdgeA, EdgeB).\n" +
            "Color ramps: sRGB ON (LightingB, EdgeCol).\n" +
            "MipMaps should be OFF for thin ramps; otherwise sampling can darken/crush values.",
            MessageType.Info);
    }

    private void ApplyToSelection()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Fix Ramp Imports", "Select one or more Texture assets in the Project window first.", "OK");
            return;
        }

        int changed = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool dirty = false;

            // sRGB
            bool wantSRGB = (kind == RampKind.Color);
            if (importer.sRGBTexture != wantSRGB)
            {
                importer.sRGBTexture = wantSRGB;
                dirty = true;
            }

            // Wrap
            if (setClamp)
            {
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    dirty = true;
                }
            }

            // Mipmaps
            if (disableMipMaps)
            {
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
            }

            // Filter
            if (importer.filterMode != filterMode)
            {
                importer.filterMode = filterMode;
                dirty = true;
            }

            // Compression
            if (setCompressionNone)
            {
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
            }
        }

        EditorUtility.DisplayDialog("Fix Ramp Imports", $"Done. Updated {changed} texture(s).", "OK");
    }
}
