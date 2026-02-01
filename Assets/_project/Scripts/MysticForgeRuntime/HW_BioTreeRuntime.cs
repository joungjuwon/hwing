using System.Collections.Generic;
using UnityEngine;

namespace MysticForgeRuntime
{
    public class HW_BioTreeRuntime : MonoBehaviour
    {
        [Header("Growth Settings")]
        [Range(0f, 1f)] public float growthCycle = 0f;
        public float growthSpeed = 0.5f;
        public bool autoGrow = false;
        
        [Header("Tree Parameters")]
        public float maxTrunkHeight = 5f;
        public float maxTrunkThickness = 0.2f;
        [Range(0, 5)] public int maxRecursion = 3;
        
        [Header("Branching Rules")]
        [Range(0.5f, 0.99f)] public float lengthDecay = 0.8f;
        [Range(0.5f, 0.99f)] public float radiusDecay = 0.7f;
        [Range(10f, 90f)] public float branchingAngle = 35f;
        [Range(0.5f, 2.0f)] public float branchSpread = 1.0f;
        [Range(0f, 1f)] public float noiseIntensity = 0.2f;
        [Range(0f, 1f)] public float lengthRandomness = 0.2f;
        [Range(0f, 1f)] public float angleRandomness = 0.2f;
        public int randomSeed = 0;

        [Header("Foliage")]
        public GameObject leafPrefab;
        public Material leafMaterial;
        [Range(0, 10)] public int leavesPerBranch = 5;
        public float leafScale = 2.0f;
        [Range(0f, 1f)] public float leafTipRange = 0.5f;
        public Vector2 leafTiling = Vector2.one;

        [Header("Texture & Material")]
        public Material treeMaterial;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;
        
        // --- SKELETON DATA STRUCTURES ---
        private class BioNode
        {
            public Vector3 position;
            public Vector3 direction; 
            public Quaternion rotation; // Parallel Transport Frame
            public float radius;
            public int depth;
            public BioNode mainChild; 
            public List<BioNode> sideChildren = new List<BioNode>();
            
            // Meshing Data
            public int ringStartIndex = -1; // Vertex index of this node's ring
        }

        private BioNode rootNode;
        private List<Vector3> verts;
        private List<Vector2> uvs;
        private List<int> tris;
        private List<CombineInstance> leafInstances;
        private int radialSegments = 12;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        private void Update()
        {
            if (autoGrow)
            {
                growthCycle += Time.deltaTime * growthSpeed;
                if (growthCycle > 1f) growthCycle = 1f;
                // Only regenerate if changed significantly? 
                // For now, regen every frame for smooth growth animation (if performant enough)
                GenerateTree();
            }
        }

        private void OnValidate()
        {
            if(randomSeed == 0) randomSeed = Random.Range(1, 10000);
            GenerateTree();
        }

        [ContextMenu("Generate")]
        public void GenerateTree()
        {
            // Robust Component Validation
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // Init State
            int masterSeed = (randomSeed == 0) ? (int)System.DateTime.Now.Ticks : randomSeed;
            Random.InitState(masterSeed);
            
            // Fix: Initialize Leaf List BEFORE Skeleton Generation (since skeleton collects leaves)
            leafInstances = new List<CombineInstance>();

            // 1. Generate Skeleton (Logical Tree)
            float height = Mathf.Lerp(0.1f, maxTrunkHeight, Mathf.Clamp01(growthCycle / 0.5f));
            float thick = Mathf.Lerp(0.01f, maxTrunkThickness, growthCycle);
            
            // Initial Rotation pointing UP
            rootNode = GenerateSkeletonNode(Vector3.zero, Vector3.up, height, thick, 0, masterSeed, Quaternion.LookRotation(Vector3.up));

            // 2. Build Single Watertight Mesh
            verts = new List<Vector3>();
            uvs = new List<Vector2>();
            tris = new List<int>();
            
            if (rootNode != null)
            {
                // Recursive mesh builder
                BuildLimbMesh(rootNode);
                
                mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                meshFilter.sharedMesh = mesh;
                
                if (treeMaterial != null) meshRenderer.sharedMaterial = treeMaterial;
                
                BuildLeaves();
            }
        }

        // --- SKELETON GENERATION ---
        private BioNode GenerateSkeletonNode(Vector3 pos, Vector3 dir, float length, float radius, int depth, int seed, Quaternion startRot)
        {
            if (depth >= maxRecursion || radius < 0.005f) return null;

            System.Random rng = new System.Random(seed); // Deterministic

            int segments = 3;
            float segLen = length / segments;
            
            BioNode firstNode = new BioNode { position = pos, direction = dir, radius = radius, depth = depth, rotation = startRot };
            BioNode current = firstNode;

            Vector3 curPos = pos;
            Vector3 curDir = dir; // This is the tangent at 'current'
            
            for(int s=0; s<segments; s++)
            {
                // Calculate NEXT direction (Bend)
                Vector3 nextDirChoice = curDir;
                if (depth > 0)
                {
                    float rX = (float)(rng.NextDouble()*2-1);
                    float rY = (float)(rng.NextDouble()*2-1);
                    float rZ = (float)(rng.NextDouble()*2-1);
                    Vector3 randomDir = (curDir + new Vector3(rX,rY,rZ)*noiseIntensity).normalized;
                    nextDirChoice = Vector3.Lerp(curDir, randomDir, 0.5f).normalized;
                }
                if (depth < maxRecursion/2) nextDirChoice = Vector3.Lerp(nextDirChoice, Vector3.up, 0.1f * lengthDecay).normalized;

                // Parallel Transport: Rotate frame from curDir to nextDirChoice
                Quaternion bend = Quaternion.FromToRotation(curDir, nextDirChoice);
                Quaternion nextRot = bend * current.rotation;

                Vector3 nextPos = curPos + nextDirChoice * segLen;
                float nextRad = Mathf.Lerp(radius, radius * radiusDecay, (float)(s+1)/segments);

                BioNode nextNode = new BioNode { position = nextPos, direction = nextDirChoice, radius = nextRad, depth = depth, rotation = nextRot };
                
                current.mainChild = nextNode;
                current = nextNode;
                
                curPos = nextPos;
                curDir = nextDirChoice;
            }

            // Branching Logic at the Tip
            float depthThreshold = 0.05f * (depth + 1);
            float localGrowth = Mathf.Clamp01((growthCycle - depthThreshold) * 5f); 
            
            if (localGrowth > 0.1f && depth < maxRecursion)
            {
                // Main Extension (Seamless Continuation)
                float baseNewLen = length * lengthDecay * localGrowth;
                int mainChildSeed = rng.Next();
                
                float mainLen = baseNewLen * (1f + (float)(rng.NextDouble()*2-1)*lengthRandomness);
                Vector3 mainDir = Vector3.Lerp(curDir, Vector3.up, 0.2f).normalized; 
                
                // Continue Parallel Transport
                Quaternion mainRot = Quaternion.FromToRotation(curDir, mainDir) * current.rotation;
                
                BioNode extRoot = GenerateSkeletonNode(curPos, mainDir, mainLen, current.radius, depth+1, mainChildSeed, mainRot);
                if(extRoot != null)
                {
                     current.mainChild = extRoot; 
                }

                // Side Branches
                int count = rng.Next(1, 3);
                for(int i=0; i<count; i++)
                {
                    int sideChildSeed = rng.Next();
                    float sideLen = baseNewLen * (1f + (float)(rng.NextDouble()*2-1)*lengthRandomness) * 0.9f;
                    
                    Vector3 sideAxis = Vector3.Cross(curDir, Vector3.up);
                    if(sideAxis == Vector3.zero) sideAxis = Vector3.right;
                    Quaternion spin = Quaternion.AngleAxis((float)rng.NextDouble() * 360f, curDir);
                    Quaternion spread = Quaternion.AngleAxis(branchingAngle * branchSpread, sideAxis);
                    Vector3 sideDir = (spin * spread * curDir).normalized;

                    // New Frame: Align Up with Parent Tangent to minimize rolling
                    Quaternion sideRot = Quaternion.LookRotation(sideDir, curDir); 

                    BioNode sideRoot = GenerateSkeletonNode(curPos, sideDir, sideLen, current.radius * 0.7f, depth+1, sideChildSeed, sideRot);
                    if(sideRoot != null)
                    {
                        current.sideChildren.Add(sideRoot);
                    }
                }
            }
            
            // Collect Leaves
            float depthFactor = (float)depth / maxRecursion;
            if (depthFactor >= (1f - leafTipRange) && leavesPerBranch > 0)
            {
                 Vector3 startL = firstNode.position;
                 Vector3 endL = current.position;
                 for(int l=0; l<leavesPerBranch; l++)
                 {
                     float t = (float)rng.NextDouble();
                     Vector3 lPos = Vector3.Lerp(startL, endL, t);
                     float lRad = Mathf.Lerp(firstNode.radius, current.radius, t);
                     // Fix orientation to use 'current.direction' approx
                     AddLeafClusterData(lPos, current.direction, lRad, leafScale, rng.Next());
                 }
            }

            return firstNode;
        }

        // --- MESH GENERATION ---
        private void BuildLimbMesh(BioNode node)
        {
            if (node == null) return;

            // Generate Ring for this node
            GenerateRing(node); 
            
            BioNode w = node;
            while(w.mainChild != null)
            {
                BioNode next = w.mainChild;
                GenerateRing(next); 
                
                // Stitching Logic
                HashSet<int> holeIndices = new HashSet<int>();
                Dictionary<int, BioNode> indexToBranch = new Dictionary<int, BioNode>();

                foreach(var branch in w.sideChildren)
                {
                    int bestK = 0;
                    float maxDot = -1f;
                    for(int k=0; k<radialSegments; k++)
                    {
                        Vector3 vDir = (verts[w.ringStartIndex + k] - w.position).normalized;
                        float d = Vector3.Dot(vDir, branch.direction);
                        if(d > maxDot) { maxDot = d; bestK = k; }
                    }
                    
                    if(!holeIndices.Contains(bestK))
                    {
                        holeIndices.Add(bestK);
                        indexToBranch[bestK] = branch;
                    }
                }

                int baseA = w.ringStartIndex;
                int baseB = next.ringStartIndex; 
                
                for(int k=0; k<radialSegments; k++)
                {
                    if(holeIndices.Contains(k))
                    {
                        // STITCH Hole -> Branch
                        BioNode branch = indexToBranch[k];
                        BuildLimbMesh(branch); // Recurse
                        BridgeHoleToBranch(baseA, baseB, k, branch);
                    }
                    else
                    {
                        // Regular Quad
                        int idxA1 = baseA + k;
                        int idxA2 = baseA + k + 1;
                        int idxB1 = baseB + k;
                        int idxB2 = baseB + k + 1;
                        AddQuad(idxA1, idxA2, idxB2, idxB1); 
                    }
                }
                
                w = next;
            }
            CloseCap(w);
        }

        private void CloseCap(BioNode node)
        {
            int centerIdx = verts.Count;
            Vector3 capPos = node.position + node.direction * (node.radius * 0.3f);
            verts.Add(capPos); 
            uvs.Add(new Vector2(0.5f, 1f)); 
            
            int baseIdx = node.ringStartIndex;
            for(int s=0; s<radialSegments; s++)
            {
                int current = baseIdx + s;
                int next = baseIdx + s + 1;
                AddTriangle(centerIdx, next, current);
            }
        }
        
        private void GenerateRing(BioNode node)
        {
            node.ringStartIndex = verts.Count;
            // Use stored stable rotation
            Quaternion rot = node.rotation;

            for(int s=0; s<=radialSegments; s++)
            {
                float angle = (float)s / radialSegments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * node.radius;
                float y = Mathf.Sin(angle) * node.radius;
                
                Vector3 p = node.position + rot * new Vector3(x,y,0);
                verts.Add(p); 
                uvs.Add(new Vector2((float)s/radialSegments, node.depth));
            }
        }
        
        private void BridgeHoleToBranch(int baseIdxA, int baseIdxB, int k, BioNode branch)
        {
             int a1 = baseIdxA + k;
             int a2 = baseIdxA + k + 1;
             int b1 = baseIdxB + k;
             int b2 = baseIdxB + k + 1;
             
             int branchBase = branch.ringStartIndex;
             int segs = radialSegments;
             
             // Twist Alignment
             int bestOffset = 0;
             float minDistSq = float.MaxValue;
             Vector3 pA1 = verts[a1];
             
             for(int j=0; j<segs; j++)
             {
                 float d = (verts[branchBase + j] - pA1).sqrMagnitude;
                 if(d < minDistSq)
                 {
                     minDistSq = d;
                     bestOffset = j;
                 }
             }

             int cSegs = segs; 
             for(int i=0; i<cSegs; i++)
             {
                 int offsetI = (i + bestOffset) % segs;
                 int nextOffsetI = (i + bestOffset + 1) % segs;
                 
                 int c1 = branchBase + offsetI;
                 int c2 = branchBase + nextOffsetI;
                 
                 int side = (i * 4) / cSegs;
                 int p1, p2;
                 if (side == 0) { p1 = a1; p2 = a2; }
                 else if (side == 1) { p1 = a2; p2 = b2; }
                 else if (side == 2) { p1 = b2; p2 = b1; }
                 else { p1 = b1; p2 = a1; }
                 
                 tris.Add(p1); tris.Add(c2); tris.Add(c1); 
                 if ((i % 3) == 2) 
                 {
                     tris.Add(p1); tris.Add(p2); tris.Add(c2); 
                 }
             }
        }

        private void AddQuad(int a, int b, int c, int d)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(c); tris.Add(d); tris.Add(a);
        }
        private void AddTriangle(int a, int b, int c)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        private void AddLeafClusterData(Vector3 centerPos, Vector3 branchDir, float radius, float scale, int seed)
        {
             if(leafPrefab == null) return;
             if(leafInstances == null) return;

             MeshFilter mfStruct = leafPrefab.GetComponent<MeshFilter>();
             if (mfStruct == null) return;
             Mesh m = mfStruct.sharedMesh;
             if (m == null) return;
             
             System.Random r = new System.Random(seed);
             
             // Surface Attachment Logic
             float angle = (float)r.NextDouble() * 360f;
             Quaternion roll = Quaternion.AngleAxis(angle, branchDir);
             
             // Get a perpendicular vector (Local Right)
             Vector3 refRight = Vector3.Cross(branchDir, Vector3.up);
             if (refRight.sqrMagnitude < 0.001f) refRight = Vector3.right;
             
             Vector3 surfaceNormal = (roll * refRight).normalized;
             
             // Move to Surface
             Vector3 surfacePos = centerPos + surfaceNormal * radius;
             
             // Look Rotation (Outwards along Normal, with Up as Branch Dir)
             Quaternion baseRot = Quaternion.LookRotation(surfaceNormal, branchDir);
             
             // Optional: Random Perturbation
             Quaternion randRot = Quaternion.Euler(
                (float)r.NextDouble()*30f - 15f,
                (float)r.NextDouble()*30f - 15f,
                (float)r.NextDouble()*30f - 15f
             );

             leafInstances.Add(new CombineInstance() { mesh = m, transform = Matrix4x4.TRS(surfacePos, baseRot * randRot, Vector3.one*scale) });
        }

        private void BuildLeaves()
        {
            Transform t = transform.Find("Leaves");
            
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if(leafInstances == null || leafInstances.Count == 0) 
            {
                 if (t != null) t.gameObject.SetActive(false); 
                 return; 
            }
            
            if(t == null)
            {
                t = new GameObject("Leaves").transform;
                t.SetParent(transform);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
            }
            
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf == null) mf = t.gameObject.AddComponent<MeshFilter>();
            
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();
            
            t.gameObject.SetActive(true);

            Mesh lm = new Mesh();
            lm.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            lm.CombineMeshes(leafInstances.ToArray(), true, true);
            lm.RecalculateNormals();
            
            mf.sharedMesh = lm;
            mr.sharedMaterial = leafMaterial ? leafMaterial : treeMaterial;
        }

    }
}
