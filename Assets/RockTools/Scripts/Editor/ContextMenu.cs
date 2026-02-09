using System.IO;
using UnityEditor;
using UnityEngine;

namespace RockTools
{
    public class ContextMenu : MonoBehaviour
    {
        private const string PrepareMeshMenuPath = "Assets/Hwing/Rock Tools/Prepare Mesh For RockGenerator";
        private const string GenerateUvTexturesMenuPath = "Assets/Hwing/Rock Tools/Generate UV + Albedo+Normal Textures";

        [MenuItem(PrepareMeshMenuPath, false, 10)]
        private static void PrepareMeshForRockGenerator()
        {
            var modifiedAssets = new Object[Selection.objects.Length];
            for (var i = 0; i < Selection.objects.Length; i++)
            {
                var selectedAsset = Selection.objects[i];
                modifiedAssets[i] = PrepareAndSaveMesh(selectedAsset as Mesh);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.objects = modifiedAssets;
        }

        [MenuItem(PrepareMeshMenuPath, true)]
        private static bool ValidatePrepareMeshForRockGenerator()
        {
            return HasOnlyMeshAssetsSelected();
        }

        [MenuItem(GenerateUvTexturesMenuPath, false, 11)]
        private static void GenerateUvTexturesForSelectedMeshes()
        {
            var selectedMeshes = new System.Collections.Generic.List<Object>(Selection.objects.Length);
            var generated = 0;
            for (var i = 0; i < Selection.objects.Length; i++)
            {
                var mesh = Selection.objects[i] as Mesh;
                if (mesh == null)
                {
                    continue;
                }

                var meshPath = AssetDatabase.GetAssetPath(mesh);
                if (RockMeshTextureBaker.GenerateUvAndTexturesForMeshAsset(mesh, meshPath, out var albedoPath, out var normalPath))
                {
                    Debug.Log($"[RockTools] Generated textures for {mesh.name}\n- {albedoPath}\n- {normalPath}");
                    selectedMeshes.Add(mesh);
                    generated++;
                }
            }

            if (generated > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.objects = selectedMeshes.ToArray();
            }

            EditorUtility.DisplayDialog("RockTools", $"Generated UV textures for {generated} mesh asset(s).", "OK");
        }

        [MenuItem(GenerateUvTexturesMenuPath, true)]
        private static bool ValidateGenerateUvTexturesForSelectedMeshes()
        {
            return HasOnlyMeshAssetsSelected();
        }

        private static bool HasOnlyMeshAssetsSelected()
        {
            if (Selection.objects == null || Selection.objects.Length == 0)
            {
                return false;
            }

            foreach (var selectedAsset in Selection.objects)
            {
                if (selectedAsset == null)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selectedAsset)))
                {
                    return false;
                }

                if (!(selectedAsset is Mesh))
                {
                    return false;
                }
            }

            return true;
        }

        private static Mesh PrepareAndSaveMesh(Mesh selectedMesh)
        {
            var modifiedMesh = new Mesh()
            {
                name = selectedMesh.name,
                vertices = selectedMesh.vertices,
                triangles = selectedMesh.triangles,
                normals = selectedMesh.normals,
                colors = selectedMesh.colors,
                colors32 = selectedMesh.colors32,
                uv = selectedMesh.uv,
                uv2 = selectedMesh.uv2,
                uv3 = selectedMesh.uv3,
                uv4 = selectedMesh.uv4,
                uv5 = selectedMesh.uv5,
                uv6 = selectedMesh.uv6,
                uv7 = selectedMesh.uv7,
                uv8 = selectedMesh.uv8,
                bindposes = selectedMesh.bindposes,
                bounds = selectedMesh.bounds,
                tangents = selectedMesh.tangents,
                boneWeights = selectedMesh.boneWeights,
                hideFlags = selectedMesh.hideFlags,
                indexFormat = selectedMesh.indexFormat,
                subMeshCount = selectedMesh.subMeshCount,
            };

            var verticesLength = selectedMesh.vertices.Length;
            var colors = new Color[verticesLength];
            var minY = selectedMesh.bounds.min.y;
            var maxY = selectedMesh.bounds.max.y;
            for (var i = 0; i < verticesLength; i++)
            {
                var f = Mathf.InverseLerp(minY, maxY, selectedMesh.vertices[i].y);
                colors[i] = Color.Lerp(Color.black, Color.white, f);
            }

            modifiedMesh.colors = colors;
            modifiedMesh.RecalculateBounds();
            modifiedMesh.RecalculateNormals();

            var originalPath = AssetDatabase.GetAssetPath(selectedMesh);
            var newPath = Path.ChangeExtension(originalPath, null);
            newPath = Path.ChangeExtension($"{newPath}-{modifiedMesh.name}", "mesh");
            newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

            AssetDatabase.CreateAsset(modifiedMesh, newPath);

            return modifiedMesh;
        }
    }
}
