using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Generates Substance Designer-style Shape Splatter textures.
/// Replicates the Splatter workflow: scatter brush strokes with random position, rotation, and gray values.
/// </summary>
public class ShapeSplatterGenerator : EditorWindow
{
    // Input
    private Texture2D sourceStroke;
    
    // Output settings
    private int outputSize = 2048;
    private string outputPath = "Assets/Shaders/StylizedPaint/GeneratedTextures";
    private string outputName = "ScatteredStrokes";
    
    // Splatter settings
    private int strokeCount = 500;
    private float minScale = 0.05f;
    private float maxScale = 0.15f;
    private float rotationRange = 180f;
    private float minGrayValue = 0.3f;
    private float maxGrayValue = 1.0f;
    private float strokeAspectRatio = 3f; // Width to height ratio
    
    // Seamless tiling
    private bool seamlessTiling = true;
    private int edgeWrap = 2; // Number of tile wraps to check
    
    // Preview
    private Texture2D previewTexture;
    
    [MenuItem("Tools/Shape Splatter Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<ShapeSplatterGenerator>("Shape Splatter");
        window.minSize = new Vector2(400, 600);
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Shape Splatter Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Generates scattered stroke textures like Substance Designer's Shape Splatter node.", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Input
        EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
        sourceStroke = (Texture2D)EditorGUILayout.ObjectField("Source Stroke", sourceStroke, typeof(Texture2D), false);
        
        EditorGUILayout.Space(10);
        
        // Output
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputSize = EditorGUILayout.IntPopup("Output Size", outputSize, 
            new[] { "512", "1024", "2048", "4096" }, 
            new[] { 512, 1024, 2048, 4096 });
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        outputName = EditorGUILayout.TextField("Output Name", outputName);
        
        EditorGUILayout.Space(10);
        
        // Splatter settings
        EditorGUILayout.LabelField("Splatter Settings", EditorStyles.boldLabel);
        strokeCount = EditorGUILayout.IntSlider("Stroke Count", strokeCount, 50, 2000);
        
        EditorGUILayout.MinMaxSlider("Scale Range", ref minScale, ref maxScale, 0.01f, 0.5f);
        EditorGUILayout.LabelField($"  Scale: {minScale:F3} - {maxScale:F3}");
        
        rotationRange = EditorGUILayout.Slider("Rotation Range (°)", rotationRange, 0f, 180f);
        strokeAspectRatio = EditorGUILayout.Slider("Stroke Aspect Ratio", strokeAspectRatio, 0.5f, 5f);
        
        EditorGUILayout.MinMaxSlider("Gray Value Range", ref minGrayValue, ref maxGrayValue, 0f, 1f);
        EditorGUILayout.LabelField($"  Gray: {minGrayValue:F2} - {maxGrayValue:F2}");
        
        EditorGUILayout.Space(5);
        seamlessTiling = EditorGUILayout.Toggle("Seamless Tiling", seamlessTiling);
        if (seamlessTiling)
        {
            edgeWrap = EditorGUILayout.IntSlider("Edge Wrap Count", edgeWrap, 1, 3);
        }
        
        EditorGUILayout.Space(15);
        
        // Buttons
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = sourceStroke != null;
        if (GUILayout.Button("Preview", GUILayout.Height(30)))
        {
            GeneratePreview();
        }
        
        if (GUILayout.Button("Generate & Save", GUILayout.Height(30)))
        {
            GenerateAndSave();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        // Preview display
        if (previewTexture != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            
            float previewHeight = EditorGUIUtility.currentViewWidth - 40;
            Rect rect = GUILayoutUtility.GetRect(previewHeight, previewHeight);
            EditorGUI.DrawPreviewTexture(rect, previewTexture, null, ScaleMode.ScaleToFit);
        }
    }
    
    private void GeneratePreview()
    {
        previewTexture = GenerateSplatterTexture(512); // Lower res for preview
        Repaint();
    }
    
    private void GenerateAndSave()
    {
        Texture2D result = GenerateSplatterTexture(outputSize);
        
        // Ensure directory exists
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        // Save as PNG
        string fullPath = Path.Combine(outputPath, outputName + ".png");
        byte[] pngData = result.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);
        
        // Cleanup
        if (result != previewTexture)
        {
            DestroyImmediate(result);
        }
        
        AssetDatabase.Refresh();
        
        // Select the created asset
        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        
        Debug.Log($"Shape Splatter texture saved to: {fullPath}");
    }
    
    private Texture2D GenerateSplatterTexture(int size)
    {
        // Create output texture
        Texture2D output = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        // Initialize with black
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.black;
        }
        
        // Get source stroke as readable texture
        Texture2D readableSource = GetReadableTexture(sourceStroke);
        if (readableSource == null)
        {
            Debug.LogError("Could not read source stroke texture. Make sure it's readable.");
            return output;
        }
        
        // Generate random stroke instances
        System.Random rand = new System.Random();
        
        for (int i = 0; i < strokeCount; i++)
        {
            // Random properties
            float posX = (float)rand.NextDouble();
            float posY = (float)rand.NextDouble();
            float rotation = ((float)rand.NextDouble() * 2f - 1f) * rotationRange * Mathf.Deg2Rad;
            float scale = Mathf.Lerp(minScale, maxScale, (float)rand.NextDouble());
            float grayValue = Mathf.Lerp(minGrayValue, maxGrayValue, (float)rand.NextDouble());
            
            // Render stroke at all tile positions for seamless
            int wrapCount = seamlessTiling ? edgeWrap : 0;
            
            for (int wy = -wrapCount; wy <= wrapCount; wy++)
            {
                for (int wx = -wrapCount; wx <= wrapCount; wx++)
                {
                    float wrappedX = posX + wx;
                    float wrappedY = posY + wy;
                    
                    RenderStroke(
                        pixels, size,
                        readableSource,
                        wrappedX, wrappedY,
                        rotation, scale, strokeAspectRatio,
                        grayValue
                    );
                }
            }
        }
        
        // Apply pixels
        output.SetPixels(pixels);
        output.Apply();
        
        // Cleanup
        if (readableSource != sourceStroke)
        {
            DestroyImmediate(readableSource);
        }
        
        return output;
    }
    
    private void RenderStroke(
        Color[] pixels, int size,
        Texture2D source,
        float posX, float posY,
        float rotation, float scale, float aspectRatio,
        float grayValue)
    {
        float cos = Mathf.Cos(rotation);
        float sin = Mathf.Sin(rotation);
        
        // Calculate stroke bounds in output texture
        float strokeWidth = scale * aspectRatio;
        float strokeHeight = scale;
        float maxExtent = Mathf.Max(strokeWidth, strokeHeight) * 1.5f;
        
        int minX = Mathf.FloorToInt((posX - maxExtent) * size);
        int maxX = Mathf.CeilToInt((posX + maxExtent) * size);
        int minY = Mathf.FloorToInt((posY - maxExtent) * size);
        int maxY = Mathf.CeilToInt((posY + maxExtent) * size);
        
        // Clamp to texture bounds
        minX = Mathf.Clamp(minX, 0, size - 1);
        maxX = Mathf.Clamp(maxX, 0, size - 1);
        minY = Mathf.Clamp(minY, 0, size - 1);
        maxY = Mathf.Clamp(maxY, 0, size - 1);
        
        int srcWidth = source.width;
        int srcHeight = source.height;
        Color[] srcPixels = source.GetPixels();
        
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // Normalized position
                float nx = (float)x / size;
                float ny = (float)y / size;
                
                // Position relative to stroke center
                float dx = nx - posX;
                float dy = ny - posY;
                
                // Apply inverse rotation
                float localX = dx * cos + dy * sin;
                float localY = -dx * sin + dy * cos;
                
                // Apply inverse scale
                localX /= strokeWidth;
                localY /= strokeHeight;
                
                // Map to source UV (centered)
                float srcU = localX + 0.5f;
                float srcV = localY + 0.5f;
                
                // Check bounds
                if (srcU < 0f || srcU > 1f || srcV < 0f || srcV > 1f)
                    continue;
                
                // Sample source texture
                int srcX = Mathf.Clamp((int)(srcU * srcWidth), 0, srcWidth - 1);
                int srcY = Mathf.Clamp((int)(srcV * srcHeight), 0, srcHeight - 1);
                Color srcColor = srcPixels[srcY * srcWidth + srcX];
                
                // Calculate luminance and apply gray value
                float lum = srcColor.r * 0.299f + srcColor.g * 0.587f + srcColor.b * 0.114f;
                float alpha = srcColor.a;
                
                // Use max blend to overlap strokes
                int idx = y * size + x;
                float newValue = lum * grayValue * alpha;
                float oldValue = pixels[idx].r;
                
                // Max blend
                float blendedValue = Mathf.Max(oldValue, newValue);
                pixels[idx] = new Color(blendedValue, blendedValue, blendedValue, 1f);
            }
        }
    }
    
    private Texture2D GetReadableTexture(Texture2D source)
    {
        if (source == null) return null;
        
        // Check if already readable
        try
        {
            source.GetPixels(0, 0, 1, 1);
            return source;
        }
        catch
        {
            // Create readable copy
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
            Graphics.Blit(source, rt);
            
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            
            return readable;
        }
    }
    
    private void OnDestroy()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
        }
    }
}
