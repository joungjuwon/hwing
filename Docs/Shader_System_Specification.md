# Technical Specification: Global Watercolor System (Sinestesia Style)

**Version:** 2.0  
**Date:** 2026-02-02  
**Context:** Project "H-Wing" - Visual Style Overhaul

---

## 1. System Overview
 This system provides a unified, painterly "watercolor" aesthetic across the entire project rendering pipeline. It consists of a versatile Master Shader for standard assets, a shared HLSL library for custom implementation, and procedural tools for environment population.

### Key Components
| Component | Type | Path | Description |
| :--- | :--- | :--- | :--- |
| **Watercolour.shader** | Shader (URP) | `Assets/_project/Settings/Shaders/WatercolourObject/Watercolour.shader` | The standard "Uber Shader" for static meshes (Props, Rocks, Trees). |
| **WatercolorCore.hlsl** | HLSL Library | `Assets/_project/Shaders/Watercolor/WatercolorCore.hlsl` | Centralized shading mathematics for reusability in Shader Graphs. |
| **VFX_DisplayGrass.cs** | Script | `Assets/FullOpaqueWater&Waterfall/Script/VFX_DisplayGrass.cs` | Procedural grass spawner with Vertex Color masking. |

---

## 2. Watercolour Master Shader Specification
**Target Usage:** All opaque and cutout environment objects (Rocks, Trees, Architecture).

### 2.1 Base Properties
| Property Name | Inspector Label | Type | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `_BaseColor` | Colour Tint | Color | White | Global tint multiplier. |
| `_MainTex` | Base Map (Texture) | Texture2D | White | The Albedo/Diffuse texture of the asset. |
| `_Cutoff` | Alpha Cutout | Range(0-1) | 0.5 | Threshold for alpha clipping (Essential for foliage). |

### 2.2 Coloring & Shading
| Property Name | Inspector Label | Type | Description |
| :--- | :--- | :--- | :--- |
| `_ShadowColor` | Shadow Tint | Color | The color of the first shadow band (Cel Shade step). |
| `_DeepShadowColor` | Deep Shadow Colour | Color | The color of the deepest shadow areas (Occlusion). |

### 2.3 Noise & Texture Inputs
| Property Name | Inspector Label | Description |
| :--- | :--- | :--- |
| `_NoiseMap` | Noise Texture | Grayscale noise for painterly distortion of light/specular. |
| `_ShadowMap` | Shadow Texture | Pattern applied inside the shadow areas (e.g., paper grain). |
| `_DeepShadowMap` | Deep Shadow Texture | Pattern applied inside deep shadow areas. |
| `_NoiseStrength` | Noise Strength | Intensity of the light boundary distortion. |
| `_NoiseBrighten` | Noise Brighten | Adds subtle paper-like brightness variation to the base color. |

### 2.4 Shadow Controls
| Property Name | Range | Description |
| :--- | :--- | :--- |
| `_ShadowThreshold` | -1 to 1 | The NdotL value where the first shadow band starts. |
| `_ShadowSmoothness` | 0 to 0.5 | Softness of the shadow edge. |
| `_DeepShadowThreshold` | -1 to 1 | (Legacy/Unused in new Core logic, replaced by Falloff). |
| `_DeepShadowSpread` | -1 to 1 | Offsets the start point of the deep shadow gradient. |
| `_DeepShadowFalloff` | 0.1 to 5 | Power curve for the deep shadow gradient (soft vs hard transition). |

### 2.5 Advanced Effects (Rim & Outline)
| Feature | Properties | Description |
| :--- | :--- | :--- |
| **Fresnel Rim** | `_FresnelAmount`, `_FresnelPower`, `_FresnelThreshold` | Adds a "Hard Rim" light effect for edge definition. |
| **Specular** | `_SpecularColor`, `_Glossiness`, `_SpecularNoiseStrength` | Adds "Wet Paint" highlights perturbed by noise. |
| **Inner Outline** | `_UseInnerOutline`, `_InnerOutlineAlpha` | Renders a "Coffee Stain" effect on the object's inner edges. |
| **Outer Outline** | `_OutlineColor`, `_OutlineWidth` | Inverted Hull method for exterior outlines. |

---

## 3. Watercolor Core Library (`WatercolorCore.hlsl`)
**Target Usage:** Custom Function Nodes in Shader Graph (e.g., `S_Water`, `S_GrassTerrain`).

### Function Signature
```hlsl
void WatercolorCore_float(
    float3 BaseColor,
    float3 NormalWS,
    float3 LightDirection,
    float3 LightColor,
    float ShadowAttenuation,
    float3 ShadowColor,
    float3 DeepShadowColor,
    float NoiseVal,
    float NoiseStrength,
    float ShadowThreshold,
    float ShadowSmoothness,
    float DeepShadowThreshold, // Unused but kept for interface compatibility
    float DeepShadowSmoothness, // Unused
    float DeepShadowSpread,
    float DeepShadowFalloff,
    out float3 OutColor
)
```

### Logic Flow
1.  **Direct Lighting**: Calculates `NdotL` (Normal dot Light).
2.  **Noisy Distortion**: Perturbs `NdotL` using `NoiseVal` and `NoiseStrength`.
3.  **Two-Tone Shadowing**:
    *   **Tier 1 (Shadow)**: `smoothstep` based on `ShadowThreshold`.
    *   **Tier 2 (Deep Shadow)**: `pow` gradient based on `DeepShadowSpread` and `DeepShadowFalloff`.
4.  **Cast Shadow Integration**: Forces both shadow factors to 0 (Dark) if `ShadowAttenuation` indicates a cast shadow.
5.  **Compositing**: Lerps between `BaseColor` -> `ShadowColor` -> `DeepShadowColor`.

---

## 4. Grass Spawner System Specification
**Target Usage:** Procedural placement of grass on terrain meshes.

### Component: `MeshSpawner`
Located on the Terrain Mesh GameObject.

### Parameters
| Parameter | Description |
| :--- | :--- |
| **Prefab** | The mesh/prefab to spawn (e.g., `P_ParticleGrass`). |
| **Density** | Number of instances to attempt spawning. |
| **Max Slope Angle** | Max terrain angle to allow spawning (prevents grass on cliffs). |
| **Use Vertex Color Mask** | **[NEW]** Toggle usage of vertex painting validation. |
| **Mask Channel** | **[NEW]** Channel to test (R, G, B, or A). |

### Usage Logic
- Without Mask: Spawns randomly based on density and slope.
- With Mask:
    1.  Samples the Vertex Color at the random candidate point.
    2.  Reads the value of the selected `Mask Channel`.
    3.  Performs a chance check: `Random.value > MaskValue`.
    4.  If check fails, the instance is discarded (masked out).
    5.  Result: Grass spawns densely in white (1.0) areas and not at all in black (0.0) areas.
