using UnityEngine;
using UnityEditor;

public class MaterialAssignerTool : EditorWindow
{
    [MenuItem("Tools/Convert to Integrated Watercolor %g")] // Ctrl+G (Windows) or Cmd+G (Mac) shortcut
    public static void ConvertToWatercolor()
    {
        // Find the new integrated watercolor shader
        Shader watercolorShader = Shader.Find("Custom/BK_StandardLayered_Watercolor");
        if (watercolorShader == null)
        {
            Debug.LogError("Could not find shader 'Custom/BK_StandardLayered_Watercolor'.");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected.");
            return;
        }

        int count = 0;
        Undo.RecordObjects(selectedObjects, "Assign Watercolor Shader");

        foreach (GameObject obj in selectedObjects)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat != null && mat.shader != watercolorShader)
                    {
                        Undo.RecordObject(mat, "Convert to Watercolor Shader");

                        // 1. Save existing properties (in case we are recovering from the simple shader)
                        Texture savedTexture = null;
                        if (mat.HasProperty("_BaseMap")) savedTexture = mat.GetTexture("_BaseMap");
                        
                        // 2. Switch Shader
                        // Since the new shader is a superset of the original, Unity preserves common properties automatically.
                        mat.shader = watercolorShader;

                        // 3. Restore/Fix properties
                        // If we came from the simple shader, _MainTex might be empty but we have _BaseMap
                        if (savedTexture != null && (!mat.HasProperty("_MainTex") || mat.GetTexture("_MainTex") == null))
                        {
                            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", savedTexture);
                        }

                        count++;
                    }
                }
            }
        }

        if (count > 0)
        {
            Debug.Log($"Converted {count} materials to Integrated Watercolor shader.");
        }
        else
        {
            Debug.Log("No materials needed conversion (or no renderers found).");
        }
    }
}
