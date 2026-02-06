using UnityEditor;
using UnityEngine;

public static class WatercolorRampPresetFactory
{
    private const string PresetFolder = "Assets/_project/Shaders/Watercolor/Presets";

    [MenuItem("Tools/Watercolor/Create Presets (Rose: Petal/StemLeaf/Wood)")]
    public static void CreateRosePresets()
    {
        EnsureFolder(PresetFolder);

        CreateOrOverwrite("WC_Rose_Petal", MakeLightingA(0.18f), MakePetalPalette(), MakeEdgeAThin(), MakeEdgeBThick(), MakeInk(new Color(0.23f, 0.10f, 0.12f)));
        CreateOrOverwrite("WC_Rose_StemLeaf", MakeLightingA(0.22f), MakeLeafPalette(), MakeEdgeAThin(), MakeEdgeBThick(), MakeInk(new Color(0.10f, 0.16f, 0.10f)));
        CreateOrOverwrite("WC_Rose_Wood", MakeLightingA(0.20f), MakeWoodPalette(), MakeEdgeAThin(), MakeEdgeBThick(), MakeInk(new Color(0.16f, 0.10f, 0.06f)));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Watercolor] Created Rose preset assets in " + PresetFolder);
    }

    private static void CreateOrOverwrite(string name, Gradient a, Gradient b, Gradient edgeA, Gradient edgeB, Gradient edgeCol)
    {
        string path = $"{PresetFolder}/{name}.asset";
        var preset = AssetDatabase.LoadAssetAtPath<WatercolorRampPreset>(path);
        if (preset == null)
        {
            preset = ScriptableObject.CreateInstance<WatercolorRampPreset>();
            AssetDatabase.CreateAsset(preset, path);
        }

        preset.lightingA = a;
        preset.lightingB = b;
        preset.edgeA = edgeA;
        preset.edgeB = edgeB;
        preset.edgeCol = edgeCol;

        EditorUtility.SetDirty(preset);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        // create nested
        var parts = folder.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    // ---- Gradient builders (max 8 keys) ----

    // LightingA: mask/intensity curve with a brighter floor (prevents crushed blacks)
    private static Gradient MakeLightingA(float floor)
    {
        return Grad(
            new[]
            {
                CK(new Color(floor, floor, floor, 1f), 0.00f),
                CK(new Color(Mathf.Lerp(floor, 1f, 0.25f), Mathf.Lerp(floor, 1f, 0.25f), Mathf.Lerp(floor, 1f, 0.25f), 1f), 0.35f),
                CK(Color.white, 1.00f),
            },
            Alpha1()
        );
    }

    // Palette for petals: warm pinks with gentle creamy highlights and deeper reds.
    private static Gradient MakePetalPalette()
    {
        return Grad(
            new[]
            {
                CK(new Color(0.92f, 0.88f, 0.85f, 1f), 0.00f), // highlight cream
                CK(new Color(0.90f, 0.70f, 0.73f, 1f), 0.35f), // soft pink
                CK(new Color(0.78f, 0.40f, 0.46f, 1f), 0.70f), // rose
                CK(new Color(0.45f, 0.16f, 0.20f, 1f), 1.00f), // deep shadow
            },
            Alpha1()
        );
    }

    // Palette for leaves/stem: warm greens (avoid neon) + olive shadows.
    private static Gradient MakeLeafPalette()
    {
        return Grad(
            new[]
            {
                CK(new Color(0.88f, 0.92f, 0.78f, 1f), 0.00f), // warm highlight
                CK(new Color(0.55f, 0.76f, 0.34f, 1f), 0.40f),
                CK(new Color(0.28f, 0.50f, 0.22f, 1f), 0.75f),
                CK(new Color(0.12f, 0.22f, 0.12f, 1f), 1.00f),
            },
            Alpha1()
        );
    }

    // Palette for wood: warm browns with paper-ish highlight.
    private static Gradient MakeWoodPalette()
    {
        return Grad(
            new[]
            {
                CK(new Color(0.90f, 0.86f, 0.80f, 1f), 0.00f),
                CK(new Color(0.70f, 0.52f, 0.34f, 1f), 0.45f),
                CK(new Color(0.45f, 0.28f, 0.16f, 1f), 0.80f),
                CK(new Color(0.20f, 0.12f, 0.07f, 1f), 1.00f),
            },
            Alpha1()
        );
    }

    // Edge masks: thin = mostly dark with a small bright ramp near end.
    private static Gradient MakeEdgeAThin()
    {
        return Grad(
            new[]
            {
                CK(Color.black, 0.00f),
                CK(Color.black, 0.78f),
                CK(new Color(0.6f, 0.6f, 0.6f, 1f), 0.92f),
                CK(Color.white, 1.00f),
            },
            Alpha1()
        );
    }

    // Thick edge mask: broader bright region.
    private static Gradient MakeEdgeBThick()
    {
        return Grad(
            new[]
            {
                CK(Color.black, 0.00f),
                CK(Color.black, 0.55f),
                CK(new Color(0.5f, 0.5f, 0.5f, 1f), 0.80f),
                CK(Color.white, 1.00f),
            },
            Alpha1()
        );
    }

    private static Gradient MakeInk(Color ink)
    {
        // Slight variation: mid ink -> deep ink
        return Grad(
            new[]
            {
                CK(new Color(Mathf.Clamp01(ink.r * 1.35f), Mathf.Clamp01(ink.g * 1.35f), Mathf.Clamp01(ink.b * 1.35f), 1f), 0.00f),
                CK(ink, 1.00f),
            },
            Alpha1()
        );
    }

    private static GradientAlphaKey[] Alpha1()
    {
        return new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
    }

    private static GradientColorKey CK(Color c, float t) => new GradientColorKey(c, t);

    private static Gradient Grad(GradientColorKey[] cks, GradientAlphaKey[] aks)
    {
        var g = new Gradient();
        g.SetKeys(cks, aks);
        return g;
    }
}
