using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MysticForgeRuntime
{
    public class HW_BioTreeRuntime : MonoBehaviour
    {
        [Header("Preset")]
        public HW_BioTreePreset parameterPreset;
        public bool usePresetParameters = true;

        [Header("Seed")]
        [Tooltip("When enabled, a new random seed is generated whenever GenerateTree() runs.")]
        public bool randomizeSeedOnGenerate = true;
        public int randomSeed = 0;

        [Header("Growth Settings")]
        [Range(0f, 1f)] public float growthCycle = 1.0f;
        [FormerlySerializedAs("growthSpeed")]
        [Tooltip("Seconds required for growthCycle to go from 0 to 1 when autoGrow is enabled.")]
        [Min(0.01f)] public float secondsToFullGrowth = 2f;
        public bool autoGrow = false;
        
        [Header("Tree Parameters")]
        public float maxTrunkHeight = 5f;
        public float maxTrunkThickness = 0.2f;
        [Range(1, 8)] public int maxRecursion = 5;
        
        [Header("Branching Rules")]
        [Range(0.5f, 0.99f)] public float lengthDecay = 0.8f;

        [Range(10f, 90f)] public float branchingAngle = 35f;

        [Range(-1f, 1f)] public float noiseIntensity = 0.2f;
        [Range(0f, 1f)] public float lengthRandomness = 0.2f;
        [Range(45f, 160f)] public float maxVerticalAngle = 100f; 

        [Header("Space Filling (Volumetric)")]
        [Range(1, 15)] public int sensingSamples = 6;
        [Range(0f, 1f)] public float repulsionStrength = 0.5f;

        [Range(0f, 1f)] public float gravityStrength = 0.3f; // Thin/long branches droop downward

        [Header("Foliage")]
        public GameObject leafPrefab;
        public Material leafMaterial;
        [Range(0, 10)] public int leavesPerBranch = 5;
        public float leafScale = 1.0f;

        [Header("Texture & Material")]
        public Material treeMaterial;

        // Skinned Mesh Components
        private SkinnedMeshRenderer skinnedMeshRenderer;
        
        // --- CACHED MESH RESOURCES (Performance) ---
        private Mesh treeMesh; // Combined mesh for trunk + leaves

        // --- SKELETAL DATA ---
        private List<Transform> bones = new List<Transform>();
        private List<Matrix4x4> bindPoses = new List<Matrix4x4>();
        private List<BoneWeight> boneWeights = new List<BoneWeight>();
        // New: Stiffness for sway
        private List<float> stiffnessList = new List<float>();
        // New: Birth time for growth animation (0..1 normalized depth)
        private List<float> birthTimeList = new List<float>();

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
            public float vCoord; // Accumulated texture V coordinate
            
            // Skeletal Info
            public int boneIndex = -1;
            public Transform boneRef;
            public BioNode parent; // Reference to parent for hierarchy building
        }

        private struct BranchSpec 
        {
            public Vector3 dir;
            public bool isMainRole; 
        }

        private BioNode rootNode;
        private List<Vector3> verts = new List<Vector3>();
        private List<Vector2> uvs = new List<Vector2>();
        
        // Submesh Triangles
        private List<int> trunkTris = new List<int>();
        private List<int> foliageTris = new List<int>();
        
        // Helper lists to merge leaf meshes
        private List<Vector3> leafVerts = new List<Vector3>();
        private List<int> leafTris = new List<int>();
        private List<Vector2> leafUVs = new List<Vector2>();
        
        private int radialSegments = 12;

        private void OnEnable()
        {
            ApplyPresetParametersInternal();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Clean up old static mesh filters if they exist (migration)
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null) {
                if(Application.isPlaying) Destroy(mf); else DestroyImmediate(mf);
            }
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null) {
                if(Application.isPlaying) Destroy(mr); else DestroyImmediate(mr);
            }

            // Ensure SkinnedMeshRenderer
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null) skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            ConfigureTreeRenderer(skinnedMeshRenderer);
            
            if (treeMesh == null) { 
                treeMesh = new Mesh(); 
                treeMesh.name = "ProceduralTreeMesh"; 
                treeMesh.hideFlags = HideFlags.DontSave; 
            }
        }

        private void Update()
        {
            if (autoGrow)
            {
                float duration = Mathf.Max(0.01f, secondsToFullGrowth);
                growthCycle += Time.deltaTime / duration;
                if (growthCycle > 1f) growthCycle = 1f;
                // No longer calls GenerateTree()! Just updates transforms.
                UpdateGrowth();
            }
            // If in editor and growthCycle changed via slider, we also want to update visual without regenerating mesh
            // But OnValidate handles that logic usually.
        }

        // Optimized Growth Update: Scales bones instead of rebuilding mesh
        private void UpdateGrowth()
        {
            if (bones == null || bones.Count == 0 || birthTimeList.Count != bones.Count) return;

            // We want the whole tree to finish growing when growthCycle = 1.
            // birthTime is 0..1. 
            
            for(int i=0; i<bones.Count; i++)
            {
                Transform bone = bones[i];
                if (bone == null) continue;

                float birthTime = birthTimeList[i];
                
                // If growthCycle < birthTime, bone is effectively invisible (scale 0)
                // We use a small window for transition "pop" or smooth scale
                
                float age = growthCycle - birthTime;
                
                // Local Growth logic: 
                // We want branches to ELONGATE first (Y axis), then THICKEN (X/Z axis).
                // Let's define a "growth duration" for each segment.
                float segmentDuration = 0.3f; // Overlap for smoothness
                float normalizedAge = Mathf.Clamp01(age / segmentDuration);

                if (normalizedAge <= 0.001f)
                {
                    bone.localScale = Vector3.zero;
                    continue;
                }

                // Smart Scaling:
                // Lengthen first: 0 -> 1 over first 60%
                // Thicken second: 0 -> 1 start at 30% -> 100%
                
                float lengthScale = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedAge * 1.5f)); 
                float thickScale = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((normalizedAge - 0.2f) * 1.25f));
                
                // If this is a leaf bone (logic: usually high generation), we might want simpler scaling?
                // Actually leaves are merged into the mesh, but attached to bones.
                // If the bone scales 0, the leaves attached to it (weighted) will collapse to the parent joint.
                // However, leaf scales are baked into the mesh.
                // If the bone is scaled 0, the leaf geometry implicitly scales to 0 IF it's fully weighted to this bone.
                // Our leaves are weighted to 'startNode.boneIndex'. 
                // So if startNode bone scales to 0, leaf scales to 0. Correct.
                
                bone.localScale = new Vector3(thickScale, lengthScale, thickScale);
            }
        }

#if UNITY_EDITOR
        private bool _isUpdateQueued = false;
        private void OnValidate()
        {
            // Prevent running on Prefab Assets (Project View)
            if (PrefabUtility.IsPartOfPrefabAsset(this)) return;

            // Debounce OnValidate to prevent freezes during slider dragging
            if (_isUpdateQueued) return;
            _isUpdateQueued = true;
            EditorApplication.delayCall += () => {
                _isUpdateQueued = false;
                // If we are just changing growthCycle, maybe we can just UpdateGrowth?
                // But for safety during parameter tuning, we Regenerate if structure might change.
                // For now, let's keep full regeneration on manual change to ensure consistency, 
                // OR we can detect if only growthCycle changed. 
                // Let's just Regenerate to be safe in Editor, but Animation uses Update().
                if (this != null && !PrefabUtility.IsPartOfPrefabAsset(this)) 
                {
                    // If playing, we assume structure is baked and we just want to update growth?
                    // Actually if we change Seed/MaxRecursion, we MUST regenerate.
                    GenerateTree(); 
                    if(Application.isPlaying) UpdateGrowth(); // Apply immediate growth pose
                }
            };
        }
#endif

        public void SetPreset(HW_BioTreePreset preset, bool regenerateTree = true)
        {
            parameterPreset = preset;
            usePresetParameters = parameterPreset != null;
            ApplyPresetParametersInternal(forceApply: true);

            if (regenerateTree)
            {
                GenerateTree();
            }
        }

        [ContextMenu("Apply Preset Parameters")]
        public void ApplyPresetParameters()
        {
            ApplyPresetParametersInternal(forceApply: true);
        }

        [ContextMenu("Export Current Parameters To Assigned Preset")]
        public void ExportCurrentParametersToAssignedPreset()
        {
            if (parameterPreset == null) return;

            parameterPreset.CaptureFrom(this);
#if UNITY_EDITOR
            EditorUtility.SetDirty(parameterPreset);
            AssetDatabase.SaveAssets();
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Create Preset Asset From Current Parameters")]
        public void CreatePresetAssetFromCurrentParameters()
        {
            HW_BioTreePreset newPreset = ScriptableObject.CreateInstance<HW_BioTreePreset>();
            newPreset.CaptureFrom(this);

            string defaultName = $"{gameObject.name}_BioTreePreset.asset";
            string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/_project/Scripts/MysticForgeRuntime/{defaultName}");
            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            parameterPreset = newPreset;
            usePresetParameters = true;
            EditorUtility.SetDirty(this);

            Selection.activeObject = newPreset;
            Debug.Log($"Created preset asset: {path}", this);
        }
#endif

        private bool ApplyPresetParametersInternal(bool forceApply = false)
        {
            if (parameterPreset == null) return false;
            if (!usePresetParameters && !forceApply) return false;

            parameterPreset.ApplyTo(this);
            return true;
        }

        [ContextMenu("Generate")]
        public void GenerateTree()
        {
#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabAsset(this)) return;
#endif
            ApplyPresetParametersInternal();
            InitializeComponents();
            
            int masterSeed = ResolveMasterSeed();
            
            // CLEANUPS
            verts.Clear();
            uvs.Clear();
            trunkTris.Clear();
            foliageTris.Clear();
            boneWeights.Clear();
            bones.Clear();
            bindPoses.Clear();
            stiffnessList.Clear();
            birthTimeList.Clear(); // Clear birth times
            
            // Clear old bone hierarchy
            Transform existingRoot = transform.Find("RootBone");
            if (existingRoot != null)
            {
                if(Application.isPlaying) Destroy(existingRoot.gameObject);
                else DestroyImmediate(existingRoot.gameObject);
            }

            // Clear Leaves container (legacy capability)
            Transform existingLeaves = transform.Find("Leaves");
            if(existingLeaves != null)
            {
                 if(Application.isPlaying) Destroy(existingLeaves.gameObject);
                 else DestroyImmediate(existingLeaves.gameObject);
            }

            // SKELETON DATA GENERATION - FULL SIZE always
            // We ignore growthCycle for structural size calc. 
            // But we keep height/thickness params.
            float targetHeight = maxTrunkHeight; // Full height
            float targetThick = maxTrunkThickness; // Full thickness
            
            rootNode = GenerateSkeletonNode(Vector3.zero, Vector3.up, targetHeight, targetThick, 0, 0, masterSeed, Quaternion.LookRotation(Vector3.up), true, targetThick, null, 0f, null);

            if (rootNode != null)
            {
                // 1. CREATE BONE HIERARCHY (GameObjects)
                CreateBoneHierarchy(rootNode, null);

                // 2. TRUNK MESH GENERATION (Skinned)
                BuildLimbMesh(rootNode); // This now populates boneWeights too
                
                // 3. LEAF GENERATION (Merged Skinned)
                // Always generate MAX foliage for baked mesh
                if(leafPrefab != null)
                {
                    CollectAndMergeLeaves(rootNode, masterSeed, maxRecursion);
                }

                // 4. APPLY TO SKINNED MESH
                treeMesh.Clear();
                treeMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                treeMesh.SetVertices(verts);
                treeMesh.SetUVs(0, uvs);
                treeMesh.boneWeights = boneWeights.ToArray();
                treeMesh.bindposes = bindPoses.ToArray();
                
                // Submeshes: 0 = Trunk, 1 = Foliage
                treeMesh.subMeshCount = 2;
                treeMesh.SetTriangles(trunkTris, 0);
                treeMesh.SetTriangles(foliageTris, 1);
                
                treeMesh.RecalculateNormals();
                
                skinnedMeshRenderer.sharedMesh = treeMesh;
                skinnedMeshRenderer.bones = bones.ToArray();
                skinnedMeshRenderer.rootBone = bones.Count > 0 ? bones[0] : null;

                // Assign Materials
                Material[] mats = new Material[2];
                mats[0] = treeMaterial;
                mats[1] = leafMaterial;
                skinnedMeshRenderer.sharedMaterials = mats;
                ConfigureTreeRenderer(skinnedMeshRenderer);
                
                // 5. ANIMATION BINDING
                HW_ProceduralSway sway = GetComponent<HW_ProceduralSway>();
                if(sway == null) sway = gameObject.AddComponent<HW_ProceduralSway>();
                sway.BindBones(bones, stiffnessList);
                
                // Initial Pose Update
                UpdateGrowth();
            }
        }
        
        // ... (ConfigureTreeRenderer, GenerateSkeletonNode are same but need modification for growthCycle removal)
        // I will target them in next chunks.
        
        private static void ConfigureTreeRenderer(Renderer renderer)
        {
            if (renderer == null) return;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            renderer.receiveShadows = true;
        }

        private BioNode GenerateSkeletonNode(Vector3 pos, Vector3 dir, float length, float structuralRadius, int depth, int generation, int seed, Quaternion startRot, bool allowTrifurcation, float parentTipRadius, List<Vector3> avoidDirs, float vStart, BioNode parent)
        {
            if (generation >= maxRecursion || structuralRadius < 0.002f) return null;

            System.Random rng = new System.Random(seed);
            int segments = 3;
            float segLen = length / segments;
            
            // Radius Blending (Collar)
            float startRadiusCap = structuralRadius * 1.5f; 
            float effectiveStartRadius = Mathf.Min(parentTipRadius, startRadiusCap);
            if (structuralRadius > parentTipRadius * 0.7f) effectiveStartRadius = parentTipRadius; 
            
            BioNode firstNode = new BioNode { position = pos, direction = dir, radius = effectiveStartRadius, depth = depth, generation = generation, rotation = startRot, vCoord = vStart, parent = parent };
            BioNode current = firstNode;

            Vector3 curPos = pos;
            Vector3 curDir = dir; 

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

                // Gravity/Droop Effect
                if (generation > 0 && gravityStrength > 0.001f)
                {
                    float thinFactor = 1f - Mathf.Clamp01(current.radius / maxTrunkThickness);
                    float progressFactor = (float)(s + 1) / segments;
                    float genFactor = (float)generation / maxRecursion;
                    // FORCE FULL GROWTH for bake
                    float growthFactor = 1.0f; 
                    
                    float droopAmount = thinFactor * progressFactor * genFactor * growthFactor * gravityStrength * 0.5f;
                    nextDirChoice = Vector3.Lerp(nextDirChoice, Vector3.down, droopAmount).normalized;
                }

                Quaternion bend = Quaternion.FromToRotation(curDir, nextDirChoice);
                Quaternion nextRot = bend * current.rotation;
                Vector3 nextPos = curPos + nextDirChoice * segLen;

                float targetR = structuralRadius * Mathf.Pow(0.98f, s+1);
                float nextV = vStart + (s + 1) * segLen; 
                
                BioNode nextNode = new BioNode { position = nextPos, direction = nextDirChoice, radius = targetR, depth = depth, generation = generation, rotation = nextRot, vCoord = nextV, parent = current };
                current.mainChild = nextNode;
                current = nextNode;
                curPos = nextPos;
                curDir = nextDirChoice;
            }

            // Growth progression
            // FORCE FULL GROWTH for bake
            float localGrowth = 1.0f;
            
            if (localGrowth > 0.05f && generation < maxRecursion)
            {
                float randomFactor = 1.0f + ((float)rng.NextDouble() * 2f - 1f) * lengthRandomness;
                float baseNewLen = length * lengthDecay * localGrowth * randomFactor;
                Vector3 refRight = Vector3.Cross(curDir, Vector3.up);
                if(refRight.sqrMagnitude < 0.01f) refRight = Vector3.right;
                
                Quaternion rollRot = Quaternion.AngleAxis((float)rng.NextDouble() * 360f, curDir);
                Vector3 forkAxis = (rollRot * refRight).normalized;

                // Avoidance Logic
                if (avoidDirs != null && avoidDirs.Count > 0)
                {
                    for(int attempt=0; attempt<36; attempt++) 
                    {
                        Vector3 d1 = Quaternion.AngleAxis(branchingAngle, forkAxis) * curDir;
                        Vector3 d2 = Quaternion.AngleAxis(-branchingAngle, forkAxis) * curDir;
                        
                        bool conflict = false;
                        foreach(var av in avoidDirs) {
                            Vector3 avProj = Vector3.ProjectOnPlane(av, curDir).normalized;
                            Vector3 d1Proj = Vector3.ProjectOnPlane(d1, curDir).normalized;
                            Vector3 d2Proj = Vector3.ProjectOnPlane(d2, curDir).normalized;
                            
                            if(Vector3.Angle(avProj, d1Proj) < 15f || Vector3.Angle(avProj, d2Proj) < 15f) {
                                conflict = true; break;
                            }
                        }
                        if(!conflict) break;
                        rollRot = Quaternion.AngleAxis(10f, curDir) * rollRot;
                        forkAxis = (rollRot * refRight).normalized;
                    }
                }

                bool isTrifurcation = allowTrifurcation && (rng.NextDouble() < 0.5);
                List<BranchSpec> specList = new List<BranchSpec>();
                
                if (isTrifurcation)
                {
                    specList.Add(new BranchSpec{ dir = Vector3.Lerp(curDir, Vector3.up, 0.2f).normalized, isMainRole = true });
                    
                    Vector3 baseDir1 = Quaternion.AngleAxis(branchingAngle, forkAxis) * curDir;
                    Vector3 baseDir2 = Quaternion.AngleAxis(-branchingAngle, forkAxis) * curDir;
                    
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
                float[] childRadii = new float[specList.Count];
                for(int i=0; i<specList.Count; i++){
                    float childArea = parentArea * (weights[i] / totalWeight);
                    childRadii[i] = Mathf.Sqrt(childArea) * 0.9f;
                }
                
                float genFactor = 1f - ((float)generation / maxRecursion);
                float balanceTarget = 0.65f; 
                
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
                    float childR = childRadii[i] * Mathf.Lerp(0.1f, 1f, localGrowth);
                    int nDepth = specList[i].isMainRole ? depth : depth + 1;
                    int childSeed = seed * 31 + i + generation * 7919;
                    
                    List<Vector3> nextAvoidDirs = null;
                    if (isTrifurcation && specList[i].isMainRole) {
                        nextAvoidDirs = new List<Vector3>();
                        foreach(var s in specList) if(!s.isMainRole) nextAvoidDirs.Add(s.dir);
                    }

                    BioNode childNode = GenerateSkeletonNode(curPos, specList[i].dir, baseNewLen, childR, nDepth, generation + 1, childSeed, 
                        Quaternion.FromToRotation(curDir, specList[i].dir) * current.rotation, !specList[i].isMainRole, current.radius, nextAvoidDirs, current.vCoord, current);
                    
                    if(childNode != null)
                    {
                        if(specList[i].isMainRole || (!mainAssigned && !isTrifurcation)) { current.mainChild = childNode; mainAssigned = true; }
                        else current.sideChildren.Add(childNode);
                    }
                }
            }
            return firstNode;
        }
        
        // --- SKELETON HIERARCHY ---
        private void CreateBoneHierarchy(BioNode node, Transform parentInfo)
        {
            if (node == null) return;
            
            // Create bone GameObject
            string boneName = (node.parent == null) ? "RootBone" : $"Bone_Gen{node.generation}_D{node.depth}";
            GameObject boneGO = new GameObject(boneName);
            Transform boneT = boneGO.transform;
            
            // Parent to correct hierarchy
            if (parentInfo != null)
                boneT.SetParent(parentInfo);
            else
                boneT.SetParent(this.transform);

            // FIX: node.position is Model Space (Absolute from tree root).
            // Parenting adds transform. We must set position correctly.
            // Simplest way: Transform Model Space -> World Space, set absolute position.
            boneT.position = transform.TransformPoint(node.position);
            
            // Rotation is also absolute model space accumulation
            // So we combine with tree rotation
            boneT.rotation = transform.rotation * node.rotation;
            
            // Register Data
            node.boneRef = boneT;
            node.boneIndex = bones.Count;
            bones.Add(boneT);
            
            // Calculate BindPose
            bindPoses.Add(boneT.worldToLocalMatrix * transform.localToWorldMatrix);

            // Calculate Stiffness
            // Normalize radius against max thickness. Clamp 0..1
            // Radius 0 = Stiffness 0 (Flexible)
            // Radius Max = Stiffness 0.8-1 (Rigid)
            float stiff = Mathf.Clamp01(node.radius / (maxTrunkThickness + 0.001f));
            stiffnessList.Add(stiff);

            // Calculate Birth Time (0..1)
            // Based on generation and depth step
            // Simple linear progression:
            float totalSteps = maxRecursion + 1f;
            float birthTime = (float)node.generation / totalSteps;
            // Add slight offset for segments within a branch if depth matters? 
            // node.depth tracks depth from root, but generation tracks branching level.
            // Using generation is safer for main flow.
            birthTimeList.Add(birthTime);

            // Traverse children
            if (node.mainChild != null)
            {
                 CreateBoneHierarchy(node.mainChild, boneT);
            }
            foreach (var side in node.sideChildren)
            {
                CreateBoneHierarchy(side, boneT);
            }
        }

        // --- MESH GENERATION SKINNED ---

        private void BuildLimbMesh(BioNode node)
        {
            if (node == null) return;
            
            // Tapering Pass
            BioNode temp = node;
            int totalSegments = 0;
            while(temp != null) { totalSegments++; temp = temp.mainChild; }
            
            int segmentIndex = 0;
            BioNode w = node;
            float startRadius = node.radius;
            
            // Apply Taper
            float t = (float)segmentIndex / totalSegments;
            float taperFactor = Mathf.Sqrt(1f - t); 
            w.radius = startRadius * Mathf.Max(taperFactor, 0.01f);
            
            // FIX SEAM CRACKING:
            // If this node is a side child (branch), its start ring matches the parent surface.
            // To prevent looking like a crack when the branch rotates, 
            // the start ring must follow the PARENT's bone (weld to parent).
            int startRingBone = w.boneIndex;
            if (node.parent != null && node.parent.sideChildren.Contains(node))
            {
                startRingBone = node.parent.boneIndex;
            }

            GenerateRing(w, startRingBone); 
            
            while(w.mainChild != null)
            {
                BioNode next = w.mainChild;
                segmentIndex++;
                
                t = (float)segmentIndex / totalSegments;
                taperFactor = Mathf.Sqrt(1f - t);
                float originalRadius = next.radius;
                next.radius = originalRadius * Mathf.Max(taperFactor, 0.01f);
                
                GenerateRing(next);
                
                next.radius = originalRadius; // Restore
                
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
            float tipRadius = Mathf.Max(node.radius * 0.01f, 0.001f); 
            Vector3 tipPos = node.position + node.direction * node.radius; 
            
            int tipRingStart = verts.Count;
            Vector3 arbitraryUp = (Mathf.Abs(Vector3.Dot(node.direction, Vector3.up)) < 0.99f) ? Vector3.up : Vector3.forward;
            Vector3 tipRight = Vector3.Cross(node.direction, arbitraryUp).normalized;
            
            // Tip Ring
            for(int s=0; s<=radialSegments; s++) {
                float angle = (float)s / radialSegments * Mathf.PI * 2f;
                Vector3 offset = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, node.direction) * tipRight * tipRadius;
                verts.Add(tipPos + offset);
                uvs.Add(new Vector2((float)s / radialSegments, 1f));
                
                // Weight to tip bone
                AddBoneWeight(node.boneIndex);
            }
            
            for(int k=0; k<radialSegments; k++) AddQuad(node.ringStartIndex+k, node.ringStartIndex+k+1, tipRingStart+k+1, tipRingStart+k);
            
            // Center Point
            int centerIdx = verts.Count; 
            verts.Add(tipPos + node.direction * tipRadius); 
            uvs.Add(new Vector2(0.5f, 1f)); 
            AddBoneWeight(node.boneIndex);
            
            for(int s=0; s<radialSegments; s++) { 
                trunkTris.Add(centerIdx); 
                trunkTris.Add(tipRingStart + s + 1); 
                trunkTris.Add(tipRingStart + s);
            }
        }
        
        private void GenerateRing(BioNode node, int overrideBoneIndex = -1)
        {
            node.ringStartIndex = verts.Count;
            Quaternion rot = node.rotation;
            int boneIdx = (overrideBoneIndex != -1) ? overrideBoneIndex : node.boneIndex;

            for(int s=0; s<=radialSegments; s++) { 
                 float a = (float)s / radialSegments * Mathf.PI * 2f; 
                 verts.Add(node.position + rot * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * node.radius); 
                 uvs.Add(new Vector2((float)s/radialSegments, node.vCoord));
                 
                 // Assign Weight
                 AddBoneWeight(boneIdx);
            }
        }
        
        private void AddBoneWeight(int index)
        {
            BoneWeight bw = new BoneWeight();
            bw.boneIndex0 = index;
            bw.weight0 = 1.0f; 
            boneWeights.Add(bw);
        }

        private void CollectAndMergeLeaves(BioNode node, int seed, int currentMaxGen)
        {
            if (node == null || leafPrefab == null) return;
            MeshFilter mfStruct = leafPrefab.GetComponent<MeshFilter>();
            if (mfStruct == null) return;
            
            // Cache leaf mesh data once
            leafVerts.Clear(); leafTris.Clear(); leafUVs.Clear();
            mfStruct.sharedMesh.GetVertices(leafVerts);
            mfStruct.sharedMesh.GetTriangles(leafTris, 0);
            mfStruct.sharedMesh.GetUVs(0, leafUVs);

            System.Random rng = new System.Random(seed);
            
            BioNode w = node;
            while(w.mainChild != null)
            {
                foreach(var c in w.sideChildren) CollectAndMergeLeaves(c, rng.Next(), currentMaxGen);
                
                int leafLayers = Mathf.Clamp(maxRecursion - 1, 1, 3);
                int startThreshold = maxRecursion - leafLayers;
                
                bool isThin = w.radius < (maxTrunkThickness * 0.05f);
                bool isCanopy = (w.generation >= startThreshold);
                
                if (isThin || isCanopy) MergeLeafInstances(w, w.mainChild, rng);
                w = w.mainChild;
            }
            MergeLeafInstances(w, null, rng);
        }

        private void MergeLeafInstances(BioNode startNode, BioNode endNode, System.Random rng)
        {
             if(leavesPerBranch <= 0) return;
             if(endNode == null) endNode = startNode; 

             // FORCE FULL GROWTH for bake
             float growthFactor = 1.0f; 
             // float growthFactor = Mathf.Clamp01((growthCycle - 0.05f) / 0.95f);
             
             if(growthFactor <= 0.001f) return;

             float currentScale = Mathf.Lerp(0.1f, 1.0f, growthFactor) * leafScale; 

             for(int l=0; l<leavesPerBranch; l++)
             {
                 float t = (float)rng.NextDouble();
                 Vector3 lPos = Vector3.Lerp(startNode.position, endNode.position, t);
                 Vector3 surfNorm = (Quaternion.AngleAxis((float)rng.NextDouble()*360f, startNode.direction) * Vector3.up).normalized;
                 
                 // Create Transform Matrix for the leaf
                 Matrix4x4 m = Matrix4x4.TRS(lPos + surfNorm * startNode.radius, 
                     Quaternion.LookRotation(surfNorm, startNode.direction) * Quaternion.Euler((float)rng.NextDouble()*30f-15f, (float)rng.NextDouble()*30f-15f, 0), 
                     Vector3.one * currentScale);
                 
                 // Decide which bone this leaf belongs to.
                 // It's strictly attached to 'startNode' segment.
                 int bindBoneIdx = startNode.boneIndex;

                 // Merge vertices
                 int baseV = verts.Count;
                 for(int v=0; v<leafVerts.Count; v++)
                 {
                     Vector3 transformedPt = m.MultiplyPoint3x4(leafVerts[v]);
                     verts.Add(transformedPt);
                     uvs.Add(leafUVs[v]);
                     AddBoneWeight(bindBoneIdx);
                 }
                 
                 // Merge triangles
                 for(int tri=0; tri<leafTris.Count; tri++)
                 {
                     foliageTris.Add(baseV + leafTris[tri]);
                 }
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
                 trunkTris.Add(a1); trunkTris.Add(c2); trunkTris.Add(c1);
             }
        }
        private void AddQuad(int a, int b, int c, int d) { trunkTris.Add(a); trunkTris.Add(b); trunkTris.Add(c); trunkTris.Add(c); trunkTris.Add(d); trunkTris.Add(a); }
        private int ResolveMasterSeed()
        {
            if (randomizeSeedOnGenerate)
            {
                randomSeed = System.Guid.NewGuid().GetHashCode();
            }

            if (randomSeed == 0)
            {
                randomSeed = GetHashCode();
                if (randomSeed == 0)
                {
                    randomSeed = 1;
                }
            }

            return randomSeed;
        }

        private Vector3 RandomVector(System.Random r) { return new Vector3((float)r.NextDouble()-0.5f, (float)r.NextDouble()-0.5f, (float)r.NextDouble()-0.5f); }
    }
}
