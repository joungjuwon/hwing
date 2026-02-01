using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MysticForgeRuntime
{
    public class HW_BioTreeRuntime : MonoBehaviour
    {
        [Header("Growth Settings")]
        [Range(0f, 1f)] public float growthCycle = 1.0f;
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
        [Range(1, 15)] public int sensingSamples = 6;
        [Range(0f, 1f)] public float repulsionStrength = 0.5f;
        [Range(0f, 1f)] public float balancingStrength = 0.5f; // Bias growth toward empty XZ directions
        [Range(0f, 1f)] public float gravityStrength = 0.3f; // Thin/long branches droop downward

        [Header("Foliage")]
        public GameObject leafPrefab;
        public Material leafMaterial;
        [Range(0, 10)] public int leavesPerBranch = 5;
        public float leafScale = 1.0f;

        [Header("Texture & Material")]
        public Material treeMaterial;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        
        // --- CACHED MESH RESOURCES (Performance) ---
        private Mesh trunkMesh;
        private Mesh leavesMesh;

        private class BioNode
        {
            public Vector3 position;
            public Vector3 direction; 
            public Quaternion rotation; 
            public float radius;
            public int depth;      
            public int generation; 
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
        private List<Vector3> verts = new List<Vector3>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<int> tris = new List<int>();
        private List<CombineInstance> leafInstances = new List<CombineInstance>();
        private List<Vector3> occupiedSpace = new List<Vector3>();
        private List<Vector3> canopySpace = new List<Vector3>(); // Only leaf-bearing branches for directional balance

        private int radialSegments = 12;

        private void OnEnable()
        {
            InitializeMeshes();
        }

        private void InitializeMeshes()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            
            if (trunkMesh == null) { trunkMesh = new Mesh(); trunkMesh.name = "TrunkMesh"; trunkMesh.hideFlags = HideFlags.DontSave; }
            if (leavesMesh == null) { leavesMesh = new Mesh(); leavesMesh.name = "LeavesMesh"; leavesMesh.hideFlags = HideFlags.DontSave; }
            
            meshFilter.sharedMesh = trunkMesh;
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

#if UNITY_EDITOR
        private bool _isUpdateQueued = false;
        private void OnValidate()
        {
            // Debounce OnValidate to prevent freezes during slider dragging
            if (_isUpdateQueued) return;
            _isUpdateQueued = true;
            EditorApplication.delayCall += () => {
                _isUpdateQueued = false;
                if (this != null) GenerateTree();
            };
        }
#endif

        [ContextMenu("Generate")]
        public void GenerateTree()
        {
            InitializeMeshes();
            
            // USE A LOCAL SEED to avoid OnValidate feedback loops
            int masterSeed = (randomSeed == 0) ? GetHashCode() : randomSeed;
            Random.InitState(masterSeed);
            
            // CLEANUPS
            occupiedSpace.Clear();
            canopySpace.Clear();
            leafInstances.Clear();
            verts.Clear();
            uvs.Clear();
            tris.Clear();

            // SKELETON GENERATION
            float height = maxTrunkHeight * growthCycle;
            float rootThick = maxTrunkThickness * Mathf.Clamp01(growthCycle);
            
            rootNode = GenerateSkeletonNode(Vector3.zero, Vector3.up, height, rootThick, 0, 0, masterSeed, Quaternion.LookRotation(Vector3.up), true, rootThick);

            if (rootNode != null)
            {
                // TRUNK MESH
                BuildLimbMesh(rootNode);
                trunkMesh.Clear();
                trunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                trunkMesh.SetVertices(verts);
                trunkMesh.SetUVs(0, uvs);
                trunkMesh.SetTriangles(tris, 0);
                trunkMesh.RecalculateNormals();
                if (treeMaterial != null) meshRenderer.sharedMaterial = treeMaterial;
                
                // LEAF DATA COLLECTION
                int currentMaxGen = (int)(growthCycle * maxRecursion);
                if (growthCycle >= 0.99f) currentMaxGen = maxRecursion;

                if(leafPrefab != null)
                {
                    CollectLeafData(rootNode, masterSeed, currentMaxGen);
                    BuildCombinedLeaves();
                }
            }
        }

        private BioNode GenerateSkeletonNode(Vector3 pos, Vector3 dir, float length, float structuralRadius, int depth, int generation, int seed, Quaternion startRot, bool allowTrifurcation, float parentTipRadius)
        {
            if (generation >= maxRecursion || structuralRadius < 0.002f) return null;

            System.Random rng = new System.Random(seed);
            int segments = 3;
            float segLen = length / segments;
            
            // Radius Blending (Collar)
            float startRadiusCap = structuralRadius * 1.5f; 
            float effectiveStartRadius = Mathf.Min(parentTipRadius, startRadiusCap);
            if (structuralRadius > parentTipRadius * 0.7f) effectiveStartRadius = parentTipRadius; 
            
            BioNode firstNode = new BioNode { position = pos, direction = dir, radius = effectiveStartRadius, depth = depth, generation = generation, rotation = startRot };
            BioNode current = firstNode;

            Vector3 curPos = pos;
            Vector3 curDir = dir; 
            occupiedSpace.Add(curPos);
            
            for(int s=0; s<segments; s++)
            {
                Vector3 nextDirChoice = curDir;
                if (generation > 0)
                {
                    // Pure RNG-based direction (NO occupiedSpace here - that's only for NEW branch creation)
                    Vector3 randOffset = new Vector3((float)rng.NextDouble()*2-1, (float)rng.NextDouble()*2-1, (float)rng.NextDouble()*2-1) * noiseIntensity;
                    Vector3 candDir = (curDir + randOffset).normalized;
                    if(Vector3.Angle(candDir, Vector3.up) > maxVerticalAngle) candDir = Vector3.RotateTowards(Vector3.up, candDir, maxVerticalAngle * Mathf.Deg2Rad, 0f);
                    nextDirChoice = Vector3.Lerp(curDir, candDir, repulsionStrength).normalized;
                }
                
                if (generation < maxRecursion/2) nextDirChoice = Vector3.Lerp(nextDirChoice, Vector3.up, 0.1f * lengthDecay).normalized;

                // Gravity/Droop Effect: thin & high-gen branches curve down like a bow
                // Young trees (low growthCycle) have minimal droop since they're short and light
                if (generation > 0 && gravityStrength > 0.001f)
                {
                    // thinFactor: 0 when thick (radius = maxTrunkThickness), 1 when very thin
                    float thinFactor = 1f - Mathf.Clamp01(current.radius / maxTrunkThickness);
                    // progressFactor: 0 at branch start, 1 at branch end (segment s out of total segments)
                    float progressFactor = (float)(s + 1) / segments;
                    // genFactor: higher generations droop more
                    float genFactor = (float)generation / maxRecursion;
                    // growthFactor: young trees (growthCycle < 0.5) have almost no droop
                    float growthFactor = Mathf.Clamp01((growthCycle - 0.3f) * 2f); // 0 at cycle 0.3, 1 at cycle 0.8+
                    
                    float droopAmount = thinFactor * progressFactor * genFactor * growthFactor * gravityStrength * 0.5f;
                    nextDirChoice = Vector3.Lerp(nextDirChoice, Vector3.down, droopAmount).normalized;
                }

                Quaternion bend = Quaternion.FromToRotation(curDir, nextDirChoice);
                Quaternion nextRot = bend * current.rotation;
                Vector3 nextPos = curPos + nextDirChoice * segLen;
                occupiedSpace.Add(nextPos);
                
                // Add to canopy space if in leaf zone
                int leafLayers = Mathf.Clamp(maxRecursion - 1, 1, 3);
                int canopyThreshold = maxRecursion - leafLayers;
                if (generation >= canopyThreshold) canopySpace.Add(nextPos);
                
                float targetR = structuralRadius * Mathf.Pow(0.98f, s+1);
                BioNode nextNode = new BioNode { position = nextPos, direction = nextDirChoice, radius = targetR, depth = depth, generation = generation, rotation = nextRot };
                current.mainChild = nextNode;
                current = nextNode;
                curPos = nextPos;
                curDir = nextDirChoice;
            }

            // Growth progression with smoother interpolation
            float step = 1.0f / (maxRecursion + 1);
            float depthThreshold = generation * step;
            float rawGrowth = Mathf.Clamp01((growthCycle - depthThreshold) / (step * 0.8f)); // Spread over ~80% of step
            float localGrowth = rawGrowth * rawGrowth * (3f - 2f * rawGrowth); // Smoothstep for gradual easing 
            
            if (localGrowth > 0.05f && generation < maxRecursion)
            {
                float baseNewLen = length * lengthDecay * localGrowth;
                Vector3 refRight = Vector3.Cross(curDir, Vector3.up);
                if(refRight.sqrMagnitude < 0.01f) refRight = Vector3.right;
                Quaternion roll = Quaternion.AngleAxis((float)rng.NextDouble() * 360f, curDir);
                Vector3 forkAxis = (roll * refRight).normalized;

                bool isTrifurcation = allowTrifurcation && (rng.NextDouble() < 0.5);
                List<BranchSpec> specList = new List<BranchSpec>();
                
                // SEED-BASED DIRECTION: Generate candidates with noise, pick one using RNG (deterministic)
                // occupiedSpace is NOT used for selection - only seed determines the result

                if (isTrifurcation)
                {
                    specList.Add(new BranchSpec{ dir = Vector3.Lerp(curDir, Vector3.up, 0.2f).normalized, isMainRole = true });
                    
                    // Generate candidates and pick one randomly (seed-based)
                    Vector3 baseDir1 = Quaternion.AngleAxis(branchingAngle, forkAxis) * curDir;
                    Vector3 baseDir2 = Quaternion.AngleAxis(-branchingAngle, forkAxis) * curDir;
                    
                    // Pick candidate index using RNG
                    int pick1 = rng.Next(sensingSamples + 1);
                    int pick2 = rng.Next(sensingSamples + 1);
                    
                    Vector3 chosen1 = baseDir1, chosen2 = baseDir2;
                    for(int c=0; c<=sensingSamples; c++) {
                        Vector3 cand1 = (c == 0) ? baseDir1 : (baseDir1 + RandomVector(rng)*noiseIntensity*0.5f).normalized;
                        Vector3 cand2 = (c == 0) ? baseDir2 : (baseDir2 + RandomVector(rng)*noiseIntensity*0.5f).normalized;
                        if(c == pick1) chosen1 = cand1;
                        if(c == pick2) chosen2 = cand2;
                    }
                    specList.Add(new BranchSpec{ dir = chosen1, isMainRole = false });
                    specList.Add(new BranchSpec{ dir = chosen2, isMainRole = false });
                }
                else
                {
                    Vector3 baseDir1 = Quaternion.AngleAxis(branchingAngle, forkAxis) * curDir;
                    Vector3 baseDir2 = Quaternion.AngleAxis(-branchingAngle, forkAxis) * curDir;
                    
                    int pick1 = rng.Next(sensingSamples + 1);
                    int pick2 = rng.Next(sensingSamples + 1);
                    
                    Vector3 chosen1 = baseDir1, chosen2 = baseDir2;
                    for(int c=0; c<=sensingSamples; c++) {
                        Vector3 cand1 = (c == 0) ? baseDir1 : (baseDir1 + RandomVector(rng)*noiseIntensity).normalized;
                        Vector3 cand2 = (c == 0) ? baseDir2 : (baseDir2 + RandomVector(rng)*noiseIntensity).normalized;
                        if(c == pick1) chosen1 = cand1;
                        if(c == pick2) chosen2 = cand2;
                    }
                    specList.Add(new BranchSpec{ dir = chosen1, isMainRole = false });
                    specList.Add(new BranchSpec{ dir = chosen2, isMainRole = false });
                }

                float totalWeight = 0f;
                float[] weights = new float[specList.Count];
                for(int i=0; i<specList.Count; i++){
                    float w = 1.0f / (1.0f + Vector3.Angle(curDir, specList[i].dir) * 0.5f);
                    weights[i] = w; totalWeight += w;
                }

                float parentArea = current.radius * current.radius;
                
                // Calculate all child radii first
                float[] childRadii = new float[specList.Count];
                for(int i=0; i<specList.Count; i++){
                    float childArea = parentArea * (weights[i] / totalWeight);
                    childRadii[i] = Mathf.Sqrt(childArea) * 0.9f;
                }
                
                // Near trunk: balance radii so thinner branch approaches 65% of thicker
                // genFactor: 1 at trunk (gen 0), 0 at max recursion
                float genFactor = 1f - ((float)generation / maxRecursion);
                float balanceTarget = 0.65f; // Thinner becomes this fraction of thicker at most
                
                if(specList.Count >= 2) {
                    float maxR = Mathf.Max(childRadii[0], childRadii.Length > 1 ? childRadii[1] : 0);
                    for(int i=0; i<specList.Count; i++) {
                        if(!specList[i].isMainRole) {
                            float minAllowed = maxR * Mathf.Lerp(0f, balanceTarget, genFactor);
                            childRadii[i] = Mathf.Max(childRadii[i], minAllowed);
                        }
                    }
                }
                
                bool mainAssigned = false;
                for(int i=0; i<specList.Count; i++)
                {
                    // Smooth interpolation: new branches start thin and grow to full radius
                    float childR = childRadii[i] * Mathf.Lerp(0.1f, 1f, localGrowth);
                    int nDepth = specList[i].isMainRole ? depth : depth + 1;
                    
                    // DETERMINISTIC SEED: Use parent seed + child index to ensure structure stability
                    int childSeed = seed * 31 + i + generation * 7919;
                    
                    BioNode childNode = GenerateSkeletonNode(curPos, specList[i].dir, baseNewLen, childR, nDepth, generation + 1, childSeed, 
                        Quaternion.FromToRotation(curDir, specList[i].dir) * current.rotation, !specList[i].isMainRole, current.radius);
                    
                    if(childNode != null)
                    {
                        if(specList[i].isMainRole || (!mainAssigned && !isTrifurcation)) { current.mainChild = childNode; mainAssigned = true; }
                        else current.sideChildren.Add(childNode);
                    }
                }
            }
            return firstNode;
        }

        private void CollectLeafData(BioNode node, int seed, int currentMaxGen)
        {
            if (node == null || leafPrefab == null) return;
            MeshFilter mfStruct = leafPrefab.GetComponent<MeshFilter>();
            if (mfStruct == null) return;
            
            System.Random rng = new System.Random(seed);
            
            BioNode w = node;
            while(w.mainChild != null)
            {
                foreach(var c in w.sideChildren) CollectLeafData(c, rng.Next(), currentMaxGen);
                
                // Leaf Generation Rule
                // leafLayers: how many layers from top get leaves, Clamp(max-1, 1, 3)
                // Max 7: layers = Clamp(6,1,3) = 3 → leaves on top 3
                // Max 2: layers = Clamp(1,1,3) = 1 → leaves on top 1
                int leafLayers = Mathf.Clamp(maxRecursion - 1, 1, 3);
                int startThreshold = maxRecursion - leafLayers;
                
                bool isThin = w.radius < (maxTrunkThickness * 0.05f);
                bool isCanopy = (w.generation >= startThreshold);
                
                if (isThin || isCanopy) AddLeafInstances(w, w.mainChild, mfStruct.sharedMesh, rng);
                w = w.mainChild;
            }
            AddLeafInstances(w, null, mfStruct.sharedMesh, rng);
        }

        private void AddLeafInstances(BioNode startNode, BioNode endNode, Mesh leafMesh, System.Random rng)
        {
             if(leavesPerBranch <= 0 || leafInstances.Count > 10000) return;
             if(endNode == null) endNode = startNode; 

             float growthFactor = Mathf.Clamp01((growthCycle - 0.05f) / 0.95f);
             if(growthFactor <= 0.001f) return;

             float currentScale = Mathf.Lerp(0.1f, 1.0f, growthFactor) * leafScale; 

             for(int l=0; l<leavesPerBranch; l++)
             {
                 float t = (float)rng.NextDouble();
                 Vector3 lPos = Vector3.Lerp(startNode.position, endNode.position, t);
                 Vector3 surfNorm = (Quaternion.AngleAxis((float)rng.NextDouble()*360f, startNode.direction) * Vector3.up).normalized;
                 
                 Matrix4x4 m = Matrix4x4.TRS(lPos + surfNorm * startNode.radius, 
                     Quaternion.LookRotation(surfNorm, startNode.direction) * Quaternion.Euler((float)rng.NextDouble()*30f-15f, (float)rng.NextDouble()*30f-15f, 0), 
                     Vector3.one * currentScale);
                 
                 leafInstances.Add(new CombineInstance { mesh = leafMesh, transform = m });
             }
        }
        
        private void BuildCombinedLeaves()
        {
            Transform leavesRoot = transform.Find("Leaves");
            if (leavesRoot == null) {
                leavesRoot = new GameObject("Leaves").transform;
                leavesRoot.SetParent(transform);
                leavesRoot.localPosition = Vector3.zero;
                leavesRoot.localRotation = Quaternion.identity;
                leavesRoot.localScale = Vector3.one;
            } 

            // Clear legacy children immediately if switching modes
            while(leavesRoot.childCount > 0) {
                 if(Application.isPlaying) Destroy(leavesRoot.GetChild(0).gameObject);
                 else DestroyImmediate(leavesRoot.GetChild(0).gameObject);
            }

            leavesMesh.Clear();
            if (leafInstances.Count == 0) return;

            leavesMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            leavesMesh.CombineMeshes(leafInstances.ToArray(), true, true);
            leavesMesh.RecalculateNormals();
            
            MeshFilter mf = leavesRoot.GetComponent<MeshFilter>();
            if (mf == null) mf = leavesRoot.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = leavesMesh;

            MeshRenderer mr = leavesRoot.GetComponent<MeshRenderer>();
            if (mr == null) mr = leavesRoot.gameObject.AddComponent<MeshRenderer>();
            if (leafMaterial != null) mr.sharedMaterial = leafMaterial;
            else mr.sharedMaterial = treeMaterial;
        }

        private void BuildLimbMesh(BioNode node)
        {
            if (node == null) return;
            GenerateRing(node); 
            BioNode w = node;
            while(w.mainChild != null)
            {
                BioNode next = w.mainChild;
                GenerateRing(next); 
                foreach(var branch in w.sideChildren)
                {
                    int bestK = 0; float maxDot = -1f;
                    for(int k=0; k<radialSegments; k++) { 
                         float d = Vector3.Dot((verts[w.ringStartIndex + k] - w.position).normalized, branch.direction); 
                         if(d > maxDot) { maxDot = d; bestK = k; } 
                    }
                    BuildLimbMesh(branch); 
                    BridgeHoleToBranch(w.ringStartIndex, next.ringStartIndex, bestK, branch); 
                }
                int baseA = w.ringStartIndex; int baseB = next.ringStartIndex; 
                for(int k=0; k<radialSegments; k++) AddQuad(baseA+k, baseA+k+1, baseB+k+1, baseB+k);
                w = next;
            }
            CloseCap(w);
        }

        private void CloseCap(BioNode node)
        {
            int centerIdx = verts.Count; 
            verts.Add(node.position + node.direction * (node.radius * 0.2f)); 
            uvs.Add(new Vector2(0.5f, 1f)); 
            for(int s=0; s<radialSegments; s++) { 
                tris.Add(centerIdx); 
                tris.Add(node.ringStartIndex + s + 1); 
                tris.Add(node.ringStartIndex + s);
            }
        }
        
        private void GenerateRing(BioNode node)
        {
            node.ringStartIndex = verts.Count;
            Quaternion rot = node.rotation;
            for(int s=0; s<=radialSegments; s++) { 
                 float a = (float)s / radialSegments * Mathf.PI * 2f; 
                 verts.Add(node.position + rot * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * node.radius); 
                 uvs.Add(new Vector2((float)s/radialSegments, (float)node.generation)); 
            }
        }
        
        private void BridgeHoleToBranch(int baseIdxA, int baseIdxB, int k, BioNode branch)
        {
             int a1 = baseIdxA + k; int branchBase = branch.ringStartIndex;
             int bestOffset = 0; float minDistSq = 1000f;
             for(int j=0; j<radialSegments; j++) { 
                  float d = (verts[branchBase + j] - verts[a1]).sqrMagnitude; 
                  if(d < minDistSq) { minDistSq = d; bestOffset = j; } 
             }
             for(int i=0; i<radialSegments; i++)
             {
                 int c1 = branchBase + (i + bestOffset) % radialSegments;
                 int c2 = branchBase + (i + bestOffset + 1) % radialSegments;
                 tris.Add(a1); tris.Add(c2); tris.Add(c1);
             }
        }
        private void AddQuad(int a, int b, int c, int d) { tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(c); tris.Add(d); tris.Add(a); }
        private Vector3 RandomVector(System.Random r) { return new Vector3((float)r.NextDouble()-0.5f, (float)r.NextDouble()-0.5f, (float)r.NextDouble()-0.5f); }
    }
}
