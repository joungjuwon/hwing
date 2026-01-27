using UnityEngine;
using UnityEditor;
using System.IO;

public class SDFBaker : EditorWindow
{
    [MenuItem("Tools/SDF Baker")]
    public static void ShowWindow()
    {
        GetWindow<SDFBaker>("SDF Baker");
    }

    public GameObject targetObject;
    public int resolution = 64;
    public float padding = 0.2f;
    public string savePath = "Assets/Textures/SDF";

    void OnGUI()
    {
        GUILayout.Label("SDF Baker (Mesh to Texture3D)", EditorStyles.boldLabel);

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 16, 128);
        padding = EditorGUILayout.FloatField("Bounds Padding", padding);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        if (GUILayout.Button("Bake SDF"))
        {
            if (targetObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Target Object.", "OK");
                return;
            }
            BakeSDF();
        }
    }

    void BakeSDF()
    {
        MeshFilter mf = targetObject.GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError("Target object must have a MeshFilter.");
            return;
        }

        // Ensure MeshCollider exists for Physics queries
        MeshCollider mc = targetObject.GetComponent<MeshCollider>();
        bool createdCollider = false;
        if (mc == null)
        {
            mc = targetObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            createdCollider = true;
        }

        try
        {
            Bounds bounds = mf.sharedMesh.bounds;
            bounds.Expand(padding);

            int w = resolution;
            int h = resolution;
            int d = resolution;

            Color[] colors = new Color[w * h * d];
            float maxDist = bounds.size.magnitude; // Normalize factor

            Vector3 min = bounds.min;
            Vector3 size = bounds.size;

            // Simple progress bar
            for (int z = 0; z < d; z++)
            {
                if (z % 5 == 0) EditorUtility.DisplayProgressBar("Baking SDF", $"Slice {z}/{d}", (float)z / d);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        // Normalized coord 0..1
                        float u = x / (float)(w - 1);
                        float v = y / (float)(h - 1);
                        float w_coord = z / (float)(d - 1);

                        // Local position
                        Vector3 localPos = min + Vector3.Scale(size, new Vector3(u, v, w_coord));
                        
                        // Transform to World for Physics query (Physics works in World Space)
                        Vector3 worldPos = targetObject.transform.TransformPoint(localPos);

                        // Find closest point on surface
                        Vector3 closestPoint = mc.ClosestPoint(worldPos);
                        
                        // Distance
                        float dist = Vector3.Distance(worldPos, closestPoint);
                        
                        // Sign check: simple heuristic. 
                        // Physics.ClosestPoint returns point on surface.
                        // If we are inside, ClosestPoint works too.
                        // But Physics.CheckSphere/Overlap can detect "Inside" if convex?
                        // For general meshes, exact sign is hard with just Collider.
                        // For "Occlusion" (Ambient), we mostly care about "Outside" distance.
                        // Let's assume unsigned distance is fine for now, or use a simple inside check if possible.
                        // (Backface raycast is robust but slow).
                        // For this shader, we assume Positive = Outside.
                        
                        // Normalize distance for storage (0..1)
                        // Let's map 0..MaxBounds to 0..1. 
                        // Or better: store raw distance in R16Float if possible.
                        // Unity Texture3D creation:
                        
                        colors[x + y * w + z * w * h] = new Color(dist, 0, 0, 1);
                    }
                }
            }

            // Create Texture3D
            Texture3D tex = new Texture3D(w, h, d, TextureFormat.RFloat, false);
            tex.SetPixels(colors);
            tex.Apply();

            // Save Asset
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            string fileName = $"{targetObject.name}_SDF.asset";
            string fullPath = Path.Combine(savePath, fileName);
            
            AssetDatabase.CreateAsset(tex, fullPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"SDF Baked: {fullPath}");
            Debug.Log($"Bounds Min: {bounds.min}, Max: {bounds.max}");
            
            // Ping the object to show user
            EditorGUIUtility.PingObject(tex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (createdCollider) DestroyImmediate(mc);
        }
    }
}
