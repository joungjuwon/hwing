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
        [Range(1, 8)] public int maxRecursion = 5;
        
        [Header("Branching Rules")]
        [Range(0.5f, 0.99f)] public float lengthDecay = 0.8f;
        [Range(0.5f, 0.99f)] public float radiusDecay = 0.7f;
        [Range(10f, 90f)] public float branchingAngle = 35f;
        [Range(0.5f, 2.0f)] public float branchSpread = 1.0f;
        [Range(0f, 1f)] public float noiseIntensity = 0.2f;
        [Range(0f, 1f)] public float lengthRandomness = 0.2f;
        [Range(45f, 160f)] public float maxVerticalAngle = 100f; 
        public int randomSeed = 0;

        [Header("Space Filling (Volumetric)")]
        [Range(1, 10)] public int sensingSamples = 6;
        [Range(0f, 1f)] public float repulsionStrength = 1.0f;

        [Header("Foliage")]
        public GameObject leafPrefab;
        public Material leafMaterial;
        [Range(0, 10)] public int leavesPerBranch = 5;
        public float leafScale = 2.0f;
        [Range(0f, 1f)] public float leafTipRange = 0.5f;

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
            public Quaternion rotation; 
            public float radius;
            public int depth;
            public BioNode mainChild; 
            public List<BioNode> sideChildren = new List<BioNode>();
            
            public int ringStartIndex = -1; 
        }

        private struct BranchSpec 
        {
            public Vector3 dir;
            public bool isMainRole; 
        }

        private BioNode rootNode;
        private List<Vector3> verts;
        private List<Vector2> uvs;
        private List<int> tris;
        private List<CombineInstance> leafInstances;
        private List<Vector3> occupiedSpace; 

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
                GenerateTree();
            }
        }

        private void OnValidate()
        {
            if(randomSeed == 0) randomSeed = Random.Range(1, 10000);
            GenerateTree();
        }

        private float GetThickness(float tVal)
        {
            // Unused in main loop now, but kept for root reference if needed
            return maxTrunkThickness * (1f - tVal); 
        }

        [ContextMenu("Generate")]
        public void GenerateTree()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            int masterSeed = (randomSeed == 0) ? (int)System.DateTime.Now.Ticks : randomSeed;
            Random.InitState(masterSeed);
            
            leafInstances = new List<CombineInstance>();
            occupiedSpace = new List<Vector3>();

            float height = maxTrunkHeight * growthCycle;
            
            float tG = Mathf.Clamp01(growthCycle);
            float growthScale = tG * tG * (3f - 2f * tG); 

            float rootThick = maxTrunkThickness * growthScale;
            // Root has no "Parent", so parentTipRadius = rootThick (No taper at start).
            rootNode = GenerateSkeletonNode(Vector3.zero, Vector3.up, height, rootThick, 0, masterSeed, Quaternion.LookRotation(Vector3.up), true, rootThick);

            verts = new List<Vector3>();
            uvs = new List<Vector2>();
            tris = new List<int>();
            
            if (rootNode != null)
            {
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
        // Added 'parentTipRadius' parameter to blend joints
        private BioNode GenerateSkeletonNode(Vector3 pos, Vector3 dir, float length, float structuralRadius, int depth, int seed, Quaternion startRot, bool allowTrifurcation, float parentTipRadius)
        {
            if (depth >= maxRecursion || structuralRadius < 0.005f) return null;

            System.Random rng = new System.Random(seed);
            
            int segments = 3;
            float segLen = length / segments;
            
            // --- SMOOTH JOINT LOGIC ---
            // Interpolate Start Radius based on Alignment to Parent.
            // If perfectly aligned (Main), start radius = parentTipRadius (Seamless).
            // If angled (Side), blend.
            // We don't have Parent Dir here easily (dir is current dir).
            // But we assume the Parent Tip was at 'pos'.
            // Let's assume seamless start for stability, then taper to structuralRadius.
            // But for very sharp angles, full seamlessness looks swollen.
            // We'll trust the caller to have oriented us.
            // Let's start at 'parentTipRadius' but clamp it if the branch is tiny.
            // If structuralRadius is 0.01 and parent is 0.2, starting at 0.2 is huge cone.
            // Limit start radius to e.g. 2x structural radius?
            float startRadiusCap = structuralRadius * 1.5f; 
            float effectiveStartRadius = Mathf.Min(parentTipRadius, startRadiusCap);
            // Actually, for Main branches in Y-split, parent=0.2, child=0.15.
            // Starting at 0.2 and tapering to 0.15 is perfect.
            // For Side twigs, parent=0.2, child=0.02.
            // Starting at 0.2 is bad. Capping at 0.03 is better.
            
            // Refined Logic:
            // If this is a "Main Role" (Extension), use full parent radius.
            // If Side Role, cap it.
            // But we don't know "Role" here easily inside recursive call (lost context unless passed).
            // Let's rely on ratio.
            if (structuralRadius > parentTipRadius * 0.7f) effectiveStartRadius = parentTipRadius; // Main-ish branches connect seamlessly
            
            BioNode firstNode = new BioNode { position = pos, direction = dir, radius = effectiveStartRadius, depth = depth, rotation = startRot };
            BioNode current = firstNode;

            Vector3 curPos = pos;
            Vector3 curDir = dir; 
            occupiedSpace.Add(curPos);
            
            float segmentTaper = 0.98f; 

            for(int s=0; s<segments; s++)
            {
                Vector3 nextDirChoice = curDir;
                if (depth > 0)
                {
                    Vector3 bestCandDir = curDir;
                    float maxDistToOccupy = -1f;
                    int checkCount = (depth < 1) ? 1 : sensingSamples; 
                    for(int k=0; k<checkCount; k++)
                    {
                        float rX = (float)(rng.NextDouble()*2-1);
                        Vector3 randOffset = new Vector3(rX, (float)(rng.NextDouble()*2-1), (float)(rng.NextDouble()*2-1)) * noiseIntensity;
                        Vector3 candDir = (curDir + randOffset).normalized;
                        if(Vector3.Angle(candDir, Vector3.up) > maxVerticalAngle) candDir = Vector3.RotateTowards(Vector3.up, candDir, maxVerticalAngle * Mathf.Deg2Rad, 0f);

                        Vector3 predPos = curPos + candDir * segLen;
                        float minDistSq = float.MaxValue;
                        int occCount = occupiedSpace.Count;
                        if(occCount == 0) minDistSq = 100f;
                        else
                        {
                            int limit = Mathf.Max(0, occCount - 2);
                            for(int o=0; o<limit; o++) { float dSq = (occupiedSpace[o] - predPos).sqrMagnitude; if(dSq < minDistSq) minDistSq = dSq; }
                        }
                        if(minDistSq > maxDistToOccupy) { maxDistToOccupy = minDistSq; bestCandDir = candDir; }
                    }
                    nextDirChoice = Vector3.Lerp(curDir, bestCandDir, repulsionStrength).normalized;
                }
                
                if (depth < maxRecursion/2) nextDirChoice = Vector3.Lerp(nextDirChoice, Vector3.up, 0.1f * lengthDecay).normalized;

                Quaternion bend = Quaternion.FromToRotation(curDir, nextDirChoice);
                Quaternion nextRot = bend * current.rotation;
                Vector3 nextPos = curPos + nextDirChoice * segLen;
                occupiedSpace.Add(nextPos);
                
                // Taper Logic:
                // We want to reach 'structuralRadius' * decays.
                // Lerp from effectiveStartRadius to structuralRadius over the first segment?
                float progress = (float)(s+1)/segments;
                // If s=0 (end of first segment), we should be mostly at structuralRadius to avoid long bulge.
                float targetR = structuralRadius * Mathf.Pow(segmentTaper, s+1);
                
                // Smooth transition:
                // Segment 0: blended. Subsequent: pure logic.
                // Let's use a decay curve from startRadius.
                float blendFactor = Mathf.Clamp01(progress * 2f); // Fast transition (by middle of branch)
                // Actually, step artifacts happen at the JOINT. So we just need the start to match.
                // The loop automatically connects 'current' (start) to 'newNode' (end).
                // current.radius is startRadius.
                // newNode.radius = targetR.
                // So the first segment IS the collar.
                // We just generate nextRad normally.
                
                float nextRad = targetR;

                BioNode nextNode = new BioNode { position = nextPos, direction = nextDirChoice, radius = nextRad, depth = depth, rotation = nextRot };
                current.mainChild = nextNode;
                current = nextNode;
                curPos = nextPos;
                curDir = nextDirChoice;
            }

            // --- BRANCHING LOGIC ---
            float depthThreshold = 0.05f * (depth + 1);
            float localGrowth = Mathf.Clamp01((growthCycle - depthThreshold) * 5f); 
            
            if (localGrowth > 0.1f && depth < maxRecursion)
            {
                float baseNewLen = length * lengthDecay * localGrowth;
                
                Vector3 refRight = Vector3.Cross(curDir, Vector3.up);
                if(refRight.sqrMagnitude < 0.01f) refRight = Vector3.right;
                Quaternion roll = Quaternion.AngleAxis((float)rng.NextDouble() * 360f, curDir);
                Vector3 forkAxis = (roll * refRight).normalized;

                bool isTrifurcation = allowTrifurcation && (rng.NextDouble() < 0.5);

                List<BranchSpec> children = new List<BranchSpec>();

                if (isTrifurcation)
                {
                    Vector3 d1 = Vector3.Lerp(curDir, Vector3.up, 0.2f).normalized;
                    d1 = (d1 + RandomVector(rng)*noiseIntensity).normalized;
                    children.Add(new BranchSpec{ dir = d1, isMainRole = true });

                    Vector3 d2 = Quaternion.AngleAxis(branchingAngle, forkAxis) * curDir;
                    children.Add(new BranchSpec{ dir = d2, isMainRole = false });

                    Vector3 d3 = Quaternion.AngleAxis(-branchingAngle, forkAxis) * curDir;
                    children.Add(new BranchSpec{ dir = d3, isMainRole = false });
                }
                else
                {
                    Vector3 d1 = Quaternion.AngleAxis(branchingAngle, forkAxis) * curDir;
                    d1 = (d1 + RandomVector(rng)*noiseIntensity).normalized;
                    children.Add(new BranchSpec{ dir = d1, isMainRole = false });

                    Vector3 d2 = Quaternion.AngleAxis(-branchingAngle, forkAxis) * curDir;
                    d2 = (d2 + RandomVector(rng)*noiseIntensity).normalized;
                    children.Add(new BranchSpec{ dir = d2, isMainRole = false });
                }

                float[] weights = new float[children.Count];
                float totalWeight = 0f;
                for(int i=0; i<children.Count; i++) {
                     float angle = Vector3.Angle(curDir, children[i].dir);
                     float w = 1.0f / (1.0f + angle * 0.5f); 
                     weights[i] = w; totalWeight += w;
                }

                float parentArea = current.radius * current.radius;
                float tipRadius = current.radius; // Pass this to children as start point

                bool mainAssigned = false;
                for(int i=0; i<children.Count; i++)
                {
                    float normW = weights[i] / totalWeight;
                    float childArea = parentArea * normW;
                    float childStructRadius = Mathf.Sqrt(childArea);
                    childStructRadius *= 0.9f; 

                    int nextDepth = children[i].isMainRole ? depth : depth + 1;
                    bool nextAllow = !children[i].isMainRole; 

                    Quaternion nextRot = Quaternion.FromToRotation(curDir, children[i].dir) * current.rotation;
                    int childSeed = rng.Next();
                    
                    // Pass tipRadius as parentTipRadius
                    BioNode childNode = GenerateSkeletonNode(curPos, children[i].dir, baseNewLen, childStructRadius, nextDepth, childSeed, nextRot, nextAllow, tipRadius);
                    
                    if(childNode != null)
                    {
                        if (children[i].isMainRole) { current.mainChild = childNode; mainAssigned = true; }
                        else if (!mainAssigned && !isTrifurcation) { current.mainChild = childNode; mainAssigned = true; }
                        else { current.sideChildren.Add(childNode); }
                    }
                }
            }
            
            float depthFactor = (float)depth / maxRecursion;
            if (depthFactor >= (1f - leafTipRange) && leavesPerBranch > 0)
            {
                 float leafGrowthFactor = Mathf.Clamp01((growthCycle - 0.1f) / 0.9f);
                 if(leafGrowthFactor > 0.01f)
                 {
                     float currentLeafScale = Mathf.Lerp(1f, 5f, leafGrowthFactor) * leafScale;
                     Vector3 startL = firstNode.position; Vector3 endL = current.position;
                     for(int l=0; l<leavesPerBranch; l++)
                     {
                         float t = (float)rng.NextDouble();
                         Vector3 lPos = Vector3.Lerp(startL, endL, t);
                         float lRad = Mathf.Lerp(firstNode.radius, current.radius, t);
                         AddLeafClusterData(lPos, current.direction, lRad, currentLeafScale, rng.Next());
                     }
                 }
            }

            return firstNode;
        }

        private Vector3 RandomVector(System.Random r) { return new Vector3((float)r.NextDouble()-0.5f, (float)r.NextDouble()-0.5f, (float)r.NextDouble()-0.5f); }

        private void BuildLimbMesh(BioNode node)
        {
            if (node == null) return;
            GenerateRing(node); 
            BioNode w = node;
            while(w.mainChild != null)
            {
                BioNode next = w.mainChild;
                GenerateRing(next); 
                Dictionary<int, BioNode> indexToBranch = new Dictionary<int, BioNode>();
                HashSet<int> holeIndices = new HashSet<int>();
                foreach(var branch in w.sideChildren)
                {
                    int bestK = 0; float maxDot = -1f;
                    for(int k=0; k<radialSegments; k++) { Vector3 vDir = (verts[w.ringStartIndex + k] - w.position).normalized; float d = Vector3.Dot(vDir, branch.direction); if(d > maxDot) { maxDot = d; bestK = k; } }
                    if(!holeIndices.Contains(bestK)) { holeIndices.Add(bestK); indexToBranch[bestK] = branch; }
                }
                int baseA = w.ringStartIndex; int baseB = next.ringStartIndex; 
                for(int k=0; k<radialSegments; k++)
                {
                    if(holeIndices.Contains(k)) { BioNode branch = indexToBranch[k]; BuildLimbMesh(branch); BridgeHoleToBranch(baseA, baseB, k, branch); }
                    else { AddQuad(baseA+k, baseA+k+1, baseB+k+1, baseB+k); }
                }
                w = next;
            }
            CloseCap(w);
        }

        private void CloseCap(BioNode node)
        {
            int centerIdx = verts.Count; verts.Add(node.position + node.direction * (node.radius * 0.3f)); uvs.Add(new Vector2(0.5f, 1f)); 
            int baseIdx = node.ringStartIndex;
            for(int s=0; s<radialSegments; s++) AddTriangle(centerIdx, baseIdx + s + 1, baseIdx + s);
        }
        
        private void GenerateRing(BioNode node)
        {
            node.ringStartIndex = verts.Count;
            Quaternion rot = node.rotation;
            for(int s=0; s<=radialSegments; s++) { float angle = (float)s / radialSegments * Mathf.PI * 2f; verts.Add(node.position + rot * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * node.radius); uvs.Add(new Vector2((float)s/radialSegments, node.depth)); }
        }
        
        private void BridgeHoleToBranch(int baseIdxA, int baseIdxB, int k, BioNode branch)
        {
             int a1 = baseIdxA + k; int a2 = baseIdxA + k + 1; int b1 = baseIdxB + k; int b2 = baseIdxB + k + 1;
             int branchBase = branch.ringStartIndex; int segs = radialSegments;
             int bestOffset = 0; float minDistSq = float.MaxValue; Vector3 pA1 = verts[a1];
             for(int j=0; j<segs; j++) { float d = (verts[branchBase + j] - pA1).sqrMagnitude; if(d < minDistSq) { minDistSq = d; bestOffset = j; } }
             for(int i=0; i<segs; i++)
             {
                 int offsetI = (i + bestOffset) % segs; int nextOffsetI = (i + bestOffset + 1) % segs;
                 int c1 = branchBase + offsetI; int c2 = branchBase + nextOffsetI;
                 int side = (i * 4) / segs;
                 int p1, p2; if (side == 0) { p1 = a1; p2 = a2; } else if (side == 1) { p1 = a2; p2 = b2; } else if (side == 2) { p1 = b2; p2 = b1; } else { p1 = b1; p2 = a1; }
                 tris.Add(p1); tris.Add(c2); tris.Add(c1); if ((i % 3) == 2) { tris.Add(p1); tris.Add(p2); tris.Add(c2); }
             }
        }
        private void AddQuad(int a, int b, int c, int d) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(c); tris.Add(d); tris.Add(a); }
        private void AddTriangle(int a, int b, int c) { tris.Add(a); tris.Add(b); tris.Add(c); }

        private void AddLeafClusterData(Vector3 centerPos, Vector3 branchDir, float radius, float scale, int seed)
        {
             if(leafPrefab == null || leafInstances == null) return;
             MeshFilter mfStruct = leafPrefab.GetComponent<MeshFilter>();
             if (mfStruct == null) return;
             System.Random r = new System.Random(seed);
             float angle = (float)r.NextDouble() * 360f;
             Quaternion roll = Quaternion.AngleAxis(angle, branchDir);
             Vector3 refRight = Vector3.Cross(branchDir, Vector3.up); if (refRight.sqrMagnitude < 0.001f) refRight = Vector3.right;
             Vector3 surfaceNormal = (roll * refRight).normalized;
             leafInstances.Add(new CombineInstance() { mesh = mfStruct.sharedMesh, transform = Matrix4x4.TRS(centerPos + surfaceNormal * radius, Quaternion.LookRotation(surfaceNormal, branchDir) * Quaternion.Euler((float)r.NextDouble()*30f-15f, (float)r.NextDouble()*30f-15f, 0), Vector3.one*scale) });
        }

        private void BuildLeaves()
        {
            Transform t = transform.Find("Leaves");
            if(leafInstances == null || leafInstances.Count == 0) { if (t != null) t.GetComponent<Renderer>().enabled = false; return; }
            if(t == null) { t = new GameObject("Leaves").transform; t.SetParent(transform); t.localPosition = Vector3.zero; }
            MeshFilter mf = t.GetComponent<MeshFilter>(); if (mf == null) mf = t.gameObject.AddComponent<MeshFilter>();
            MeshRenderer mr = t.GetComponent<MeshRenderer>(); if (mr == null) mr = t.gameObject.AddComponent<MeshRenderer>();
            mr.enabled = true;
            Mesh lm = new Mesh(); lm.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            lm.CombineMeshes(leafInstances.ToArray(), true, true); lm.RecalculateNormals();
            mf.sharedMesh = lm; mr.sharedMaterial = leafMaterial ? leafMaterial : treeMaterial;
        }
    }
}
