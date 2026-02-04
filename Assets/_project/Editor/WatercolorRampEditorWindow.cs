using System.IO;
using UnityEditor;
using UnityEngine;

public class WatercolorRampEditorWindow : EditorWindow
{
    private const int RampWidth = 512;
    private const int RampHeight = 1;

    private static readonly string[] MaskProps = { "_RampLightingA", "_RampEdgeA", "_RampEdgeB" };
    private static readonly string[] ColorProps = { "_RampLightingB", "_RampEdgeCol" };

    private Material _mat;
    private WatercolorRampPreset _preset;

    [MenuItem("Tools/Watercolor/Ramp Editor")]
    public static void Open()
    {
        var w = GetWindow<WatercolorRampEditorWindow>(false, "Watercolor Ramp Editor", true);
        w.minSize = new Vector2(420, 420);
        w.RefreshSelection();
        w.Show();
    }

    private void OnSelectionChange()
    {
        RefreshSelection();
        Repaint();
    }

    private void RefreshSelection()
    {
        // Prefer explicit selection in Project window; fall back to currently assigned material field.
        var selected = Selection.activeObject as Material;
        if (selected == null)
            selected = _mat;

        if (selected == null)
            return;

        _mat = selected;
        _preset = LoadOrCreatePresetForMaterial(_mat);
        _preset.EnsureDefaults();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            _mat = (Material)EditorGUILayout.ObjectField("Target Material", _mat, typeof(Material), false);
            if (GUILayout.Button("Use Selection", GUILayout.Width(110)))
                RefreshSelection();

            // If a material is manually assigned, ensure we have a preset loaded.
            if (_mat != null && _preset == null)
            {
                _preset = LoadOrCreatePresetForMaterial(_mat);
                _preset.EnsureDefaults();
            }
        }

        if (_mat == null)
        {
            EditorGUILayout.HelpBox("Select a Material to edit its watercolor ramps.", MessageType.Info);
            return;
        }

        if (_preset == null)
        {
            EditorGUILayout.HelpBox("No preset loaded. Click Use Selection.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gradients (edit like Blender ColorRamp)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _preset.lightingA = EditorGUILayout.GradientField(new GUIContent("Lighting A (mask/intensity)"), _preset.lightingA);
        _preset.lightingB = EditorGUILayout.GradientField(new GUIContent("Lighting B (palette/color)"), _preset.lightingB);
        _preset.edgeA = EditorGUILayout.GradientField(new GUIContent("Edge A (thin mask)"), _preset.edgeA);
        _preset.edgeB = EditorGUILayout.GradientField(new GUIContent("Edge B (thick mask)"), _preset.edgeB);
        _preset.edgeCol = EditorGUILayout.GradientField(new GUIContent("Edge Color (ink)"), _preset.edgeCol);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(_preset);
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply -> Generate textures & assign", GUILayout.Height(32)))
            {
                ApplyToMaterial(_mat, _preset);
            }

            if (GUILayout.Button("Ping Preset", GUILayout.Height(32), GUILayout.Width(110)))
            {
                EditorGUIUtility.PingObject(_preset);
                Selection.activeObject = _preset;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This generates 512x1 ramp textures next to the material and assigns them.\n" +
            "Mask ramps are Linear, palette/ink ramps are sRGB. Clamp/Point/NoMip/Uncompressed.",
            MessageType.None);
    }

    private static WatercolorRampPreset LoadOrCreatePresetForMaterial(Material mat)
    {
        string matPath = AssetDatabase.GetAssetPath(mat);
        string dir = Path.GetDirectoryName(matPath)?.Replace('\\', '/') ?? "Assets";
        string presetPath = $"{dir}/{mat.name}_WCRampPreset.asset";

        var preset = AssetDatabase.LoadAssetAtPath<WatercolorRampPreset>(presetPath);
        if (preset != null)
            return preset;

        preset = CreateInstance<WatercolorRampPreset>();
        preset.EnsureDefaults();

        // If there is a named preset that matches the material, you can manually assign it later.
        AssetDatabase.CreateAsset(preset, presetPath);
        AssetDatabase.SaveAssets();
        return preset;
    }

    private static void ApplyToMaterial(Material mat, WatercolorRampPreset preset)
    {
        if (mat == null || preset == null) return;

        string matPath = AssetDatabase.GetAssetPath(mat);
        string dir = Path.GetDirectoryName(matPath)?.Replace('\\', '/') ?? "Assets";

        AssetDatabase.StartAssetEditing();
        try
        {
            // Generate textures
            var texLightingA = WriteRampTexture(dir, mat.name + "_RampLightingA.png", preset.lightingA, linear: true);
            var texLightingB = WriteRampTexture(dir, mat.name + "_RampLightingB.png", preset.lightingB, linear: false);
            var texEdgeA = WriteRampTexture(dir, mat.name + "_RampEdgeA.png", preset.edgeA, linear: true);
            var texEdgeB = WriteRampTexture(dir, mat.name + "_RampEdgeB.png", preset.edgeB, linear: true);
            var texEdgeCol = WriteRampTexture(dir, mat.name + "_RampEdgeCol.png", preset.edgeCol, linear: false);

            Undo.RecordObject(mat, "Apply Watercolor Ramps");
            AssignIfExists(mat, "_RampLightingA", texLightingA);
            AssignIfExists(mat, "_RampLightingB", texLightingB);
            AssignIfExists(mat, "_RampEdgeA", texEdgeA);
            AssignIfExists(mat, "_RampEdgeB", texEdgeB);
            AssignIfExists(mat, "_RampEdgeCol", texEdgeCol);

            EditorUtility.SetDirty(mat);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[Watercolor] Generated ramp textures for {mat.name} and assigned to material.");
    }

    private static void AssignIfExists(Material mat, string prop, Texture2D tex)
    {
        if (mat == null || tex == null) return;
        if (!mat.HasProperty(prop)) return;
        mat.SetTexture(prop, tex);
    }

    private static Texture2D WriteRampTexture(string folder, string filename, Gradient gradient, bool linear)
    {
        if (gradient == null) return null;

        string path = $"{folder}/{filename}";

        // Create pixels
        var tex = new Texture2D(RampWidth, RampHeight, TextureFormat.RGBA32, mipChain: false, linear: linear);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;

        var cols = new Color[RampWidth];
        for (int x = 0; x < RampWidth; x++)
        {
            float t = (RampWidth <= 1) ? 0f : (x / (float)(RampWidth - 1));
            cols[x] = gradient.Evaluate(t);
        }
        tex.SetPixels(cols);
        // Keep readable until after encoding.
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        var png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        if (png == null || png.Length == 0)
            throw new System.Exception($"Failed to encode ramp PNG: {filename}");

        File.WriteAllBytes(path, png);

        // Import
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = !linear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.compressionQuality = 0;
            importer.npotScale = TextureImporterNPOTScale.None;

            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
