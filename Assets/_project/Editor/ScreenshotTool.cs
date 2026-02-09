using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

public class ScreenshotTool
{
    public static void CaptureDemo()
    {
        string scenePath = "Assets/_project/Scenes/Demo.unity";
        Debug.Log($"Opening scene: {scenePath}");
        EditorSceneManager.OpenScene(scenePath);

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindFirstObjectByType<Camera>();
        }

        if (cam == null)
        {
            Debug.LogError("No camera found in scene!");
            return;
        }

        // 1. Take Screenshot
        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToPNG();
        string imgPath = Path.Combine(Directory.GetCurrentDirectory(), "Capture_Demo.png");
        File.WriteAllBytes(imgPath, bytes);
        Debug.Log($"Saved screenshot to: {imgPath}");

        // Cleanup
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(screenShot);

        // 2. Map Objects to Shaders
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Object & Shader Mapping ===");
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null)
                {
                    sb.AppendLine($"Object: {r.name} | Material: {mat.name} | Shader: {mat.shader.name}");
                }
            }
        }
        string logPath = Path.Combine(Directory.GetCurrentDirectory(), "Capture_Log.txt");
        File.WriteAllText(logPath, sb.ToString());
        Debug.Log($"Saved mapping log to: {logPath}");
    }
}
