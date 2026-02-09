using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RockTools
{
    [CustomEditor(typeof(RockGenerator))]
    public class RockGeneratorEditor : Editor
    {
        private const string KPropertyPathRandomSeed = "rndSeed";
        private const bool KOptimize = true;
        private const string FallbackRockMaterialPath = "Assets/RockTools/Materials/RockSandy01.mat";
        private const int BakedTextureSize = 1024;

        private RockGenerator rockGen;
        private SerializedProperty randomSeed;
        private SerializedProperty rockType;
        private SerializedProperty material;
        private readonly LogicEditorBase[] rockEditors = new LogicEditorBase[RockTypeExtensions.RockTypesLenght];

        private int tmpRandomSeed;
        private ERockType tmpRockType;
        private Object tmpMaterial;

        private const bool AddCollider = false;

        private void OnEnable()
        {
            rockGen = target as RockGenerator;

            InitializeProperties();

            SceneView.duringSceneGui += DuringSceneGui;

            UpdateTmpValues();
        }

        private void InitializeProperties()
        {
            randomSeed = serializedObject.FindProperty("rndSeed");
            rockType = serializedObject.FindProperty("type");
            material = serializedObject.FindProperty("material");
        }

        private void UpdateTmpValues()
        {
            tmpRandomSeed = randomSeed.intValue;
            tmpRockType = (ERockType) rockType.intValue;
            tmpMaterial = material.objectReferenceValue;
            InitializeRockTypeEditors();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
            ShutDownRockTypeEditors();
        }

        public override void OnInspectorGUI()
        {
            if (rockGen == null)
            {
                return;
            }

            if (!rockGen.isActiveAndEnabled)
            {
                EditorGUILayout.HelpBox("Please enable rock generator's game object before editing!", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!rockGen.isActiveAndEnabled);

            serializedObject.Update();
            DrawProperties();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bake", EditorStyles.boldLabel);
            //addCollider = EditorGUILayout.Toggle("Add Collider", addCollider);

            if (GUILayout.Button("Bake"))
            {
                PreBake(Bake);
                GUIUtility.ExitGUI();
                return;
            }

            if (tmpRandomSeed != randomSeed.intValue)
            {
                rockGen.UpdateRock();
            }
            else if (tmpRockType != (ERockType) rockType.intValue)
            {
                rockGen.UpdateRock();
            }
            else if (tmpMaterial != material.objectReferenceValue)
            {
                rockGen.UpdateMaterials();
            }

            UpdateTmpValues();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawProperties()
        {
            DrawRandomSeedField();

            var iterator = serializedObject.GetIterator();
            var propertyToExclude = new[] {"m_Script", KPropertyPathRandomSeed, "logic"};
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!propertyToExclude.Contains(iterator.name))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            rockEditors[rockGen.type.GetTypeIndex()]?.OnInspectorGUI();
        }

        private void InitializeRockTypeEditors()
        {
            for (var i = 0; i < rockEditors.Length; i++)
            {
                if (ReferenceEquals(rockEditors[i], null))
                {
                    rockEditors[i] = CreateEditor(rockGen.logics[i]) as LogicEditorBase;
                    rockEditors[i].OnPropertyChanged += () => { rockGen.UpdateRock(); };
                }
            }
        }

        private void ShutDownRockTypeEditors()
        {
            foreach (var editor in rockEditors)
            {
                if (!ReferenceEquals(editor, null))
                {
                    DestroyImmediate(editor);
                }
            }
        }

        private void DrawRandomSeedField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Random Seed");

            if (GUILayout.Button("Randomize"))
            {
                rockGen.Randomize(1);
            }

            rockGen.rndSeed = EditorGUILayout.IntField(rockGen.rndSeed);
            EditorGUILayout.EndHorizontal();
        }

        private void DuringSceneGui(SceneView obj)
        {
            if (rockGen != null)
            {
                var vertexCount = rockGen.GetVertexCount();
                if (vertexCount >= 0)
                {
                    Handles.BeginGUI();
                    GUILayout.BeginArea(new Rect(20, 20, 300, 60));
                    GUILayout.BeginVertical("Box");
                    GUILayout.Label($"{rockGen.name}");
                    GUILayout.Label($"Vertex Count (Before Bake): {vertexCount}");
                    GUILayout.EndVertical();
                    GUILayout.EndArea();
                    Handles.EndGUI();
                }
            }
        }

        private async void PreBake(Action<string> preBakeDone)
        {
            var scenePath = SceneManager.GetActiveScene().path;

            // check if scene has a valid path
            if (string.IsNullOrEmpty(scenePath))
            {
                if (EditorUtility.DisplayDialog("The untitled scene needs saving",
                        "You need to save the scene before baking rock.", "Save Scene", "Cancel"))
                    scenePath = EditorUtility.SaveFilePanel("Save Scene", "Assets/", "", "unity");

                scenePath = FileUtil.GetProjectRelativePath(scenePath);

                if (string.IsNullOrEmpty(scenePath))
                {
                    Debug.LogWarning("Scene was not saved, bake canceled.");
                    return;
                }

                var saveOk = EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);

                if (!saveOk)
                {
                    Debug.LogWarning("Scene was not saved, bake canceled.");
                    return;
                }

                AssetDatabase.Refresh();
                await Task.Delay(100);
            }

            scenePath = SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            var assetPath = $"{Path.ChangeExtension(scenePath, null)}-generated-mesh/baked-rock.asset";
            var assetDir = Path.GetDirectoryName(assetPath);

            if (string.IsNullOrEmpty(assetDir))
            {
                return;
            }

            if (!Directory.Exists(assetDir))
            {
                Directory.CreateDirectory(assetDir);
                AssetDatabase.Refresh();
                await Task.Delay(100);
            }

            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            preBakeDone.Invoke(assetPath);
        }

        private void Bake(string path)
        {
            var bakedMeshFilter = new GameObject("Baked-Rock").AddComponent<MeshFilter>();
            var bakedMeshRenderer = bakedMeshFilter.gameObject.AddComponent<MeshRenderer>();
            bakedMeshRenderer.sharedMaterial = ResolveBakeMaterial();
            var parameters = new BakeParameters {addCollider = AddCollider, path = path, mergeVerticesThreshold = 0.1f, generateSecondaryUVSet = true, optimize = KOptimize};
            RockBaker.Bake(rockGen, parameters, bakedMeshFilter);

            if (bakedMeshFilter.sharedMesh != null && !string.IsNullOrEmpty(path))
            {
                if (RockMeshTextureBaker.GenerateUvAndTexturesForMeshAsset(
                        bakedMeshFilter.sharedMesh,
                        path,
                        out var albedoPath,
                        out var normalPath,
                        BakedTextureSize))
                {
                    var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                    var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                    ApplyGeneratedTexturesToMaterial(bakedMeshRenderer.sharedMaterial, albedo, normal);
                }
            }
        }

        private Material ResolveBakeMaterial()
        {
            var materialOnGenerator = rockGen.pMeshRenderer.sharedMaterial;
            if (IsUsableMaterial(materialOnGenerator))
            {
                return materialOnGenerator;
            }

            var fallback = AssetDatabase.LoadAssetAtPath<Material>(FallbackRockMaterialPath);
            if (IsUsableMaterial(fallback))
            {
                Debug.LogWarning($"[RockTools] Source material is missing/invalid. Using fallback: {FallbackRockMaterialPath}");
                return fallback;
            }

            fallback = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (fallback != null)
            {
                Debug.LogWarning("[RockTools] Source material is missing/invalid. Using built-in Default-Material.");
                return fallback;
            }

            return null;
        }

        private static bool IsUsableMaterial(Material material)
        {
            if (material == null || material.shader == null)
            {
                return false;
            }

            return !string.Equals(material.shader.name, "Hidden/InternalErrorShader", StringComparison.Ordinal);
        }

        private static void ApplyGeneratedTexturesToMaterial(Material material, Texture2D albedo, Texture2D normal)
        {
            if (material == null)
            {
                return;
            }

            var changed = false;
            Undo.RecordObject(material, "Apply Rock Bake Textures");

            changed |= SetTextureIfPropertyExists(material, "_MainTex", albedo);
            changed |= SetTextureIfPropertyExists(material, "_BaseMap", albedo);
            changed |= SetTextureIfPropertyExists(material, "_BumpMap", normal);
            changed |= SetTextureIfPropertyExists(material, "_Normal_Map", normal);

            if (changed)
            {
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static bool SetTextureIfPropertyExists(Material material, string propertyName, Texture texture)
        {
            if (texture == null || !material.HasProperty(propertyName))
            {
                return false;
            }

            material.SetTexture(propertyName, texture);
            return true;
        }
    }
}
