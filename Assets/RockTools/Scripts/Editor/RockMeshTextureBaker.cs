using System.IO;
using UnityEditor;
using UnityEngine;

namespace RockTools
{
    internal static class RockMeshTextureBaker
    {
        private const int DefaultTextureSize = 1024;
        private const int DilationIterations = 3;

        public static bool GenerateUvAndTexturesForMeshAsset(
            Mesh mesh,
            string meshPath,
            out string albedoPath,
            out string normalPath,
            int textureSize = DefaultTextureSize)
        {
            albedoPath = string.Empty;
            normalPath = string.Empty;

            if (mesh == null || string.IsNullOrEmpty(meshPath))
            {
                return false;
            }

            EnsureMeshChannels(mesh);

            if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
            {
                Debug.LogWarning($"[RockTools] UV generation failed for {mesh.name}.");
                return false;
            }

            var safeSize = Mathf.Clamp(textureSize, 64, 4096);
            var albedoTex = BakeVertexColorTexture(mesh, safeSize);
            var normalTex = CreateNormalFromHeight(albedoTex, safeSize, 4.0f);

            var folder = Path.GetDirectoryName(meshPath)?.Replace('\\', '/') ?? "Assets";
            var name = Path.GetFileNameWithoutExtension(meshPath);
            albedoPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}_UVAlbedo.png");
            normalPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}_UVNormal.png");

            File.WriteAllBytes(albedoPath, albedoTex.EncodeToPNG());
            File.WriteAllBytes(normalPath, normalTex.EncodeToPNG());

            Object.DestroyImmediate(albedoTex);
            Object.DestroyImmediate(normalTex);

            AssetDatabase.ImportAsset(albedoPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(normalPath, ImportAssetOptions.ForceUpdate);

            ConfigureAlbedoImporter(albedoPath);
            ConfigureNormalImporter(normalPath);

            return true;
        }

        private static void EnsureMeshChannels(Mesh mesh)
        {
            var dirty = false;

            if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
            {
                Unwrapping.GenerateSecondaryUVSet(mesh);
                if (mesh.uv2 != null && mesh.uv2.Length == mesh.vertexCount)
                {
                    mesh.uv = mesh.uv2;
                    dirty = true;
                }
            }

            if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount)
            {
                mesh.RecalculateNormals();
                dirty = true;
            }

            if (mesh.tangents == null || mesh.tangents.Length != mesh.vertexCount)
            {
                mesh.RecalculateTangents();
                dirty = true;
            }

            if (dirty)
            {
                EditorUtility.SetDirty(mesh);
            }
        }

        private static Texture2D BakeVertexColorTexture(Mesh mesh, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            var filled = new bool[size * size];

            var uvs = mesh.uv;
            var colors = mesh.colors;
            var triangles = mesh.triangles;

            if (colors == null || colors.Length != mesh.vertexCount)
            {
                colors = new Color[mesh.vertexCount];
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i] = Color.white;
                }
            }

            for (var i = 0; i < triangles.Length; i += 3)
            {
                var i0 = triangles[i];
                var i1 = triangles[i + 1];
                var i2 = triangles[i + 2];

                RasterizeTriangle(
                    uvs[i0], uvs[i1], uvs[i2],
                    colors[i0], colors[i1], colors[i2],
                    size, pixels, filled);
            }

            DilateUnfilledPixels(pixels, filled, size, DilationIterations);

            for (var i = 0; i < pixels.Length; i++)
            {
                if (!filled[i])
                {
                    pixels[i] = new Color32(128, 128, 128, 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D CreateNormalFromHeight(Texture2D source, int size, float strength)
        {
            var src = source.GetPixels32();
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var yDown = Mathf.Max(y - 1, 0);
                var yUp = Mathf.Min(y + 1, size - 1);
                for (var x = 0; x < size; x++)
                {
                    var xLeft = Mathf.Max(x - 1, 0);
                    var xRight = Mathf.Min(x + 1, size - 1);

                    var hL = Luma(src[(y * size) + xLeft]);
                    var hR = Luma(src[(y * size) + xRight]);
                    var hD = Luma(src[(yDown * size) + x]);
                    var hU = Luma(src[(yUp * size) + x]);

                    var dx = (hR - hL) * strength;
                    var dy = (hU - hD) * strength;

                    var n = new Vector3(-dx, -dy, 1f).normalized;
                    var encoded = n * 0.5f + Vector3.one * 0.5f;

                    pixels[(y * size) + x] = new Color(
                        encoded.x,
                        encoded.y,
                        encoded.z,
                        1f);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static float Luma(Color32 color)
        {
            return ((0.299f * color.r) + (0.587f * color.g) + (0.114f * color.b)) / 255f;
        }

        private static void RasterizeTriangle(
            Vector2 uv0,
            Vector2 uv1,
            Vector2 uv2,
            Color c0,
            Color c1,
            Color c2,
            int size,
            Color32[] pixels,
            bool[] filled)
        {
            var p0 = new Vector2(uv0.x * (size - 1), uv0.y * (size - 1));
            var p1 = new Vector2(uv1.x * (size - 1), uv1.y * (size - 1));
            var p2 = new Vector2(uv2.x * (size - 1), uv2.y * (size - 1));

            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x))), 0, size - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x))), 0, size - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y))), 0, size - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y))), 0, size - 1);

            var denom = ((p1.y - p2.y) * (p0.x - p2.x)) + ((p2.x - p1.x) * (p0.y - p2.y));
            if (Mathf.Abs(denom) < 1e-8f)
            {
                return;
            }

            const float epsilon = -0.0005f;
            for (var y = minY; y <= maxY; y++)
            {
                var py = y + 0.5f;
                for (var x = minX; x <= maxX; x++)
                {
                    var px = x + 0.5f;

                    var w0 = (((p1.y - p2.y) * (px - p2.x)) + ((p2.x - p1.x) * (py - p2.y))) / denom;
                    var w1 = (((p2.y - p0.y) * (px - p2.x)) + ((p0.x - p2.x) * (py - p2.y))) / denom;
                    var w2 = 1f - w0 - w1;

                    if (w0 < epsilon || w1 < epsilon || w2 < epsilon)
                    {
                        continue;
                    }

                    var idx = y * size + x;
                    var color = (c0 * w0) + (c1 * w1) + (c2 * w2);
                    var newColor = (Color32)color;

                    if (filled[idx])
                    {
                        var old = pixels[idx];
                        pixels[idx] = new Color32(
                            (byte)((old.r + newColor.r) >> 1),
                            (byte)((old.g + newColor.g) >> 1),
                            (byte)((old.b + newColor.b) >> 1),
                            255);
                    }
                    else
                    {
                        pixels[idx] = newColor;
                        filled[idx] = true;
                    }
                }
            }
        }

        private static void DilateUnfilledPixels(Color32[] pixels, bool[] filled, int size, int iterations)
        {
            var offsets = new[]
            {
                new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
                new Vector2Int(-1, 0),                            new Vector2Int(1, 0),
                new Vector2Int(-1, 1),  new Vector2Int(0, 1),  new Vector2Int(1, 1)
            };

            var currentPixels = pixels;
            var currentFilled = filled;

            for (var iter = 0; iter < iterations; iter++)
            {
                var nextPixels = (Color32[])currentPixels.Clone();
                var nextFilled = (bool[])currentFilled.Clone();

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var idx = y * size + x;
                        if (currentFilled[idx])
                        {
                            continue;
                        }

                        var sumR = 0;
                        var sumG = 0;
                        var sumB = 0;
                        var count = 0;
                        for (var i = 0; i < offsets.Length; i++)
                        {
                            var nx = x + offsets[i].x;
                            var ny = y + offsets[i].y;
                            if (nx < 0 || ny < 0 || nx >= size || ny >= size)
                            {
                                continue;
                            }

                            var nIdx = ny * size + nx;
                            if (!currentFilled[nIdx])
                            {
                                continue;
                            }

                            var c = currentPixels[nIdx];
                            sumR += c.r;
                            sumG += c.g;
                            sumB += c.b;
                            count++;
                        }

                        if (count <= 0)
                        {
                            continue;
                        }

                        nextPixels[idx] = new Color32(
                            (byte)(sumR / count),
                            (byte)(sumG / count),
                            (byte)(sumB / count),
                            255);
                        nextFilled[idx] = true;
                    }
                }

                currentPixels = nextPixels;
                currentFilled = nextFilled;
            }

            if (!ReferenceEquals(currentPixels, pixels))
            {
                currentPixels.CopyTo(pixels, 0);
                currentFilled.CopyTo(filled, 0);
            }
        }

        private static void ConfigureAlbedoImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureNormalImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
