using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    public static class TreeMeshBuilder
    {
        public struct Segment
        {
            public Vector3 start;
            public Vector3 end;
            public float startRadius;
            public float endRadius;
        }

        public static Mesh BuildTubeMesh(IList<Vector3> positions, IList<float> radii, IList<int> parents, int radialSegments)
        {
            var mesh = new Mesh();
            if (positions == null || radii == null || parents == null) return mesh;
            int nodeCount = positions.Count;
            if (nodeCount == 0 || radii.Count != nodeCount || parents.Count != nodeCount) return mesh;

            radialSegments = Mathf.Max(3, radialSegments);

            var vertices = new List<Vector3>(nodeCount * radialSegments);
            var normals = new List<Vector3>(nodeCount * radialSegments);
            var triangles = new List<int>(nodeCount * radialSegments * 6);

            var forwards = new Vector3[nodeCount];
            var normalsRing = new Vector3[nodeCount];
            var binormalsRing = new Vector3[nodeCount];
            var childCounts = new int[nodeCount];

            for (int i = 1; i < nodeCount; i++)
            {
                int p = parents[i];
                if (p >= 0 && p < nodeCount)
                {
                    childCounts[p]++;
                }
            }

            for (int i = 0; i < nodeCount; i++)
            {
                Vector3 forward;
                if (parents[i] >= 0 && parents[i] < nodeCount)
                {
                    forward = (positions[i] - positions[parents[i]]);
                }
                else if (childCounts[i] > 0)
                {
                    Vector3 avg = Vector3.zero;
                    for (int c = 0; c < nodeCount; c++)
                    {
                        if (parents[c] == i)
                        {
                            avg += (positions[c] - positions[i]);
                        }
                    }
                    forward = avg;
                }
                else
                {
                    forward = Vector3.up;
                }

                if (forward.sqrMagnitude < 0.0001f) forward = Vector3.up;
                forwards[i] = forward.normalized;
            }

            for (int i = 0; i < nodeCount; i++)
            {
                if (parents[i] < 0)
                {
                    Vector3 up = Vector3.up;
                    if (Mathf.Abs(Vector3.Dot(up, forwards[i])) > 0.9f) up = Vector3.right;
                    Vector3 n = Vector3.Cross(forwards[i], up).normalized;
                    Vector3 b = Vector3.Cross(forwards[i], n).normalized;
                    normalsRing[i] = n;
                    binormalsRing[i] = b;
                }
                else
                {
                    int p = parents[i];
                    Vector3 prevN = normalsRing[p];
                    Vector3 f = forwards[i];
                    Vector3 n = prevN - f * Vector3.Dot(prevN, f);
                    if (n.sqrMagnitude < 0.0001f)
                    {
                        Vector3 up = Vector3.up;
                        if (Mathf.Abs(Vector3.Dot(up, f)) > 0.9f) up = Vector3.right;
                        n = Vector3.Cross(f, up).normalized;
                    }
                    else
                    {
                        n.Normalize();
                    }
                    Vector3 b = Vector3.Cross(f, n).normalized;
                    normalsRing[i] = n;
                    binormalsRing[i] = b;
                }
            }

            for (int i = 0; i < nodeCount; i++)
            {
                float r = Mathf.Max(0.0001f, radii[i]);
                Vector3 n = normalsRing[i];
                Vector3 b = binormalsRing[i];

                for (int s = 0; s < radialSegments; s++)
                {
                    float angle = (Mathf.PI * 2f) * (s / (float)radialSegments);
                    Vector3 dir = (n * Mathf.Cos(angle)) + (b * Mathf.Sin(angle));
                    vertices.Add(positions[i] + dir * r);
                    normals.Add(dir);
                }
            }

            for (int i = 1; i < nodeCount; i++)
            {
                int p = parents[i];
                if (p < 0 || p >= nodeCount) continue;

                int parentStart = p * radialSegments;
                int childStart = i * radialSegments;

                for (int s = 0; s < radialSegments; s++)
                {
                    int s0 = s;
                    int s1 = (s + 1) % radialSegments;

                    int p0 = parentStart + s0;
                    int p1 = parentStart + s1;
                    int c0 = childStart + s0;
                    int c1 = childStart + s1;

                    triangles.Add(p0);
                    triangles.Add(c0);
                    triangles.Add(p1);

                    triangles.Add(p1);
                    triangles.Add(c0);
                    triangles.Add(c1);
                }
            }

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh BuildBarkMesh(List<Segment> segments, int radialSegments)
        {
            if (segments == null || segments.Count == 0)
            {
                return new Mesh();
            }

            radialSegments = Mathf.Max(3, radialSegments);
            var vertices = new List<Vector3>(segments.Count * radialSegments * 2);
            var normals = new List<Vector3>(segments.Count * radialSegments * 2);
            var triangles = new List<int>(segments.Count * radialSegments * 6);

            for (int s = 0; s < segments.Count; s++)
            {
                AddCylinder(segments[s], radialSegments, vertices, normals, triangles);
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh BuildLineMesh(List<Segment> segments)
        {
            var mesh = new Mesh();
            if (segments == null || segments.Count == 0) return mesh;

            var vertices = new Vector3[segments.Count * 2];
            var indices = new int[segments.Count * 2];

            for (int i = 0; i < segments.Count; i++)
            {
                int v = i * 2;
                vertices[v] = segments[i].start;
                vertices[v + 1] = segments[i].end;
                indices[v] = v;
                indices[v + 1] = v + 1;
            }

            mesh.SetVertices(vertices);
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCylinder(
            Segment seg,
            int radialSegments,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> triangles)
        {
            Vector3 axis = seg.end - seg.start;
            float length = axis.magnitude;
            if (length <= 0.0001f) return;
            axis /= length;

            Vector3 tangent = Vector3.Cross(axis, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector3.Cross(axis, Vector3.right);
            }
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(axis, tangent);

            int baseIndex = vertices.Count;

            for (int i = 0; i < radialSegments; i++)
            {
                float angle = (Mathf.PI * 2f) * (i / (float)radialSegments);
                Vector3 circleDir = (tangent * Mathf.Cos(angle)) + (bitangent * Mathf.Sin(angle));

                Vector3 v0 = seg.start + circleDir * seg.startRadius;
                Vector3 v1 = seg.end + circleDir * seg.endRadius;

                vertices.Add(v0);
                vertices.Add(v1);
                normals.Add(circleDir);
                normals.Add(circleDir);
            }

            for (int i = 0; i < radialSegments; i++)
            {
                int i0 = baseIndex + (i * 2);
                int i1 = baseIndex + (i * 2) + 1;
                int i2 = baseIndex + (((i + 1) % radialSegments) * 2);
                int i3 = baseIndex + (((i + 1) % radialSegments) * 2) + 1;

                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);

                triangles.Add(i2);
                triangles.Add(i3);
                triangles.Add(i1);
            }
        }
    }
}
