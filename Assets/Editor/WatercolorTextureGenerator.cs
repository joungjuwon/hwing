using UnityEngine;
using UnityEditor;
using System.IO;

public class WatercolorTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Watercolor Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<WatercolorTextureGenerator>("Watercolor Tex Gen");
    }

    public int resolution = 512;
    public float noiseScale = 8f;
    public float noiseOctaves = 4;
    public string savePath = "Assets/Textures/Watercolor";

    void OnGUI()
    {
        GUILayout.Label("Packed Texture Generator", EditorStyles.boldLabel);
        GUILayout.Label("Creates RG=Normal, B=Mask, A=Extra", EditorStyles.miniLabel);
        
        EditorGUILayout.Space();
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 128, 2048);
        noiseScale = EditorGUILayout.Slider("Noise Scale", noiseScale, 1f, 32f);
        noiseOctaves = EditorGUILayout.Slider("Noise Octaves", noiseOctaves, 1, 8);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Packed Paint Texture"))
        {
            GeneratePackedTexture();
        }
        
        if (GUILayout.Button("Generate Paper Texture"))
        {
            GeneratePaperTexture();
        }
    }

    void GeneratePackedTexture()
    {
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        // Use RGBA32 for packed data
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true, true); // linear!
        Color[] pixels = new Color[resolution * resolution];
        
        for (int y = 0; y < resolution; y++)
        {
            if (y % 64 == 0) EditorUtility.DisplayProgressBar("Generating", $"Row {y}/{resolution}", (float)y / resolution);
            
            for (int x = 0; x < resolution; x++)
            {
                float u = (float)x / resolution;
                float v = (float)y / resolution;
                
                // R, G: Normal (Perlin-based displacement)
                // Generate a height field first, then derive normal from gradients
                float h00 = FBM(u, v, noiseScale, (int)noiseOctaves);
                float h10 = FBM(u + 1f / resolution, v, noiseScale, (int)noiseOctaves);
                float h01 = FBM(u, v + 1f / resolution, noiseScale, (int)noiseOctaves);
                
                float dx = (h10 - h00) * 2f;
                float dy = (h01 - h00) * 2f;
                
                // Normal XY in -1..1, remap to 0..1
                float nx = Mathf.Clamp(dx, -1f, 1f) * 0.5f + 0.5f;
                float ny = Mathf.Clamp(dy, -1f, 1f) * 0.5f + 0.5f;
                
                // B: Mask (different noise pattern for brush strokes)
                float maskB = FBM(u * 1.3f + 100f, v * 1.3f + 100f, noiseScale * 0.7f, (int)noiseOctaves);
                
                // A: Extra (high-frequency variation)
                float extraA = FBM(u * 2.5f + 200f, v * 2.5f + 200f, noiseScale * 1.5f, 2);
                
                pixels[y * resolution + x] = new Color(nx, ny, maskB, extraA);
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        SaveTextureAsset(tex, "WatercolorPacked.png", linear: true);
        Debug.Log($"Generated Packed Texture: {savePath}/WatercolorPacked.png\n" +
                  "IMPORTANT: Set texture import to sRGB OFF!");
    }
    
    void GeneratePaperTexture()
    {
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[resolution * resolution];
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Paper grain: high-frequency noise
                float grain = Random.Range(0.85f, 1.0f);
                
                // Add occasional "fiber" marks
                if (Random.value > 0.97f) grain -= Random.Range(0.1f, 0.2f);
                
                // Add subtle low-freq variation
                float u = (float)x / resolution;
                float v = (float)y / resolution;
                float lowFreq = Mathf.PerlinNoise(u * 3f, v * 3f) * 0.1f + 0.9f;
                
                grain *= lowFreq;
                grain = Mathf.Clamp01(grain);
                
                pixels[y * resolution + x] = new Color(grain, grain, grain, 1);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        SaveTextureAsset(tex, "WatercolorPaper.png", linear: false);
        Debug.Log($"Generated Paper Texture: {savePath}/WatercolorPaper.png");
    }
    
    float FBM(float x, float y, float scale, int octaves)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = scale;
        float maxValue = 0f;
        
        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        
        return value / maxValue;
    }

    void SaveTextureAsset(Texture2D tex, string name, bool linear)
    {
        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(savePath, name);
        File.WriteAllBytes(path, bytes);
        
        AssetDatabase.Refresh();
        
        // Set import settings
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.sRGBTexture = !linear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
    }
}
