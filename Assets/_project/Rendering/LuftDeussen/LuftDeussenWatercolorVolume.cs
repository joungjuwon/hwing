using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Post-processing/Luft-Deussen Watercolor")]
public class LuftDeussenWatercolorVolume : VolumeComponent, IPostProcessComponent
{
    [Header("Edge Detection")]
    public ClampedFloatParameter edgeThreshold = new ClampedFloatParameter(2.0f, 0.1f, 10.0f);
    public ClampedFloatParameter edgeDarkening = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
    
    [Header("Pigment Density")]
    public ClampedFloatParameter densityBase = new ClampedFloatParameter(1.0f, 0.5f, 2.0f);
    public ClampedFloatParameter densityContrast = new ClampedFloatParameter(0.5f, 0.0f, 2.0f);
    
    [Header("Texture Influence")]
    public ClampedFloatParameter paperStrength = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);
    public ClampedFloatParameter turbulenceStrength = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
    public ClampedFloatParameter dispersal1Strength = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);
    public ClampedFloatParameter dispersal2Strength = new ClampedFloatParameter(0.125f, 0.0f, 1.0f);
    public ClampedFloatParameter textureScale = new ClampedFloatParameter(1.0f, 0.1f, 10.0f);
    
    [Header("Wobble (Hand-Drawn Distortion)")]
    [Tooltip("UV distortion strength for hand-drawn look. 0 = no wobble.")]
    public ClampedFloatParameter wobbleStrength = new ClampedFloatParameter(0.5f, 0.0f, 2.0f);
    
    [Header("Color Quantization (Toon Steps)")]
    [Tooltip("Number of color bands. Higher = more colors, smoother. Lower = more toon-like.")]
    public ClampedIntParameter colorSteps = new ClampedIntParameter(8, 2, 32);
    [Tooltip("Enable/Disable color quantization.")]
    public BoolParameter enableQuantization = new BoolParameter(true);
    
    [Header("Blur")]
    public ClampedFloatParameter blurSize = new ClampedFloatParameter(1.0f, 0.0f, 4.0f);
    public ClampedIntParameter blurIterations = new ClampedIntParameter(1, 0, 3);
    
    public bool IsActive() => paperStrength.value > 0f || turbulenceStrength.value > 0f || wobbleStrength.value > 0f;
    public bool IsTileCompatible() => false;
}

