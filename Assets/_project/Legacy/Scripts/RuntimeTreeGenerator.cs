using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralTreeGeneratorByMysticForge
{
    public class RuntimeTreeGenerator : MonoBehaviour
    {
        [Header("General Settings")]
        public int seed = 0;
        public bool autoUpdate = false;

        [Header("Trunk Settings")]
        public float trunkHeight = 4.1f;
        public float trunkRadius = 0.1f;
        public float trunkRadiusCurvature = 0.8f;
        public float trunkRadiusNoise = 0.5f;
        public int trunkSubdivision = 0;
        public float trunkCrinkliness = 0f;
        public int trunkSegments = 4;
        public float trunkBending = 0.02f;
        public bool includeStump = true;
        public float treeStumpStartPoint = 0.1f;
        public float treeStumpWidth = 2f;
        public Material trunkMaterial;

        [Header("Branch Settings")]
        public int numberOfBranches = 17;
        public float branchHeightMin = 0.19f;
        public float branchHeightMax = 0.94f;
        public float branchRadius = 0.08f;
        public float branchLength = 2.99f;
        public float branchRadiusCurvature = 0.95f;
        public float branchRadiusNoise = 0.96f;
        public int branchSubdivision = 0;
        public float branchCrinkliness = 0f;
        public int branchSegments = 4;
        public float branchBending = 0.15f;
        public float branchAngle = -68.5f;
        public bool adjustBranchLengthByHeight = true;
        public bool angleAdjustmentByHeight = false;
        public float gravity = 0.13f;

        [Header("Branchlet Settings")]
        public int numberOfBranchlets = 40;
        public float branchletHeightMin = 0.2f;
        public float branchletHeightMax = 0.94f;
        public float branchletRadius = 0.2f;
        public float branchletLength = 0.76f;
        public float branchletRadiusCurvature = 0.92f;
        public float branchletRadiusNoise = 0.29f;
        public int branchletSubdivision = 0;
        public float branchletCrinkliness = 0f;
        public int branchletSegments = 3;
        public float branchletBending = 0.14f;
        public float branchletAngle = 53.1f;
        public float branchletForwardAngle = -50.5f;
        public float gravityBranchlets = 0.15f;
        public bool adjustBranchletLengthByHeight = true;

        [Header("Leaf Settings")]
        public Material leafMaterial;
        public GameObject leafPrefab; // We need the mesh from this
        
        [Header("Leaf - Branch")]
        public int numberOfLeaves = 21;
        public float leafSize = 1.37f;
        public float leafPositionMin = 0.83f;
        public float leafPositionMax = 1f;
        public float leafForwardRotation = 0f;
        public float leafRotation = 0f;
        public float leafRandomizeRotation = 0.47f;
        public Vector3 leafBranchPositioning = Vector3.zero;
        public float leafBranchRandomPositioning = 0f;
        public Vector3 leafBranchSizeV3 = new Vector3(1f, 1f, 1f);
        public float leafSizeBranchRandom = 0.32f;

        [Header("Leaf - Branchlet")]
        public int numberOfLeavesBranchlet = 15;
        public float leafBranchletSize = 1.5f;
        public float leafBranchletPositionMin = 0.27f;
        public float leafBranchletPositionMax = 1f;
        public float leafBranchletForwardRotation = 0f;
        public float leafBranchletRotation = 14.8f;
        public float leafBranchletRandomizeRotation = 0.2f;
        public Vector3 leafBranchletPositioning = Vector3.zero;
        public float leafBranchletRandomPositioning = 0f;
        public Vector3 leafBranchletSizeV3 = new Vector3(1f, 1f, 1f);
        public float leafSizeBranchletRandom = 0.59f;

        [Header("Leaf - Trunk")]
        public int numberOfLeavesTrunk = 14;
        public float leafTrunkSize = 2.3f;
        public float leafTrunkPositionMin = 0.97f;
        public float leafTrunkPositionMax = 1f;
        public float leafTrunkForwardRotation = 0f;
        public float leafTrunkRotation = 0f;
        public float leafTrunkRandomizeRotation = 0.27f;
        public float leafTrunkRandomPositioning = 0f;
        public Vector3 leafTrunkPositioning = Vector3.zero;
        public Vector3 leafTrunkSizeV3 = new Vector3(1f, 1f, 1f);
        public float leafSizeTrunkRandom = 0f;

        // Internal Data Structures
        private class Trunk
        {
            public List<Vector3> trunkBendPositions;
            public Trunk(List<Vector3> trunkBendPositions) { this.trunkBendPositions = trunkBendPositions; }
        }

        private class Branch
        {
            public Vector3 position;
            public Vector3 direction;
            public float length;
            public float adjustedBranchRadius;
            public Quaternion randomRotation;
            public List<Vector3> bendPositions;

            public Branch(Vector3 position, Vector3 direction, float length, float adjustedBranchRadius, Quaternion randomRotation, List<Vector3> bendPositions)
            {
                this.position = position;
                this.direction = direction;
                this.length = length;
                this.randomRotation = randomRotation;
                this.bendPositions = bendPositions;
                this.adjustedBranchRadius = adjustedBranchRadius;
            }
        }

        private class BranchletsX
        {
            public Vector3 branchletPosition;
            public Vector3 direction;
            public float length;
            public float branchletMaxRadius;
            public float sideFactor;
            public Quaternion fixedRotation;
            public List<Vector3> bendPositions;

            public BranchletsX(Vector3 position, Vector3 dir, float len, float branchletMaxRadius, float sideFactor, Quaternion fixedRotation, List<Vector3> bendBranchletPositions)
            {
                this.branchletPosition = position;
                this.direction = dir;
                this.length = len;
                this.branchletMaxRadius = branchletMaxRadius;
                this.sideFactor = sideFactor;
                this.fixedRotation = fixedRotation;
                this.bendPositions = bendBranchletPositions;
            }
        }

        private Trunk trunk;
        private List<Branch> branches = new List<Branch>();
        private List<BranchletsX> branchlets = new List<BranchletsX>();

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        private void Update()
        {
            if (autoUpdate)
            {
                GenerateTree();
            }
        }

        public void GenerateTree()
        {
            Random.InitState(seed);

            // 1. Generate Trunk
            int vCount = 0, tCount = 0, eCount = 0;
            List<Vector3> trunkBendPositions = new List<Vector3>();
            Mesh trunkMesh = CreateTrunkMesh(ref vCount, ref tCount, ref eCount, trunkBendPositions);
            trunk = new Trunk(trunkBendPositions);

            // 2. Generate Branches
            List<CombineInstance> branchCombineInstances = new List<CombineInstance>();
            branches.Clear();
            GenerateTreeBranches(branchCombineInstances);

            // 3. Generate Branchlets
            List<CombineInstance> branchletCombineInstances = new List<CombineInstance>();
            branchlets.Clear();
            GenerateTreeBranchlets(branchletCombineInstances);

            // 4. Combine Bark Meshes (Trunk + Branches + Branchlets)
            List<CombineInstance> barkCombineInstances = new List<CombineInstance>();
            
            barkCombineInstances.Add(new CombineInstance { mesh = trunkMesh, transform = Matrix4x4.identity });
            barkCombineInstances.AddRange(branchCombineInstances);
            barkCombineInstances.AddRange(branchletCombineInstances);

            Mesh barkMesh = new Mesh();
            // Use 32 bit index buffer to support large trees
            barkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
            barkMesh.CombineMeshes(barkCombineInstances.ToArray(), true, true);

            // 5. Generate Leaves
            List<CombineInstance> leafCombineInstances = new List<CombineInstance>();
            if (leafPrefab != null)
            {
                Mesh leafMeshAsset = leafPrefab.GetComponent<MeshFilter>()?.sharedMesh;
                if (leafMeshAsset != null)
                {
                    GenerateLeafPlanes(leafCombineInstances, leafMeshAsset);
                    GenerateLeafBranchletPlanes(leafCombineInstances, leafMeshAsset);
                    GenerateLeafTrunkPlanes(leafCombineInstances, leafMeshAsset);
                }
            }

            Mesh finalLeafMesh = new Mesh();
            finalLeafMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalLeafMesh.CombineMeshes(leafCombineInstances.ToArray(), true, true);

            // 6. Final Combine (Submeshes for Materials)
            // We want 2 submeshes: 0 for Bark, 1 for Leaves
            CombineInstance[] finalCombine = new CombineInstance[2];
            finalCombine[0].mesh = barkMesh;
            finalCombine[0].transform = Matrix4x4.identity;
            
            finalCombine[1].mesh = finalLeafMesh;
            finalCombine[1].transform = Matrix4x4.identity;

            Mesh finalMesh = new Mesh();
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(finalCombine, false, false); // false for mergeSubMeshes to keep them separate
            
            meshFilter.sharedMesh = finalMesh;

            // Assign Materials
            Material[] mats = new Material[2];
            mats[0] = trunkMaterial != null ? trunkMaterial : new Material(Shader.Find("Standard"));
            mats[1] = leafMaterial != null ? leafMaterial : new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterials = mats;
        }

        // --- Trunk Generation ---
        private Mesh CreateTrunkMesh(ref int vertexCount, ref int triangleCount, ref int edgeCount, List<Vector3> trunkBendPositions)
        {
            Mesh mesh = new Mesh();

            int radialSegments = Mathf.Max(6, 3 + trunkSubdivision);
            int horizontalSegments = Mathf.Max(2, trunkSegments);
            int verticesCount = (radialSegments + 1) * (horizontalSegments + 1) + 1;
            Vector3[] vertices = new Vector3[verticesCount];
            int[] triangles = new int[radialSegments * horizontalSegments * 6 + radialSegments * 3];
            Vector2[] uvs = new Vector2[verticesCount];
            Color[] colors = new Color[verticesCount];

            float topRadius = trunkRadius * Mathf.Clamp01(1 - trunkRadiusCurvature);
            float bottomRadius = trunkRadius * Mathf.Clamp01(1 + trunkRadiusCurvature);

            float randomBending = Random.Range(15f, 45f);

            for (int y = 0; y <= horizontalSegments; y++)
            {
                float heightFraction = (float)y / horizontalSegments;
                float radius;

                if (includeStump && heightFraction < treeStumpStartPoint)
                {
                    float stumpFactor = 1f - (heightFraction / treeStumpStartPoint);
                    float exaggeratedStumpScale = 1f + stumpFactor * treeStumpWidth;
                    float baseRadius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);
                    radius = baseRadius * exaggeratedStumpScale;
                }
                else
                {
                    radius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);
                }

                float bendOffset = Mathf.Sin(heightFraction * Mathf.PI * randomBending * trunkBending) * trunkHeight * Mathf.Abs(trunkBending);
                Vector3 bendPosition = new Vector3(0, heightFraction * trunkHeight, 0) + new Vector3(bendOffset, 0, 0);
                trunkBendPositions.Add(bendPosition);

                for (int x = 0; x <= radialSegments; x++)
                {
                    float angle = Mathf.PI * 2 * x / radialSegments;
                    float twist = trunkCrinkliness * heightFraction * Mathf.PI * 2;
                    float crinkledAngle = angle + twist;

                    float xPos = Mathf.Cos(crinkledAngle);
                    float zPos = Mathf.Sin(crinkledAngle);

                    float noiseFactor = 1 + (Mathf.PerlinNoise(xPos * trunkRadiusNoise + y, zPos * trunkRadiusNoise + y) - 0.5f) * trunkRadiusNoise;
                    xPos *= radius * noiseFactor;
                    zPos *= radius * noiseFactor;

                    int index = y * (radialSegments + 1) + x;
                    vertices[index] = new Vector3(xPos + bendOffset, heightFraction * trunkHeight, zPos);

                    float u = (float)x / radialSegments;
                    float v = heightFraction * trunkHeight;
                    uvs[index] = new Vector2(u, v);
                    colors[index] = Color.blue;
                }
            }

            int topRingStartIndex = horizontalSegments * (radialSegments + 1);
            Vector3 topVertexPosition = vertices[topRingStartIndex];
            int pointedTopIndex = verticesCount - 1;
            vertices[pointedTopIndex] = topVertexPosition;
            uvs[pointedTopIndex] = new Vector2(0f, 1);
            colors[pointedTopIndex] = Color.blue;

            int triIndex = 0;
            for (int y = 0; y < horizontalSegments; y++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int current = y * (radialSegments + 1) + x;
                    int next = current + 1;
                    int above = current + radialSegments + 1;
                    int aboveNext = above + 1;

                    triangles[triIndex++] = current;
                    triangles[triIndex++] = above;
                    triangles[triIndex++] = next;

                    triangles[triIndex++] = next;
                    triangles[triIndex++] = above;
                    triangles[triIndex++] = aboveNext;
                }
            }

            for (int x = 0; x < radialSegments; x++)
            {
                int current = topRingStartIndex + x;
                int next = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current;
                triangles[triIndex++] = pointedTopIndex;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.RecalculateNormals();

            vertexCount = mesh.vertexCount;
            triangleCount = mesh.triangles.Length / 3;
            // edgeCount calculation skipped for runtime performance

            return mesh;
        }

        // --- Branch Generation ---
        private void GenerateTreeBranches(List<CombineInstance> combineInstances)
        {
            for (int i = 0; i < numberOfBranches; i++)
            {
                float height = Random.Range(branchHeightMin * trunkHeight, branchHeightMax * trunkHeight);

                float adjustedBranchLength = branchLength;
                if (adjustBranchLengthByHeight)
                {
                    float normalizedHeight = height / trunkHeight;
                    adjustedBranchLength = Mathf.Lerp(branchLength, branchLength / 5f, normalizedHeight);
                }

                float trunkRadiusAtHeight = Mathf.Lerp(
                    trunkRadius * Mathf.Clamp01(1 + trunkRadiusCurvature),
                    trunkRadius * Mathf.Clamp01(1 - trunkRadiusCurvature),
                    height / trunkHeight
                );

                float adjustedBranchRadius = Mathf.Min(branchRadius, trunkRadiusAtHeight);

                float normalizedHeightAlongTrunk = height / trunkHeight;
                int index1 = Mathf.FloorToInt(normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1));
                int index2 = Mathf.Min(index1 + 1, trunk.trunkBendPositions.Count - 1);

                Vector3 trunkBendPosition1 = trunk.trunkBendPositions[index1];
                Vector3 trunkBendPosition2 = trunk.trunkBendPositions[index2];
                float t = normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1) - index1;

                Vector3 branchPosition = Vector3.Lerp(trunkBendPosition1, trunkBendPosition2, t); // Local position relative to trunk start (which is 0,0,0 local)
                // Note: Original code added trunkObject.transform.position, but we want local space for mesh combination
                branchPosition.y = height; 

                float adjustedBranchAngle = branchAngle;
                if (angleAdjustmentByHeight)
                {
                    float normalizedHeight = height / trunkHeight;
                    float heightBasedAdjustment = normalizedHeight * 30f;
                    adjustedBranchAngle = Mathf.Min(branchAngle + heightBasedAdjustment, 160f);
                }
                float segmentAngle = 360f / numberOfBranches;
                float baseRotationAngle = i * segmentAngle;
                float randomVariation = Random.Range(-segmentAngle / 4f, segmentAngle / 4f);
                float randomRotationAngle = baseRotationAngle + randomVariation;

                Quaternion randomRotation = Quaternion.Euler(adjustedBranchAngle, randomRotationAngle, 0f);
                Vector3 direction = Quaternion.Euler(-90f, 0f, 0f) * Vector3.forward;
                direction = randomRotation * direction;

                List<Vector3> bendPositions = new List<Vector3>();
                int v = 0, tr = 0, e = 0;
                Mesh branchMesh = CreateBranchMesh(gravity, adjustedBranchRadius, adjustedBranchLength, branchBending, direction, branchPosition, randomRotation, bendPositions, branchSegments, branchSubdivision, ref v, ref tr, ref e);

                branches.Add(new Branch(branchPosition, direction, adjustedBranchLength, adjustedBranchRadius, randomRotation, bendPositions));

                combineInstances.Add(new CombineInstance { mesh = branchMesh, transform = Matrix4x4.identity }); // Mesh vertices are already in "tree local" space
            }
        }

        private Mesh CreateBranchMesh(float gravity, float adjustedBranchRadius, float adjustedBranchLength, float branchBending, Vector3 direction, Vector3 branchPosition, Quaternion randomRotation, List<Vector3> bendPositions, int branchSegments, int branchSubdivision, ref int vertexBranchCount, ref int triangleBranchCount, ref int edgeBranchCount)
        {
            Mesh mesh = new Mesh();

            int radialSegments = Mathf.Max(6, 3 + branchSubdivision);
            int horizontalSegments = Mathf.Max(2, branchSegments);
            int verticesCount = (radialSegments + 1) * (horizontalSegments + 1) + 1;
            Vector3[] vertices = new Vector3[verticesCount];
            int[] triangles = new int[radialSegments * horizontalSegments * 6 + radialSegments * 3];
            Vector2[] uvs = new Vector2[verticesCount];

            float bendNoiseSeed = Random.Range(0f, 100f) + branchPosition.x + branchPosition.y + branchPosition.z;
            float topRadius = adjustedBranchRadius * Mathf.Clamp01(1 - branchRadiusCurvature);
            float bottomRadius = adjustedBranchRadius * Mathf.Clamp01(1 + branchRadiusCurvature);

            for (int y = 0; y <= horizontalSegments; y++)
            {
                float heightFraction = (float)y / horizontalSegments;
                float radius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);

                float bendNoise1 = Mathf.PerlinNoise(heightFraction * 10f, bendNoiseSeed);
                float bendNoise2 = Mathf.PerlinNoise(heightFraction * 15f, bendNoiseSeed + 5f);
                float combinedNoise = Mathf.Lerp(bendNoise1, bendNoise2, 0.5f);
                float randomOffset = Random.Range(-0.02f, 0.02f);

                float bendOffset = Mathf.Sin(heightFraction * Mathf.PI * 30 * branchBending * combinedNoise) * adjustedBranchLength * Mathf.Abs(branchBending) * 0.5f + randomOffset;
                float gravityBend = Mathf.Sin(heightFraction * Mathf.PI) * gravity;

                Vector3 localBendOffset = new Vector3(bendOffset, 0f, gravityBend);
                Vector3 worldBendOffset = randomRotation * localBendOffset;
                Vector3 bendPosition = branchPosition + direction * heightFraction * adjustedBranchLength + worldBendOffset;

                bendPositions.Add(bendPosition);

                for (int x = 0; x <= radialSegments; x++)
                {
                    float angle = Mathf.PI * 2 * x / radialSegments;
                    float twist = branchCrinkliness * heightFraction * Mathf.PI * 2;
                    float crinkledAngle = angle + twist;

                    float xPos = Mathf.Cos(crinkledAngle);
                    float zPos = Mathf.Sin(crinkledAngle);

                    float radiusNoise = Mathf.PerlinNoise(xPos * branchRadiusNoise + y, zPos * branchRadiusNoise + y);
                    float noiseAdjustedRadius = radius * (1 + (radiusNoise - 0.5f) * branchRadiusNoise);

                    xPos *= noiseAdjustedRadius;
                    zPos *= noiseAdjustedRadius;

                    // Note: vertices are set directly in "tree local" space (relative to trunk root)
                    // The original code did this too, but wrapped in a GameObject. 
                    // Here we calculate the position relative to the branch start, then add branchPosition?
                    // Wait, the original code:
                    // vertices[...] = new Vector3(xPos + bendOffset, adjustedBranchLength * heightFraction, zPos + gravityBend);
                    // This creates a mesh where (0,0,0) is the branch start.
                    // BUT then it sets the GameObject position to `branchPosition` and rotation to `randomRotation`.
                    // So the mesh itself is in "branch local" space.
                    // To combine them into one mesh, I need to transform these vertices into "tree local" space.
                    
                    Vector3 vertexLocalToBranch = new Vector3(xPos + bendOffset, adjustedBranchLength * heightFraction, zPos + gravityBend);
                    
                    // Transform: Rotate then Translate
                    // The branch GameObject had: position = branchPosition, rotation = randomRotation, up = direction
                    // Actually, the original code sets `branch.transform.up = direction`. 
                    // And `branch.transform.rotation = randomRotation`. 
                    // Wait, setting `up` overrides rotation? Or vice versa?
                    // Original:
                    // branch.transform.up = direction;
                    // branch.transform.rotation = randomRotation;
                    // If `direction` is derived from `randomRotation` (which it is: direction = randomRotation * ...), then they are consistent.
                    // So the transform is just `randomRotation` and `branchPosition`.
                    
                    Vector3 vertexTreeLocal = (randomRotation * vertexLocalToBranch) + branchPosition;
                    
                    vertices[y * (radialSegments + 1) + x] = vertexTreeLocal;

                    float u = (float)x / radialSegments;
                    float v = heightFraction * adjustedBranchLength;
                    uvs[y * (radialSegments + 1) + x] = new Vector2(u, v);
                }
            }

            int topRingStartIndex = horizontalSegments * (radialSegments + 1);
            Vector3 topVertexPosition = vertices[topRingStartIndex];
            int pointedTipIndex = verticesCount - 1;
            Vector3 pointedTipPosition = topVertexPosition; // + new Vector3(0, 0f, 0);
            float topRadiusDiameter = topRadius * 2;
            // pointedTipPosition.x -= topRadiusDiameter / 2; // Why? Original code did this.
            // But wait, if we are in Tree Local space, we can't just subtract X. We need to move along the local 'left' or something?
            // The original code was in Branch Local space.
            // Let's replicate the original logic in Branch Local then transform.
            
            // Re-calculating tip in Tree Local:
            // We need the tip in Branch Local first.
            // The loop above calculated `topVertexPosition` in Tree Local.
            // Let's calculate the tip in Branch Local and transform it.
            // Actually, `vertices` array already contains Tree Local positions.
            // The tip logic in original code:
            // vertices[pointedTipIndex] = pointedTipPosition;
            // pointedTipPosition.x -= topRadiusDiameter / 2;
            // This seems like a hack to close the mesh?
            // Let's just use the center of the top ring.
            
            // Calculate center of top ring in Tree Local
            Vector3 topRingCenterBranchLocal = new Vector3(0 + bendPositions.Last().x - branchPosition.x, adjustedBranchLength, 0 + bendPositions.Last().z - branchPosition.z); 
            // Wait, bendPositions are in Tree Local.
            // Let's just take the last bendPosition. That is the center of the branch at the tip.
            Vector3 tipCenterTreeLocal = bendPositions[bendPositions.Count - 1];
            
            vertices[pointedTipIndex] = tipCenterTreeLocal;
            uvs[pointedTipIndex] = new Vector2(0.5f, 1);

            int triIndex = 0;
            for (int y = 0; y < horizontalSegments; y++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int current = y * (radialSegments + 1) + x;
                    int next = current + 1;
                    int above = current + radialSegments + 1;
                    int aboveNext = above + 1;

                    triangles[triIndex++] = current;
                    triangles[triIndex++] = above;
                    triangles[triIndex++] = next;

                    triangles[triIndex++] = next;
                    triangles[triIndex++] = above;
                    triangles[triIndex++] = aboveNext;
                }
            }

            for (int x = 0; x < radialSegments; x++)
            {
                int current = topRingStartIndex + x;
                int next = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current;
                triangles[triIndex++] = pointedTipIndex;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            return mesh;
        }

        // --- Branchlet Generation ---
        private void GenerateTreeBranchlets(List<CombineInstance> combineInstances)
        {
            if (branches.Count == 0) return;

            int branchletPerBranch = Mathf.FloorToInt((float)numberOfBranchlets / branches.Count);
            int remainderBranchlets = numberOfBranchlets % branches.Count;
            int branchletCounter = 0;

            float minHeight = branches.Min(b => b.position.y);
            float maxHeight = branches.Max(b => b.position.y);

            for (int i = 0; i < branches.Count; i++)
            {
                Branch branch = branches[i];
                int branchletCount = branchletPerBranch + (i < remainderBranchlets ? 1 : 0);

                for (int j = 0; j < branchletCount; j++)
                {
                    float adjustedBranchletLength = branchletLength;
                    if (adjustBranchletLengthByHeight)
                    {
                        float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, branch.position.y);
                        adjustedBranchletLength = Mathf.Lerp(branchletLength, branchletLength / 3f, normalizedHeight);
                    }

                    float branchletHeight = Random.Range(branchletHeightMin, branchletHeightMax);
                    branchletHeight = Mathf.Clamp01(branchletHeight);

                    Vector3 branchletPosition = Vector3.zero;

                    if (branch.bendPositions.Count > 1)
                    {
                        float totalLength = 0f;
                        List<float> segmentLengths = new List<float>();
                        for (int k = 0; k < branch.bendPositions.Count - 1; k++)
                        {
                            float segmentLength = Vector3.Distance(branch.bendPositions[k], branch.bendPositions[k + 1]);
                            segmentLengths.Add(segmentLength);
                            totalLength += segmentLength;
                        }

                        float targetLength = branchletHeight * totalLength;
                        float accumulatedLength = 0f;
                        for (int k = 0; k < segmentLengths.Count; k++)
                        {
                            if (accumulatedLength + segmentLengths[k] >= targetLength)
                            {
                                float segmentFactor = (targetLength - accumulatedLength) / segmentLengths[k];
                                branchletPosition = Vector3.Lerp(branch.bendPositions[k], branch.bendPositions[k + 1], segmentFactor);
                                break;
                            }
                            accumulatedLength += segmentLengths[k];
                        }
                    }
                    else
                    {
                        branchletPosition = branch.position + branch.direction.normalized * branch.length * branchletHeight;
                    }

                    Vector3 forwardDirection = branch.direction.normalized;
                    if (branch.bendPositions.Count > 1)
                    {
                        forwardDirection = (branch.bendPositions[Mathf.Min(branch.bendPositions.Count - 1, 1)] - branch.bendPositions[0]).normalized;
                    }
                    Quaternion branchRotation = Quaternion.LookRotation(forwardDirection);

                    float sideFactor = (j % 2 == 0) ? 1f : -1f;
                    float adjustedRotationAngle = sideFactor * branchletForwardAngle;
                    Quaternion fixedRotation = branchRotation;
                    fixedRotation = Quaternion.AngleAxis(adjustedRotationAngle, branchRotation * Vector3.forward) * fixedRotation;
                    fixedRotation = Quaternion.AngleAxis(branchletAngle, branchRotation * Vector3.right) * fixedRotation;

                    Vector3 direction = fixedRotation * Vector3.up;
                    float branchletRadiusVal = CalculateBranchletRadius(branchletPosition, branch);
                    
                    List<Vector3> bendBranchletPositions = new List<Vector3>();
                    int v = 0, tr = 0, e = 0;
                    Mesh branchletMesh = CreateBranchletMesh(adjustedBranchletLength, gravityBranchlets, branchletRadiusVal, branchletBending, branchletSegments, branchletSubdivision, ref v, ref tr, ref e, direction, branchletPosition, fixedRotation, branchRotation, bendBranchletPositions);

                    branchlets.Add(new BranchletsX(branchletPosition, direction, branchletLength, branchletRadiusVal, sideFactor, fixedRotation, bendBranchletPositions));
                    combineInstances.Add(new CombineInstance { mesh = branchletMesh, transform = Matrix4x4.identity });
                    branchletCounter++;
                }
            }
        }

        private float CalculateBranchletRadius(Vector3 branchletPosition, Branch branch)
        {
            float totalLength = 0f;
            List<float> segmentLengths = new List<float>();
            for (int j = 0; j < branch.bendPositions.Count - 1; j++)
            {
                float segmentLength = Vector3.Distance(branch.bendPositions[j], branch.bendPositions[j + 1]);
                segmentLengths.Add(segmentLength);
                totalLength += segmentLength;
            }
            float accumulatedLength = 0f;
            float branchletLengthAlongBranch = 0f;
            for (int j = 0; j < segmentLengths.Count; j++)
            {
                if (accumulatedLength + segmentLengths[j] >= Vector3.Distance(branch.bendPositions[0], branchletPosition))
                {
                    branchletLengthAlongBranch = accumulatedLength + (Vector3.Distance(branch.bendPositions[0], branchletPosition) - accumulatedLength);
                    break;
                }
                accumulatedLength += segmentLengths[j];
            }
            float heightFraction = branchletLengthAlongBranch / totalLength;
            float topRadius = branch.adjustedBranchRadius * Mathf.Clamp01(1 - branchRadiusCurvature);
            float bottomRadius = branch.adjustedBranchRadius * Mathf.Clamp01(1 + branchRadiusCurvature);
            return Mathf.Lerp(bottomRadius, topRadius, heightFraction);
        }

        private Mesh CreateBranchletMesh(float adjustedBranchletLength, float gravityBranchlets, float branchletMaxRadius, float branchletBending, int branchletSegments, int branchletSubdivision, ref int vertexBranchletCount, ref int triangleBranchletCount, ref int edgeBranchletCount, Vector3 direction, Vector3 branchletPosition, Quaternion fixedRotation, Quaternion branchRotation, List<Vector3> bendBranchletPositions)
        {
            Mesh mesh = new Mesh();

            int radialSegments = Mathf.Max(6, 3 + branchletSubdivision);
            int horizontalSegments = Mathf.Max(2, branchletSegments);
            int verticesCount = (radialSegments + 1) * (horizontalSegments + 1) + 1;
            Vector3[] vertices = new Vector3[verticesCount];
            int[] triangles = new int[radialSegments * horizontalSegments * 6 + radialSegments * 3];
            Vector2[] uvs = new Vector2[verticesCount];

            float topRadius = branchletMaxRadius * Mathf.Clamp01(1 - branchletRadiusCurvature);
            float bottomRadius = branchletMaxRadius * Mathf.Clamp01(1 + branchletRadiusCurvature);
            float branchletBendNoiseSeed = Random.Range(0f, 100f);

            for (int y = 0; y <= horizontalSegments; y++)
            {
                float heightFraction = (float)y / horizontalSegments;
                float radius = Mathf.Lerp(bottomRadius, topRadius, heightFraction);

                float bendNoise = Mathf.PerlinNoise(branchletBendNoiseSeed, heightFraction * 10f);
                float noiseFactor1 = Mathf.Lerp(0.8f, 1.2f, bendNoise);
                float bendOffset = Mathf.Sin(heightFraction * Mathf.PI * 30 * branchletBending * noiseFactor1)
                                   * adjustedBranchletLength * Mathf.Abs(branchletBending) * 0.5f;
                float gravityBend = Mathf.Sin(heightFraction * Mathf.PI) * gravityBranchlets;

                Vector3 localBendOffset = new Vector3(bendOffset, 0f, gravityBend);
                Vector3 worldBendOffset = fixedRotation * localBendOffset;
                Vector3 bendPosition = branchletPosition + direction * heightFraction * adjustedBranchletLength + worldBendOffset;

                bendBranchletPositions.Add(bendPosition);

                for (int x = 0; x <= radialSegments; x++)
                {
                    float angle = Mathf.PI * 2 * x / radialSegments;
                    float twist = branchletCrinkliness * heightFraction * Mathf.PI * 2;
                    float crinkledAngle = angle + twist;

                    float xPos = Mathf.Cos(crinkledAngle);
                    float zPos = Mathf.Sin(crinkledAngle);

                    float noiseFactor = 1 + (Mathf.PerlinNoise(xPos * branchletRadiusNoise + y, zPos * branchletRadiusNoise + y) - 0.5f) * branchletRadiusNoise;
                    xPos *= radius * noiseFactor;
                    zPos *= radius * noiseFactor;

                    // Convert to Tree Local Space
                    Vector3 vertexLocalToBranchlet = new Vector3(xPos + bendOffset, heightFraction * adjustedBranchletLength, zPos + gravityBend);
                    Vector3 vertexTreeLocal = (fixedRotation * vertexLocalToBranchlet) + branchletPosition;

                    vertices[y * (radialSegments + 1) + x] = vertexTreeLocal;

                    float u = (float)x / radialSegments;
                    float v = heightFraction * adjustedBranchletLength;
                    uvs[y * (radialSegments + 1) + x] = new Vector2(u, v);
                }
            }

            int topRingStartIndex = horizontalSegments * (radialSegments + 1);
            int pointedTipIndex = verticesCount - 1;
            
            // Tip in Tree Local
            Vector3 tipCenterTreeLocal = bendBranchletPositions[bendBranchletPositions.Count - 1];
            vertices[pointedTipIndex] = tipCenterTreeLocal;
            uvs[pointedTipIndex] = new Vector2(0.5f, 1);

            int triIndex = 0;
            for (int y = 0; y < horizontalSegments; y++)
            {
                for (int x = 0; x < radialSegments; x++)
                {
                    int current = y * (radialSegments + 1) + x;
                    int next = current + 1;
                    int above = current + radialSegments + 1;
                    int aboveNext = above + 1;

                    triangles[triIndex++] = current;
                    triangles[triIndex++] = above;
                    triangles[triIndex++] = next;

                    triangles[triIndex++] = next;
                    triangles[triIndex++] = above;
                    triangles[triIndex++] = aboveNext;
                }
            }

            for (int x = 0; x < radialSegments; x++)
            {
                int current = topRingStartIndex + x;
                int next = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current;
                triangles[triIndex++] = pointedTipIndex;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            return mesh;
        }

        // --- Leaf Generation ---
        private void GenerateLeafPlanes(List<CombineInstance> combineInstances, Mesh leafMesh)
        {
            if (branches.Count == 0) return;

            // Clamp values
            leafPositionMin = Mathf.Clamp01(leafPositionMin);
            leafPositionMax = Mathf.Clamp01(leafPositionMax);
            if (leafPositionMin > leafPositionMax) (leafPositionMin, leafPositionMax) = (leafPositionMax, leafPositionMin);

            foreach (Branch branch in branches)
            {
                for (int i = 0; i < numberOfLeaves; i++)
                {
                    float adjustedLeafRotation = (i % 2 == 0) ? leafRotation : -leafRotation;
                    float randomizedLeafForwardRotation = leafForwardRotation + Random.Range(-360f, 360f) * leafRandomizeRotation;
                    float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafRandomizeRotation;

                    float t = Random.Range(leafPositionMin, leafPositionMax);
                    int segmentCount = branch.bendPositions.Count - 1;
                    int segmentIndex = Mathf.FloorToInt(t * segmentCount);
                    float segmentT = (t * segmentCount) - segmentIndex;
                    segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);

                    Vector3 leafPosition = Vector3.zero;
                    if (Mathf.Approximately(t, 1f))
                        leafPosition = branch.bendPositions[branch.bendPositions.Count - 1];
                    else
                        leafPosition = Vector3.Lerp(branch.bendPositions[segmentIndex], branch.bendPositions[segmentIndex + 1], segmentT);

                    leafPosition += leafBranchPositioning;

                    Vector3 localDirection = (branch.bendPositions[segmentIndex + 1] - branch.bendPositions[segmentIndex]).normalized;
                    Quaternion branchRotation = Quaternion.LookRotation(localDirection);
                    Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, 0f);
                    Quaternion finalRotation = branchRotation * customRotation;

                    float randomSizeMultiplier = 1 + (Random.Range(-leafSizeBranchRandom, leafSizeBranchRandom));
                    Vector3 scale = Vector3.Scale(leafBranchSizeV3, new Vector3(leafSize, leafSize, leafSize)) * randomSizeMultiplier;

                    leafPosition += new Vector3(
                        Random.Range(0f, leafBranchRandomPositioning),
                        Random.Range(0f, leafBranchRandomPositioning),
                        Random.Range(0f, leafBranchRandomPositioning)
                    );

                    Matrix4x4 matrix = Matrix4x4.TRS(leafPosition, finalRotation, scale);
                    combineInstances.Add(new CombineInstance { mesh = leafMesh, transform = matrix });
                }
            }
        }

        private void GenerateLeafBranchletPlanes(List<CombineInstance> combineInstances, Mesh leafMesh)
        {
            if (branchlets.Count == 0) return;

            leafBranchletPositionMin = Mathf.Clamp01(leafBranchletPositionMin);
            leafBranchletPositionMax = Mathf.Clamp01(leafBranchletPositionMax);
            if (leafBranchletPositionMin > leafBranchletPositionMax) (leafBranchletPositionMin, leafBranchletPositionMax) = (leafBranchletPositionMax, leafBranchletPositionMin);

            foreach (BranchletsX branchlet in branchlets)
            {
                for (int i = 0; i < numberOfLeavesBranchlet; i++)
                {
                    float adjustedLeafRotation = (i % 2 == 0) ? leafBranchletRotation : -leafBranchletRotation;
                    float randomizedLeafForwardRotation = leafBranchletForwardRotation + Random.Range(-360f, 360f) * leafBranchletRandomizeRotation;
                    float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafBranchletRandomizeRotation;

                    float t = Random.Range(leafBranchletPositionMin, leafBranchletPositionMax);
                    int segmentCount = branchlet.bendPositions.Count - 1;
                    int segmentIndex = Mathf.FloorToInt(t * segmentCount);
                    float segmentT = (t * segmentCount) - segmentIndex;
                    segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);

                    Vector3 leafBranchletPosition = Vector3.zero;
                    if (Mathf.Approximately(t, 1f))
                        leafBranchletPosition = branchlet.bendPositions[branchlet.bendPositions.Count - 1];
                    else
                        leafBranchletPosition = Vector3.Lerp(branchlet.bendPositions[segmentIndex], branchlet.bendPositions[segmentIndex + 1], segmentT);

                    leafBranchletPosition += leafBranchletPositioning;

                    //Quaternion leafBranchletRotationQuaternion = branchlet.fixedRotation;
                    //leafBranchletRotationQuaternion = Quaternion.Euler(randomizedLeafRotation, randomizedLeafForwardRotation, 0f);
                    Vector3 localDirection = (branchlet.bendPositions[segmentIndex + 1] - branchlet.bendPositions[segmentIndex]).normalized;
                    Quaternion branchletRotation = Quaternion.LookRotation(localDirection);
                    Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, 0f);
                    Quaternion finalRotation = branchletRotation * customRotation;

                    float randomSizeMultiplier = 1 + (Random.Range(-leafSizeBranchletRandom, leafSizeBranchletRandom));
                    Vector3 scale = Vector3.Scale(leafBranchletSizeV3, new Vector3(leafBranchletSize, leafBranchletSize, leafBranchletSize)) * randomSizeMultiplier;

                    leafBranchletPosition += new Vector3(
                        Random.Range(0f, leafBranchletRandomPositioning),
                        Random.Range(0f, leafBranchletRandomPositioning),
                        Random.Range(0f, leafBranchletRandomPositioning)
                    );

                    Matrix4x4 matrix = Matrix4x4.TRS(leafBranchletPosition, finalRotation, scale);
                    combineInstances.Add(new CombineInstance { mesh = leafMesh, transform = matrix });
                }
            }
        }

        private void GenerateLeafTrunkPlanes(List<CombineInstance> combineInstances, Mesh leafMesh)
        {
            if (trunk == null) return;

            leafTrunkPositionMin = Mathf.Clamp01(leafTrunkPositionMin);
            leafTrunkPositionMax = Mathf.Clamp01(leafTrunkPositionMax);
            if (leafTrunkPositionMin > leafTrunkPositionMax) (leafTrunkPositionMin, leafTrunkPositionMax) = (leafTrunkPositionMax, leafTrunkPositionMin);

            for (int i = 0; i < numberOfLeavesTrunk; i++)
            {
                float adjustedLeafRotation = (i % 2 == 0) ? leafTrunkRotation : -leafTrunkRotation;
                float randomizedLeafForwardRotation = leafTrunkForwardRotation + Random.Range(-360f, 360f) * leafTrunkRandomizeRotation;
                float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafTrunkRandomizeRotation;

                float t = Random.Range(leafTrunkPositionMin, leafTrunkPositionMax);
                int segmentCount = trunk.trunkBendPositions.Count - 1;
                int index1 = Mathf.FloorToInt(t * segmentCount);
                int index2 = Mathf.Clamp(index1 + 1, 0, segmentCount);
                float segmentT = (t * segmentCount) - index1;

                Vector3 trunkBendPosition1 = trunk.trunkBendPositions[index1];
                Vector3 trunkBendPosition2 = trunk.trunkBendPositions[index2];
                Vector3 leafTrunkPosition = Vector3.Lerp(trunkBendPosition1, trunkBendPosition2, segmentT); // Local space

                leafTrunkPosition += leafTrunkPositioning;

                float segmentAngle = 360f / numberOfLeavesTrunk;
                float baseRotationAngle = i * segmentAngle;
                float randomVariation = Random.Range(-segmentAngle / 4f, segmentAngle / 4f);
                float randomRotationAngle = baseRotationAngle + randomVariation;

                Vector3 localDirection = (trunkBendPosition2 - trunkBendPosition1).normalized;
                Quaternion trunkRotation = Quaternion.LookRotation(localDirection);
                Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, randomRotationAngle);
                Quaternion finalRotation = trunkRotation * customRotation;

                float randomSizeMultiplier = 1 + (Random.Range(-leafSizeTrunkRandom, leafSizeTrunkRandom));
                Vector3 scale = Vector3.Scale(leafTrunkSizeV3, new Vector3(leafTrunkSize, leafTrunkSize, leafTrunkSize)) * randomSizeMultiplier;

                leafTrunkPosition += new Vector3(
                    Random.Range(0f, leafTrunkRandomPositioning),
                    Random.Range(0f, leafTrunkRandomPositioning),
                    Random.Range(0f, leafTrunkRandomPositioning)
                );

                Matrix4x4 matrix = Matrix4x4.TRS(leafTrunkPosition, finalRotation, scale);
                combineInstances.Add(new CombineInstance { mesh = leafMesh, transform = matrix });
            }
        }
    }
}
