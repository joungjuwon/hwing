using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureGenerator : EditorWindow
{
    [MenuItem("Tools/Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<TextureGenerator>("Tex Gen");
    }

    public int resolution = 512;
    public string savePath = "Assets/Textures/Watercolor";

    void OnGUI()
    {
        GUILayout.Label("Watercolor Texture Generator", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 128, 2048);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        if (GUILayout.Button("Generate Noise & Paper Textures"))
        {
            GenerateTextures();
        }
    }

    void GenerateTextures()
    {
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        // 1. Perlin Noise (for Wash/Distortion)
        Texture2D noiseTex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        Color[] noiseCols = new Color[resolution * resolution];
        float scale = 10.0f;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xCoord = (float)x / resolution * scale;
                float yCoord = (float)y / resolution * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                
                // Make it tileable (simple blend) or just use large scale
                // For simple usage, standard perlin is fine.
                
                noiseCols[y * resolution + x] = new Color(sample, sample, sample, 1);
            }
        }
        noiseTex.SetPixels(noiseCols);
        noiseTex.Apply();
        SaveTexture(noiseTex, "Watercolor_Noise.png");

        // 2. Paper Grain (High frequency noise)
        Texture2D paperTex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
        Color[] paperCols = new Color[resolution * resolution];
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float r = Random.Range(0.8f, 1.0f); // Slight grain
                // Add some "fiber" lines maybe?
                if (Random.value > 0.98f) r -= 0.2f;
                
                paperCols[y * resolution + x] = new Color(r, r, r, 1);
            }
        }
        paperTex.SetPixels(paperCols);
        paperTex.Apply();
        SaveTexture(paperTex, "Watercolor_Paper.png");
        
        AssetDatabase.Refresh();
        Debug.Log("Generated Textures in " + savePath);
    }

    void SaveTexture(Texture2D tex, string name)
    {
        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(savePath, name);
        File.WriteAllBytes(path, bytes);
        Debug.Log("Saved: " + path);
    }
}
