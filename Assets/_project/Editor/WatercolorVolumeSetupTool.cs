using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class WatercolorVolumeSetupTool
{
    private const string DefaultFolder = "Assets/_project/Rendering/Volumes";
    private const string DefaultProfileName = "Watercolor_FilmicVeryHigh_Profile.asset";
    private const string DefaultVolumeObjectName = "Global Volume (Watercolor Filmic)";

    [MenuItem("Tools/Watercolor/Setup Global Volume (Filmic)")]
    public static void SetupGlobalVolumeFilmic()
    {
        EnsureFolder(DefaultFolder);

        // 1) Create or load profile asset
        string profilePath = AssetDatabase.GenerateUniqueAssetPath($"{DefaultFolder}/{DefaultProfileName}");
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Tonemapping (ACES)
        var tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        // Color Adjustments (contrast/sat/exposure)
        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.overrideState = true;
        color.postExposure.value = 0.0f;

        color.contrast.overrideState = true;
        color.contrast.value = 60.0f; // "Very High Contrast" starting point

        color.saturation.overrideState = true;
        color.saturation.value = 0.0f;

        // Optional: lift/gamma/gain for shadow floor control (disabled by default)
        // var lgg = profile.Add<LiftGammaGain>(false);

        AssetDatabase.CreateAsset(profile, profilePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2) Create volume object in current scene
        var go = new GameObject(DefaultVolumeObjectName);
        Undo.RegisterCreatedObjectUndo(go, "Create Global Volume (Watercolor Filmic)");

        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.weight = 1f;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);

        // 3) Add a VolumeTrigger + layer setup is not required for Global Volume

        // 4) Ping created asset/object
        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);

        // Mark scene dirty
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        // Hint about URP settings we cannot safely change here
        Debug.Log("[Watercolor] Global Volume created with ACES Tonemapping + Color Adjustments. Ensure URP Pipeline Asset has HDR enabled and Color Grading Mode set to High for best results.");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        // Create nested folders
        var parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
