# Blender to Unity URP Migration Analysis

## 1. Blender Node Graph (JSON Representation)

This JSON represents the **complete, node-by-node** structure. Every node (including Math nodes) and every connection matches the screenshots exactly to allow 1:1 reconstruction.

```json
{
  "BlenderProject": {
    "Shared_NodeGroups": {
      "Paper_Texture_Subgraph": {
        "Type": "NodeGroup",
        "Nodes": [
          { "Id": "Group_Input", "Type": "NodeGroupInput", "Outputs": ["Vector"] },
          {
            "Id": "TexCoord",
            "Type": "ShaderNodeTexCoord",
            "Outputs": ["Generated"]
          },
          {
            "Id": "Mapping",
            "Type": "ShaderNodeMapping",
            "Inputs": { "Vector": "TexCoord.Generated" },
            "Outputs": ["Vector"]
          },
          {
            "Id": "Image_Texture",
            "Type": "ShaderNodeTexImage",
            "Inputs": { "Vector": "Mapping.Vector" },
            "Properties": { "Image": "watercolor_paper.jpg" },
            "Outputs": ["Color"]
          },
          {
            "Id": "Hue_Sat",
            "Type": "ShaderNodeHueSaturation",
            "Inputs": { "Color": "Image_Texture.Color", "Saturation": 1.0, "Value": 1.0 },
            "Outputs": ["Color"]
          },
          {
            "Id": "RGB_Curves",
            "Type": "ShaderNodeRGBCurve",
            "Inputs": { "Color": "Hue_Sat.Color" },
            "Outputs": ["Color"]
          },
          {
            "Id": "Group_Output",
            "Type": "NodeGroupOutput",
            "Inputs": { "Color": "RGB_Curves.Color" }
          }
        ]
      }
    },
    "Object_Material": {
      "Name": "Watercolor_Rose",
      "Nodes": [
        { "Comment": "--- 1. Normals Logic ---" },
        {
          "Id": "TexCoord",
          "Type": "ShaderNodeTexCoord",
          "Outputs": ["Generated"]
        },
        {
          "Id": "Offset_Strength",
          "Type": "ShaderNodeValue",
          "Properties": { "Value": 0.7 },
          "Outputs": ["Value"]
        },
        {
          "Id": "Add_Offset",
          "Type": "ShaderNodeVectorMath",
          "Operation": "ADD",
          "Inputs": { "Vector": "TexCoord.Generated", "Vector_001": "Offset_Strength.Value" }, 
          "Outputs": ["Vector"]
        },
        {
          "Id": "Noise_1",
          "Type": "ShaderNodeTexNoise",
          "Inputs": { "Vector": "Add_Offset.Vector" },
          "Properties": { "Scale": 5.6 },
          "Outputs": ["Fac"]
        },
        {
          "Id": "Noise_2",
          "Type": "ShaderNodeTexNoise",
          "Inputs": { "Vector": "Add_Offset.Vector" },
          "Properties": { "Scale": 14.6 },
          "Outputs": ["Fac"]
        },
        {
          "Id": "Noise_3",
          "Type": "ShaderNodeTexNoise",
          "Inputs": { "Vector": "Add_Offset.Vector" },
          "Properties": { "Scale": 3.58 },
          "Outputs": ["Fac"]
        },
        {
          "Id": "Mult_1",
          "Type": "ShaderNodeMath",
          "Operation": "MULTIPLY",
          "Inputs": { "Value": "Noise_1.Fac", "Value_001": "Offset_Strength.Value" },
          "Outputs": ["Value"]
        },
        {
          "Id": "Mult_2",
          "Type": "ShaderNodeMath",
          "Operation": "MULTIPLY",
          "Inputs": { "Value": "Noise_2.Fac", "Value_001": "Offset_Strength.Value" },
          "Outputs": ["Value"]
        },
        {
          "Id": "Mult_3",
          "Type": "ShaderNodeMath",
          "Operation": "MULTIPLY",
          "Inputs": { "Value": "Noise_3.Fac", "Value_001": "Offset_Strength.Value" },
          "Outputs": ["Value"]
        },
        {
          "Id": "Combine_XYZ",
          "Type": "ShaderNodeCombineXYZ",
          "Inputs": { "X": "Mult_1.Value", "Y": "Mult_2.Value", "Z": "Mult_3.Value" },
          "Outputs": ["Vector"]
        },
        {
          "Id": "Geometry",
          "Type": "ShaderNodeNewGeometry",
          "Outputs": ["Normal", "Incoming"]
        },
        {
          "Id": "Add_To_Normal",
          "Type": "ShaderNodeVectorMath",
          "Operation": "ADD",
          "Inputs": { "Vector": "Geometry.Normal", "Vector_001": "Combine_XYZ.Vector" },
          "Outputs": ["Vector"]
        },

        { "Comment": "--- 2. Lighting Logic ---" },
        {
          "Id": "Diffuse_BSDF",
          "Type": "ShaderNodeBsdfDiffuse",
          "Inputs": { "Normal": "Add_To_Normal.Vector" },
          "Outputs": ["BSDF"]
        },
        {
          "Id": "Shader_To_RGB",
          "Type": "ShaderNodeShaderToRGB",
          "Inputs": { "Shader": "Diffuse_BSDF.BSDF" },
          "Outputs": ["Color"]
        },
        {
          "Id": "Lighting_Ramp",
          "Type": "ShaderNodeValToRGB",
          "Inputs": { "Fac": "Shader_To_RGB.Color" }, 
          "Properties": { "Stops": "0.36(Dark), 0.6(Light)" },
          "Outputs": ["Color"]
        },

        { "Comment": "--- 3. Edges Logic ---" },
        {
          "Id": "Dot_Product",
          "Type": "ShaderNodeVectorMath",
          "Operation": "DOT_PRODUCT",
          "Inputs": { "Vector": "Add_To_Normal.Vector", "Vector_001": "Geometry.Incoming" },
          "Outputs": ["Value"]
        },
        {
          "Id": "Edge_Ramp",
          "Type": "ShaderNodeValToRGB",
          "Inputs": { "Fac": "Dot_Product.Value" },
          "Properties": { "Stops": "0.4(Color), 0.5(White)" },
          "Outputs": ["Color"]
        },

        { "Comment": "--- 4. Composition ---" },
        {
          "Id": "Mix_Lighting_Edges",
          "Type": "ShaderNodeMixRGB",
          "Inputs": { 
             "Fac": "Edge_Ramp.Color", 
             "Color1": "Lighting_Ramp.Color", 
             "Color2": "Edge_Color_Global" 
          },
          "Outputs": ["Color"]
        },
        {
          "Id": "Paper_Instance",
          "Type": "Group",
          "NodeTree": "Paper_Texture_Subgraph",
          "Outputs": ["Color"]
        },
        {
          "Id": "Final_Multiply",
          "Type": "ShaderNodeMixRGB",
          "BlendMode": "MULTIPLY",
          "Inputs": { "Color1": "Mix_Lighting_Edges.Color", "Color2": "Paper_Instance.Color" },
          "Outputs": ["Color"]
        }
      ]
    },
    "World_Shader": {
      "Name": "World",
      "Nodes": [
        {
          "Id": "Background",
          "Type": "ShaderNodeBackground",
          "Outputs": ["Background"]
        },
        {
          "Id": "Paper_Instance_World",
          "Type": "Group",
          "NodeTree": "Paper_Texture_Subgraph",
          "Outputs": ["Color"]
        },
        {
          "Id": "World_Multiply",
          "Type": "ShaderNodeMixRGB",
          "BlendMode": "MULTIPLY",
          "Inputs": { "Color1": "Background.Background", "Color2": "Paper_Instance_World.Color" },
          "Outputs": ["Color"]
        },
        {
          "Id": "World_Output",
          "Type": "ShaderNodeOutputWorld",
          "Inputs": { "Surface": "World_Multiply.Color" }
        }
      ]
    }
  }
}
```

## 2. Render Pipeline Comparison (Blender Eevee vs Unity URP)

### A. Lighting Calculation & `Shader to RGB`
*   **Blender (Eevee)**:
    *   **Feature**: `Shader to RGB` node allows extracting the results of a BSDF (like Diffuse) as raw color data. This is powerful for non-photorealistic rendering (NPR) because you can use the *actual* lighting result (shadows, light falloff) as a mask for a Color Ramp.
    *   **Behavior**: It calculates the physical interaction of light (N dot L + Shadows) and outputs it.
*   **Unity (URP)**:
    *   **Limitation**: Standard URP Lit shaders do strictly physical PBR. You cannot easily access "Lighting Result" in the middle of a shader graph to drive a ramp.
    *   **Solution**: We replicated this by manually calculating the lighting term (`saturate(dot(Normal, LightDir))`) and applying the Main Light's Shadow Attenuation within the Fragment shader. This mimics the "Diffuse BSDF -> Shader to RGB" conversion manually.

### B. Coordinate Systems (`Generated` vs `Object Space`)
*   **Blender**:
    *   **Generated Coordinates**: Automatically map 0 to 1 across the **Bounding Box** of the mesh. It's stable for deforming meshes (if enabled) and convenient.
*   **Unity**:
    *   **Object Space (PositionOS)**: Raw vertex positions relative to the Pivot (0,0,0). They are measured in **Meters**, not normalized 0-1.
    *   **Discrepancy**: A noise scale of 5.0 in Blender (operating on 0-1 coords) will look vastly different in Unity (operating on meter coords) if the object is large (e.g., 2 meters tall).
    *   **Solution**: In our port, we multiplied `PositionOS` by a `Scale` factor. To be simpler, we are treating `PositionOS` as the input. If exact 0-1 normalization is needed, we would need to pass the Object Bounds to the shader (requires C# script) or bake coordinates into UVs. Currently, we use a manual Scale parameter to tune it visually.

### C. Color Space & Tone Mapping
*   **Blender**:
    *   **AgX / Filmic**: Blender defaults to Filmic or AgX tone mapping, which desaturates highlights and handles high dynamic range gracefully.
    *   **Gamma**: Operations often feel like they happen in a perceptually linear space.
*   **Unity**:
    *   **ACES / Neutral**: Unity URP typically uses ACES.
    *   **Impact**: Colors might look more saturated or "digital" in Unity compared to the soft look in Blender. The `WatercolorPaperTexture` logic replicates the HSV node, but the final "look" might need post-processing (Volume Profile) adjustments in Unity to match the exact softness of Blender's render.
