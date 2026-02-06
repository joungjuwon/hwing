using UnityEngine;

// Holds per-material ramp gradients in a serializable asset.
// We generate 1D ramp textures from these gradients.
public class WatercolorRampPreset : ScriptableObject
{
    [Header("Lighting")]
    public Gradient lightingA;
    public Gradient lightingB;

    [Header("Edges")]
    public Gradient edgeA;
    public Gradient edgeB;
    public Gradient edgeCol;

    public static Gradient DefaultMaskGradient(float floor = 0f)
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(floor, floor, floor, 1f), 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
        return g;
    }

    public static Gradient DefaultColorGradient(Color a, Color b)
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(a, 0f),
                new GradientColorKey(b, 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
        return g;
    }

    public void EnsureDefaults()
    {
        lightingA ??= DefaultMaskGradient(0.25f);
        lightingB ??= DefaultColorGradient(new Color(0.86f, 0.86f, 0.86f, 1f), new Color(0.25f, 0.25f, 0.25f, 1f));
        edgeA ??= DefaultMaskGradient(0f);
        edgeB ??= DefaultMaskGradient(0f);
        edgeCol ??= DefaultColorGradient(new Color(0.18f, 0.18f, 0.18f, 1f), new Color(0.05f, 0.05f, 0.05f, 1f));
    }
}
