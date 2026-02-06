using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [AddComponentMenu("HW/Tree Runtime (Seedling + SC)")]
    public class HW_TreeRuntime : MonoBehaviour
    {
        [Header("Runtime")]
        public bool generateOnStart = true;
        public bool regenerateEveryFrame = false;
        public bool regenerateOnValidate = false;
        public bool useRandomSeed = false;
        public int randomSeed = 0;
        public Vector3 spawnPosition = Vector3.zero;
        [Range(0f, 1f)]
        public float growth01 = 1f;
        public bool useGrowthForDimensions = true;
        [Range(0f, 1f)]
        public float minGrowth01ForBranches = 0.45f;
        [Range(0f, 1f)]
        public float minGrowth01ForBranchlets = 0.6f;
        [Range(0f, 1f)]
        public float minGrowth01ForLeaves = 0.55f;
        [Range(0f, 1f)]
        public float minGrowth01ForTrueLeaves = 0.3f;
        public bool usePersistentGraph = true;
        public bool lockGraphOnceBuilt = true;
        public int persistentGraphSeed = 0;
        public bool enableBranchletChildren = true;
        public int branchletChildrenPerBranchlet = 2;
        public float branchletPromoteThreshold = 0.6f;
        public float yDominantBias = 3f;
        public float maxAsymmetry = 0.6f;
        public bool branchesAtTipOnly = true;
        public int branchletsPerBranch = 2;
        [Range(0f, 1f)]
        public float trunkPostBranchGrowthScale = 0.2f;
        public float tipSplitYaw = 30f;
        [Header("Bio-Mimetic Growth")]
        [Tooltip("Maximum height for the tree at full maturity.")]
        public float maxTreeHeight = 8.0f;
        [Tooltip("Steepness of the logistic growth curve.")]
        public float growthRateK = 8.0f; 
        [Tooltip("Midpoint of the growth phase (0.0 to 1.0).")]
        public float growthMidpointT0 = 0.4f;

        [Header("Stage 1: Seedling")]
        public float seedlingMaxHeight = 0.8f; /* Height at end of seedling stage */
        public float cotyledonStartSize = 1.0f;
        public float cotyledonWitherStart = 0.3f; /* Growth01 value */

        [Header("Stage 2: Hybrid L-System")]
        public int lSystemIterations = 4;
        public float lSystemStepLength = 0.45f; /* Longer segments for smoothness */
        public float lSystemAngle = 18f; /* Narrower angle for upward flow */
        public float lSystemAngleRandomness = 5f; /* Reduced chaos */
        [Tooltip("Dominance of Space Colonization direction (0=Pure L-System, 1=Pure SC)")]
        [Range(0f, 1f)]
        public float scDirectionBias = 0.7f; /* Stronger SC bias for tropism */
        public string lsystemAxiom = "FFF[+X][-X]F"; /* Start with longer trunk */
        public string lsystemRuleX = "[+F][&F]/F"; /* simpler branching, less fractal noise */
        public string lsystemRuleF = "F"; /* Linear growth */
        
        [Header("Space Colonization")]
        [Tooltip("Enable Space Colonization growth after seedling stage.")]
        public bool useSpaceColonization = false;
        [Range(0f, 1f)]
        [Tooltip("Growth01 threshold to start Space Colonization.")]
        public float spaceColonizationStartGrowth01 = 0.6f;
        public int scAttractorCount = 300;
        public Vector3 scCrownSize = new Vector3(2f, 3f, 2f);
        public Vector3 scCrownCenterOffset = new Vector3(0f, 0.5f, 0f);
        public float scInfluenceRadius = 1.6f; /* Broader search for smoother paths */
        public float scKillRadius = 1.0f; /* Aggressive pruning to prevent overcrowding */
        public float scSegmentLength = 0.3f;
        public float scTipRadius = 0.02f;
        public int scIterationsPerFullGrowth = 140;
        public float scThicknessUpdateInterval = 0.5f;
        [Range(0f, 1f)]
        public float scUpBias = 0.6f; /* Strong Upward Tendency */
        [Range(0f, 1f)]
        public float scAttractorUpBias = 0.7f; /* Attractors favor top */
        public bool scUpperHemisphereOnly = true;
        [Range(0f, 1f)]
        [Tooltip("Growth01 threshold to start withering/dropping seedling leaves.")]
        public float leafWitherStartGrowth01 = 0.6f;

        [Header("Persistent Targets")]
        public float targetTrunkHeight = 0f;
        public float targetTrunkRadius = 0f;
        public float targetBranchLength = 0f;
        public float targetBranchRadius = 0f;
        public float targetBranchletLength = 0f;
        public float targetBranchletRadius = 0f;
        public int targetNumberOfBranches = 0;
        public int targetNumberOfBranchlets = 0;

        [Header("Trunk")]
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

        [Header("Branches")]
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

        [Header("Branchlets")]
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

        [Header("Leaves - Branches")]
        public bool generateBranchLeaves = true;
        public int numberOfLeaves = 21;
        public float leafSize = 1.37f;
        public float leafPositionMin = 0.83f;
        public float leafPositionMax = 1f;
        public bool useLeafEndDistance = true;
        public float leafEndDistanceMeters = 0.8f;
        public float leafForwardRotation = 0f;
        public float leafRotation = 0f;
        public float leafRandomizeRotation = 0.47f;
        public float leafBranchRandomPositioning = 0f;
        public Vector3 leafBranchPositioning = Vector3.zero;
        public Vector3 leafBranchSizeV3 = new Vector3(1f, 1f, 1f);
        public float leafSizeBranchRandom = 0.32f;

        [Header("Leaves - Branchlets")]
        public bool generateBranchletLeaves = true;
        public int numberOfLeavesBranchlet = 15;
        public float leafBranchletSize = 1.5f;
        public float leafBranchletPositionMin = 0.27f;
        public float leafBranchletPositionMax = 1f;
        public bool useBranchletLeafEndDistance = true;
        public float branchletLeafEndDistanceMeters = 0.8f;
        public float leafBranchletForwardRotation = 0f;
        public float leafBranchletRotation = 14.8f;
        public float leafBranchletRandomizeRotation = 0.2f;
        public Vector3 leafBranchletPositioning = Vector3.zero;
        public float leafBranchletRandomPositioning = 0f;
        public Vector3 leafBranchletSizeV3 = new Vector3(1f, 1f, 1f);
        public float leafSizeBranchletRandom = 0.59f;

        [Header("Leaves - Trunk (Cotyledon)")]
        public bool generateTrunkLeaves = true;
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

        [Header("Leaves - True Leaves (Bon-Ip)")]
        public bool generateTrueLeaves = false;
        public int trueLeavesPairs = 0;
        public float trueLeavesStartHeight = 0.2f;
        public float trueLeavesInterval = 0.1f;
        public float trueLeavesSize = 1.0f;
        public float trueLeavesAngleOffset = 90f;
        public float trueLeavesForwardRotation = 0f;
        public float trueLeavesRotation = 0f;
        public float trueLeavesRotationRandom = 0f;
        public Vector3 trueLeavesSizeV3 = new Vector3(1f, 1f, 1f);
        public GameObject trueLeafPrefabOverride;

        [Header("Leaf Assets")]
        public GameObject leafPrefab;
        public GameObject trunkLeafPrefabOverride;
        public Material leafMaterial;
        public Material trunkLeafMaterialOverride;

        private int vertexCount = 0;
        private int triangleCount = 0;
        private int edgeCount = 0;

        private int vertexBranchCount = 0;
        private int triangleBranchCount = 0;
        private int edgeBranchCount = 0;

        private int vertexBranchletCount = 0;
        private int triangleBranchletCount = 0;
        private int edgeBranchletCount = 0;

        private int vertexBranchLeavesCount = 0;
        private int triangleBranchLeavesCount = 0;
        private int edgeBranchLeavesCount = 0;

        private int vertexBranchletLeavesCount = 0;
        private int triangleBranchletLeavesCount = 0;
        private int edgeBranchletLeavesCount = 0;

        private int vertexTrunkLeavesCount = 0;
        private int triangleTrunkLeavesCount = 0;
        private int edgeTrunkLeavesCount = 0;

        private int vertexTrueLeavesCount = 0;
        private int triangleTrueLeavesCount = 0;
        private int edgeTrueLeavesCount = 0;

        private GameObject trunkObject;
        private GameObject branchesParent;
        private GameObject branchletsParent;
        private GameObject leavesTrunk;
        private GameObject leavesBranchlet;
        private GameObject leavesBranch;

        private Trunk trunk;
        private List<Branch> branches = new List<Branch>();
        public List<BranchletsX> branchlets = new List<BranchletsX>();
        private GameObject spaceColonizationParent;
        private float lastGrowth01 = 0f;
        private bool suppressSeedlingLeaves = false;
        private bool requestWitherLeaves = false;
        private float scThicknessTimer = 0f;
        private float scRootRadiusHint = 0f;
        private bool scInitialized = false;
        private int scIterationCount = 0;
        private System.Random scRng;
        private readonly List<SCAttractor> scAttractors = new List<SCAttractor>();
        private readonly List<SCNode> scNodes = new List<SCNode>();
        private readonly List<int> scRootIndices = new List<int>();
        private readonly List<float> scRootCaps = new List<float>();
        private Vector3 scOrigin = Vector3.zero;
        private bool generatedOnce = false;

        private class SCAttractor
        {
            public Vector3 position;
            public SCAttractor(Vector3 position)
            {
                this.position = position;
            }
        }

        private class SCNode
        {
            public Vector3 position;
            public Vector3 direction;
            public int parent;
            public List<int> children = new List<int>();
            public float radius;
            public SCNode(Vector3 position, Vector3 direction, int parent)
            {
                this.position = position;
                this.direction = direction;
                this.parent = parent;
            }
        }

        [System.Serializable]
        private class PersistentBranchlet
        {
            public float alongFrac;
            public float sideFactor;
            public float baseLength;
            public float baseRadius;
            public float lengthScale;
            public float angleScale;
            public int childSeed;
        }

        [System.Serializable]
        private class PersistentBranch
        {
            public float heightFrac;
            public float rotationY;
            public float angle;
            public float baseLength;
            public float baseRadius;
            public List<PersistentBranchlet> branchlets = new List<PersistentBranchlet>();
            public float sideFactor;
            public float lengthScale = 1f;
            public float angleScale = 1f;
        }

        [System.Serializable]
        private class PersistentGraphData
        {
            public int seed;
            public List<PersistentBranch> branches = new List<PersistentBranch>();
        }

        private PersistentGraphData persistentGraph;

        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        private void Update()
        {
            scThicknessTimer += Time.deltaTime;
            if (!generatedOnce && generateOnStart && Application.isPlaying)
            {
                Generate();
            }
            if (regenerateEveryFrame)
            {
                Generate();
            }
        }

        private void OnEnable()
        {
            if (generateOnStart && Application.isPlaying && !generatedOnce)
            {
                Generate();
            }
        }

        private void OnValidate()
        {
            if (!regenerateOnValidate) return;
            if (Application.isPlaying) return;
            Generate();
        }

        public void Generate()
        {
            generatedOnce = true;
            Random.State prevState = Random.state;
            if (useRandomSeed)
            {
                Random.InitState(randomSeed);
            }

            if (!useSpaceColonization)
            {
                suppressSeedlingLeaves = false;
            }
            float growthT = Mathf.Clamp01(growth01);
            
            // [BIO-MIMETIC] Logistic Growth for Main Trunk Height
            // We use the new parameters if they are set sane, otherwise fallback
            float currentHeight = CalculateLogisticGrowth(growthT, maxTreeHeight, growthRateK, growthMidpointT0);
            float currentRadius = trunkRadius * (currentHeight / maxTreeHeight); // Simple allometry for now

            float growthFactor = useGrowthForDimensions ? growthT : 1f;

            // Shedding Logic
            bool shedCotyledons = growthT > cotyledonWitherStart;
            float cotyledonScale = 1.0f;
            if (shedCotyledons)
            {
                // Rapidly scale down to 0
                float witherProgress = (growthT - cotyledonWitherStart) * 5.0f; // Fast wither
                cotyledonScale = Mathf.Clamp01(1.0f - witherProgress);
            }

            if (useSpaceColonization && growthT >= leafWitherStartGrowth01 && lastGrowth01 < leafWitherStartGrowth01)
            {
                requestWitherLeaves = true;
                suppressSeedlingLeaves = true;
            }
            if (requestWitherLeaves)
            {
                DetachLeavesForWither();
                requestWitherLeaves = false;
            }

            EnsurePersistentGraph();
            ClearGenerated();

            // Use calculated bio-height instead of linear scaling
            float trunkHeightScaled = currentHeight;
            
            // Adjust Trunk Radius for seedling look
            float trunkRadiusScaled = currentRadius;
            if (trunkRadiusScaled < 0.005f) trunkRadiusScaled = 0.005f; // Minimum thickness

            if (scRootRadiusHint > 0f)
            {
                trunkRadiusScaled = Mathf.Max(trunkRadiusScaled, scRootRadiusHint);
            }
            int trunkSegmentsScaled = Mathf.Max(2, Mathf.RoundToInt(trunkSegments * Mathf.Max(0.05f, growthT)));

            GenerateTreeTrunk(ref vertexCount, ref triangleCount, ref edgeCount, trunkSubdivision, trunkSegmentsScaled, trunkBending, trunkHeightScaled, trunkRadiusCurvature, trunkRadiusScaled, treeStumpStartPoint, treeStumpWidth, includeStump, spawnPosition);

            // Branches (Sapling Stage +)
            // Only generate branches if we are past Seedling stage (defined by height or growthT)
            // Let's say Seedling ends roughly when growthT > 0.2
            bool isSeedling = growthT < 0.2f;

            if (!isSeedling)
            {
                // [BIO-MIMETIC] Stage 2: Hybrid L-System
                GenerateHybridBranches(growthFactor, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);
                
                // [BIO-MIMETIC] Stage 3: Space Colonization (Mature Crown)
                // Takes over from L-System tips if enabled
                if (useSpaceColonization && growthT >= spaceColonizationStartGrowth01)
                {
                    GenerateSpaceColonization(growthT);
                }
            }


            if (leafPrefab != null)
            {
                // Normal Leaves
                if (!isSeedling)
                {
                    if (generateBranchLeaves && numberOfLeaves > 0 && growthT >= minGrowth01ForLeaves)
                    {
                        GenerateLeafPlanes(leafBranchPositioning, leafBranchSizeV3, leafMaterial, numberOfLeaves, ref vertexBranchLeavesCount, ref triangleBranchLeavesCount, ref edgeBranchLeavesCount, leafPrefab, leafSizeBranchRandom, leafSize, leafPositionMin, leafPositionMax, leafForwardRotation, leafRotation, leafRandomizeRotation, leafBranchRandomPositioning);
                    }
                    if (generateBranchletLeaves && numberOfLeavesBranchlet > 0 && growthT >= minGrowth01ForLeaves)
                    {
                        GenerateLeafBranchletPlanes(leafBranchletPositioning, leafBranchletSizeV3, leafMaterial, numberOfLeavesBranchlet, ref vertexBranchletLeavesCount, ref triangleBranchletLeavesCount, ref edgeBranchletLeavesCount, leafPrefab, leafSizeBranchletRandom, leafBranchletSize, leafBranchletPositionMin, leafBranchletPositionMax, leafBranchletForwardRotation, leafBranchletRotation, leafBranchletRandomizeRotation, leafBranchletRandomPositioning);
                    }
                }
            }

            // Cotyledons (Stage 1)
            // Replaces "Trunk Leaves" for seedling phase
            if (cotyledonScale > 0.01f && generateTrunkLeaves)
            {
                GameObject trunkLeafPrefab = trunkLeafPrefabOverride != null ? trunkLeafPrefabOverride : leafPrefab;
                Material trunkLeafMaterial = trunkLeafMaterialOverride != null ? trunkLeafMaterialOverride : leafMaterial;
                if (trunkLeafPrefab != null)
                {
                    GenerateCotyledons(trunkLeafPrefab, trunkLeafMaterial, cotyledonScale, ref vertexTrunkLeavesCount, ref triangleTrunkLeavesCount, ref edgeTrunkLeavesCount);
                }
            }

            // True Leaves (Stage 1 & 2)
            // Replaces "True Leaves" procedural generation
            if (!suppressSeedlingLeaves && generateTrueLeaves && trueLeavesPairs > 0 && growthT >= minGrowth01ForTrueLeaves)
            {
                GameObject trueLeafPrefab = trueLeafPrefabOverride != null ? trueLeafPrefabOverride : (trunkLeafPrefabOverride != null ? trunkLeafPrefabOverride : leafPrefab);
                Material trueLeafMaterial = trunkLeafMaterialOverride != null ? trunkLeafMaterialOverride : leafMaterial;

                if (trueLeafPrefab != null)
                {
                    GenerateDecussateLeaves(trueLeafPrefab, trueLeafMaterial, trueLeavesStartHeight, trueLeavesInterval, ref vertexTrueLeavesCount, ref triangleTrueLeavesCount, ref edgeTrueLeavesCount);
                }
            }

            Random.state = prevState;
            lastGrowth01 = growthT;
        }

        [ContextMenu("Generate Now")]
        private void GenerateNow()
        {
            Generate();
        }

        [ContextMenu("Clear Generated")]
        private void ClearGeneratedContext()
        {
            ClearGenerated();
            generatedOnce = false;
        }

        public void ClearGenerated()
        {
            if (trunkObject != null)
            {
                DestroyObject(trunkObject);
            }

            trunkObject = null;
            if (spaceColonizationParent != null)
            {
                DestroyObject(spaceColonizationParent);
            }
            spaceColonizationParent = null;
            branchesParent = null;
            branchletsParent = null;
            leavesTrunk = null;
            leavesBranchlet = null;
            leavesBranch = null;
            trunk = null;
            branches.Clear();
            branchlets.Clear();
        }

        public string GetPersistentGraphJson()
        {
            if (persistentGraph == null) return string.Empty;
            return JsonUtility.ToJson(persistentGraph);
        }

        public void SetPersistentGraphJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            persistentGraph = JsonUtility.FromJson<PersistentGraphData>(json);
        }

        private float CalculateLogisticGrowth(float t, float maxVal, float k, float t0)
        {
            // Logistic Function: f(x) = L / (1 + e^(-k(x-x0)))
            // We shift x so that at t=0, f(x) is near 0? 
            // Actually, we want t=0 to 1.
            // Let's assume t is the 'time' input.
            
            float val = maxVal / (1.0f + Mathf.Exp(-k * (t - t0)));
            
            // Normalize to start at 0 if needed, but for height it's fine.
            // Let's ensure at t=0 it is small but not negative.
            // With k=10, t0=0.5, at t=0: exp(5) is big, so roughly 0.
            return val;
        }

        private float GetTargetValue(float target, float fallback)
        {
            return target > 0f ? target : fallback;
        }

        private int GetTargetValue(int target, int fallback)
        {
            return target > 0 ? target : fallback;
        }

        private void EnsurePersistentGraph()
        {
            if (!usePersistentGraph) return;
            if (persistentGraph != null && lockGraphOnceBuilt)
            {
                if (!branchesAtTipOnly)
                {
                    return;
                }
                if (persistentGraph.branches.Count == 2)
                {
                    bool tipLike = true;
                    float tipFrac = Mathf.Clamp01(branchHeightMax > 0f ? branchHeightMax : 0.95f);
                    for (int i = 0; i < persistentGraph.branches.Count; i++)
                    {
                        if (persistentGraph.branches[i].heightFrac < tipFrac - 0.01f)
                        {
                            tipLike = false;
                            break;
                        }
                    }
                    if (tipLike)
                    {
                        return;
                    }
                }
            }

            int seed = persistentGraphSeed != 0 ? persistentGraphSeed : randomSeed;
            Random.State prev = Random.state;
            Random.InitState(seed);

            float trunkHeightTarget = GetTargetValue(targetTrunkHeight, trunkHeight);
            float trunkRadiusTarget = GetTargetValue(targetTrunkRadius, trunkRadius);
            int branchCountTarget = GetTargetValue(targetNumberOfBranches, numberOfBranches);
            if (branchesAtTipOnly)
            {
                branchCountTarget = 2;
            }
            int branchletCountTarget = GetTargetValue(targetNumberOfBranchlets, numberOfBranchlets);
            float branchLengthTarget = GetTargetValue(targetBranchLength, branchLength);
            float branchRadiusTarget = GetTargetValue(targetBranchRadius, branchRadius);
            float branchletLengthTarget = GetTargetValue(targetBranchletLength, branchletLength);
            float branchletRadiusTarget = GetTargetValue(targetBranchletRadius, branchletRadius);

            persistentGraph = new PersistentGraphData
            {
                seed = seed,
                branches = new List<PersistentBranch>(branchCountTarget)
            };

            float segmentAngle = branchCountTarget > 0 ? 360f / branchCountTarget : 360f;
            for (int i = 0; i < branchCountTarget; i++)
            {
                float tipFrac = Mathf.Clamp01(branchHeightMax > 0f ? branchHeightMax : 0.95f);
                float heightFrac = branchesAtTipOnly ? tipFrac : Random.Range(branchHeightMin, branchHeightMax);
                float sideFactor = (i % 2 == 0) ? 1f : -1f;
                float asymmetry = Mathf.Pow(Random.value, Mathf.Max(0.1f, yDominantBias)) * Mathf.Clamp01(maxAsymmetry);
                float lengthScale = sideFactor > 0f ? (1f + asymmetry) : (1f - asymmetry);
                float angleScale = sideFactor > 0f ? (1f + asymmetry) : (1f - asymmetry);
                float randomVariation = branchesAtTipOnly ? 0f : Random.Range(-segmentAngle / 4f, segmentAngle / 4f);
                float rotationY = branchesAtTipOnly ? (sideFactor * tipSplitYaw * angleScale) : i * segmentAngle + randomVariation;

                float adjustedAngle = branchAngle * angleScale;
                if (angleAdjustmentByHeight)
                {
                    float heightBasedAdjustment = heightFrac * 30f;
                    adjustedAngle = Mathf.Min(branchAngle + heightBasedAdjustment, 160f);
                }

                float adjustedBranchLength = branchLengthTarget;
                if (adjustBranchLengthByHeight && !branchesAtTipOnly)
                {
                    adjustedBranchLength = Mathf.Lerp(branchLengthTarget, branchLengthTarget / 5f, heightFrac);
                }

                float trunkRadiusAtHeight = Mathf.Lerp(
                    trunkRadiusTarget * Mathf.Clamp01(1 + trunkRadiusCurvature),
                    trunkRadiusTarget * Mathf.Clamp01(1 - trunkRadiusCurvature),
                    heightFrac
                );

                float adjustedBranchRadius = Mathf.Min(branchRadiusTarget, trunkRadiusAtHeight);

                PersistentBranch branchData = new PersistentBranch
                {
                    heightFrac = heightFrac,
                    rotationY = rotationY,
                    angle = adjustedAngle,
                    baseLength = adjustedBranchLength,
                    baseRadius = adjustedBranchRadius,
                    sideFactor = sideFactor,
                    lengthScale = lengthScale,
                    angleScale = angleScale
                };

                persistentGraph.branches.Add(branchData);
            }

            if (branchCountTarget > 0 && branchletCountTarget > 0)
            {
                int branchletPerBranch = branchesAtTipOnly ? Mathf.Max(0, branchletsPerBranch) : Mathf.FloorToInt((float)branchletCountTarget / branchCountTarget);
                int remainder = branchesAtTipOnly ? 0 : branchletCountTarget % branchCountTarget;

                for (int i = 0; i < persistentGraph.branches.Count; i++)
                {
                    int branchletCount = branchletPerBranch + (i < remainder ? 1 : 0);
                    PersistentBranch branchData = persistentGraph.branches[i];

                    for (int j = 0; j < branchletCount; j++)
                    {
                        float alongFrac = branchesAtTipOnly ? 1f : Mathf.Clamp01(Random.Range(branchletHeightMin, branchletHeightMax));
                        float sideFactor = (j % 2 == 0) ? 1f : -1f;

                        float asymmetry = Mathf.Pow(Random.value, Mathf.Max(0.1f, yDominantBias)) * Mathf.Clamp01(maxAsymmetry);
                        float lengthScale = sideFactor > 0f ? (1f + asymmetry) : (1f - asymmetry);
                        float angleScale = sideFactor > 0f ? (1f + asymmetry) : (1f - asymmetry);

                        float adjustedBranchletLength = branchletLengthTarget;
                        if (adjustBranchletLengthByHeight && !branchesAtTipOnly)
                        {
                            adjustedBranchletLength = Mathf.Lerp(branchletLengthTarget, branchletLengthTarget / 3f, branchData.heightFrac);
                        }

                        float topRadius = branchData.baseRadius * Mathf.Clamp01(1 - branchRadiusCurvature);
                        float bottomRadius = branchData.baseRadius * Mathf.Clamp01(1 + branchRadiusCurvature);
                        float baseRadius = Mathf.Lerp(bottomRadius, topRadius, alongFrac);

                        PersistentBranchlet branchletData = new PersistentBranchlet
                        {
                            alongFrac = alongFrac,
                            sideFactor = sideFactor,
                            baseLength = adjustedBranchletLength,
                            baseRadius = Mathf.Min(branchletRadiusTarget, baseRadius),
                            lengthScale = lengthScale,
                            angleScale = angleScale,
                            childSeed = Random.Range(int.MinValue, int.MaxValue)
                        };

                        branchData.branchlets.Add(branchletData);
                    }
                }
            }

            Random.state = prev;
        }

        private new void DestroyObject(Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(obj);
                return;
            }
#endif
            Destroy(obj);
        }

        private void DetachLeavesForWither()
        {
            if (trunkObject == null) return;

            string[] leafGroups = { "Leaves Trunk", "True Leaves", "Leaves Branch", "Leaves Branchlet" };
            Transform parentTransform = trunkObject.transform.parent;
            GameObject witherParent = new GameObject("WitherLeaves");
            witherParent.transform.SetParent(parentTransform);
            witherParent.transform.position = trunkObject.transform.position;

            for (int i = 0; i < leafGroups.Length; i++)
            {
                Transform group = trunkObject.transform.Find(leafGroups[i]);
                if (group == null) continue;
                List<Transform> children = new List<Transform>();
                for (int c = 0; c < group.childCount; c++)
                {
                    children.Add(group.GetChild(c));
                }

                for (int c = 0; c < children.Count; c++)
                {
                    Transform leaf = children[c];
                    leaf.SetParent(witherParent.transform, true);
                    if (leaf.GetComponent<HW_TreeLeafWither>() == null)
                    {
                        leaf.gameObject.AddComponent<HW_TreeLeafWither>();
                    }
                }

                DestroyObject(group.gameObject);
            }
        }

        private void GenerateTreeTrunk(ref int vertexCount, ref int triangleCount, ref int edgeCount, int trunkSubdivision, int trunkSegments, float trunkBending, float trunkHeight, float trunkRadiusCurvature, float trunkRadius, float treeStumpStartPoint, float treeStumpWidth, bool includeStump, Vector3 spawnPosition)
        {
            if (trunkObject != null)
            {
                DestroyObject(trunkObject);
            }

            trunkObject = new GameObject("Generated Tree (unsaved)");
            trunkObject.transform.position = spawnPosition;

            MeshFilter meshFilter = trunkObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = trunkObject.AddComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = GetTrunkMaterial();
            List<Vector3> trunkBendPositions = new List<Vector3>();

            meshFilter.mesh = CreateTrunkMesh(ref vertexCount, ref triangleCount, ref edgeCount, trunkSubdivision, trunkSegments, trunkBending, trunkHeight, trunkRadiusCurvature, trunkRadius, trunkBendPositions, includeStump);

            trunk = new Trunk(trunkBendPositions);
        }

        private Material GetTrunkMaterial()
        {
            if (trunkMaterial != null)
            {
                return trunkMaterial;
            }

            Shader fallback = Shader.Find("Custom/Watercolour");
            if (fallback == null)
            {
                fallback = Shader.Find("Standard");
            }
            Material mat = new Material(fallback);
            mat.color = new Color(0.55f, 0.27f, 0.07f);
            return mat;
        }

        private Mesh CreateTrunkMesh(ref int vertexCount, ref int triangleCount, ref int edgeCount, int trunkSubdivision, int trunkSegments, float trunkBending, float trunkHeight, float trunkRadiusCurvature, float trunkRadius, List<Vector3> trunkBendPositions, bool includeStump)
        {
            Mesh mesh = new Mesh();
            mesh.name = "HW_TrunkMesh";

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

            Vector3 previousPosition = Vector3.zero;

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
            Vector3 pointedTopPosition = topVertexPosition + new Vector3(0, 0f, 0);

            vertices[pointedTopIndex] = pointedTopPosition;
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
            mesh.RecalculateBounds();

            vertexCount = mesh.vertexCount;
            triangleCount = mesh.triangles.Length / 3;
            edgeCount = CalculateEdges(mesh);

            return mesh;
        }

        private int CalculateEdges(Mesh mesh)
        {
            HashSet<(int, int)> edges = new HashSet<(int, int)>();
            int[] triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];

                edges.Add((Mathf.Min(a, b), Mathf.Max(a, b)));
                edges.Add((Mathf.Min(b, c), Mathf.Max(b, c)));
                edges.Add((Mathf.Min(c, a), Mathf.Max(c, a)));
            }

            return edges.Count;
        }

        private void GenerateTreeBranches(float trunkHeight, float branchLength, float branchAngle, float branchBending, float trunkBending, float trunkRadius, float trunkRadiusCurvature, float gravity, bool angleAdjustmentByHeight, bool adjustBranchLengthByHeight, int branchSegments, int branchSubdivision, ref int vertexBranchCount, ref int triangleBranchCount, ref int edgeBranchCount)
        {
            vertexBranchCount = 0;
            triangleBranchCount = 0;
            edgeBranchCount = 0;
            if (branchesParent != null)
            {
                DestroyObject(branchesParent);
            }

            branchesParent = new GameObject("TreeBranches");
            branchesParent.transform.SetParent(trunkObject.transform);

            branches.Clear();

            float growthFactor = useGrowthForDimensions ? Mathf.Clamp01(growth01) : 1f;
            float maxBranchHeight = trunkHeight;
            Vector3 trunkTipDirection = Vector3.up;
            float trunkRadiusAtTip = trunkRadius * Mathf.Clamp01(1 - trunkRadiusCurvature);
            if (branchesAtTipOnly && trunk != null && trunk.trunkBendPositions != null && trunk.trunkBendPositions.Count >= 2)
            {
                Vector3 trunkTip = trunk.trunkBendPositions[trunk.trunkBendPositions.Count - 1];
                Vector3 trunkPrev = trunk.trunkBendPositions[trunk.trunkBendPositions.Count - 2];
                Vector3 dir = trunkTip - trunkPrev;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    trunkTipDirection = dir.normalized;
                }
            }

            if (usePersistentGraph && persistentGraph != null && persistentGraph.branches.Count > 0)
            {
                float normalizedGrowth = Mathf.Clamp01((growthFactor - minGrowth01ForBranches) / Mathf.Max(0.0001f, 1f - minGrowth01ForBranches));
                int allowedBranches = branchesAtTipOnly
                    ? persistentGraph.branches.Count
                    : Mathf.Clamp(Mathf.RoundToInt(persistentGraph.branches.Count * normalizedGrowth), 0, persistentGraph.branches.Count);

                for (int i = 0; i < persistentGraph.branches.Count; i++)
                {
                    if (i >= allowedBranches)
                    {
                        break;
                    }
                    PersistentBranch branchData = persistentGraph.branches[i];
                    if (!branchesAtTipOnly && branchData.heightFrac > growthFactor)
                    {
                        continue;
                    }

                    float height = branchesAtTipOnly ? trunkHeight : branchData.heightFrac * trunkHeight;
                    if (height > maxBranchHeight)
                    {
                        continue;
                    }

                    float branchGrowth = branchesAtTipOnly
                        ? Mathf.Clamp01((growthFactor - minGrowth01ForBranches) / Mathf.Max(0.0001f, 1f - minGrowth01ForBranches))
                        : Mathf.Clamp01(Mathf.InverseLerp(branchData.heightFrac, Mathf.Max(branchData.heightFrac + 0.0001f, 1f), growthFactor));
                    float adjustedBranchLength = branchData.baseLength * branchData.lengthScale * branchGrowth;
                    float adjustedBranchRadius = branchData.baseRadius * Mathf.Max(0.05f, branchGrowth);
                    if (branchesAtTipOnly)
                    {
                        adjustedBranchRadius = Mathf.Min(adjustedBranchRadius, trunkRadiusAtTip * Mathf.Max(0.05f, branchGrowth));
                    }

                    Vector3 branchPosition = branchesAtTipOnly
                        ? (trunk.trunkBendPositions[trunk.trunkBendPositions.Count - 1] + trunkObject.transform.position)
                        : Vector3.zero;
                    if (!branchesAtTipOnly)
                    {
                        float normalizedHeightAlongTrunk = height / trunkHeight;
                        int index1 = Mathf.FloorToInt(normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1));
                        int index2 = Mathf.Min(index1 + 1, trunk.trunkBendPositions.Count - 1);

                        Vector3 trunkBendPosition1 = trunk.trunkBendPositions[index1];
                        Vector3 trunkBendPosition2 = trunk.trunkBendPositions[index2];
                        float t = normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1) - index1;

                        branchPosition = Vector3.Lerp(trunkBendPosition1, trunkBendPosition2, t) + trunkObject.transform.position;
                        branchPosition.y = height + trunkObject.transform.position.y;
                    }

                    Quaternion randomRotation = Quaternion.Euler(branchData.angle, branchData.rotationY, 0f);
                    Vector3 direction = Vector3.forward;
                    direction = Quaternion.Euler(-90f, 0f, 0f) * direction;
                    direction = randomRotation * direction;
                    if (branchesAtTipOnly)
                    {
                        Quaternion toTrunk = Quaternion.FromToRotation(Vector3.up, trunkTipDirection);
                        Quaternion yawRot = Quaternion.AngleAxis(branchData.rotationY, trunkTipDirection);
                        Vector3 pitchAxis = yawRot * (toTrunk * Vector3.right);
                        float baseAngle = Mathf.Abs(branchData.angle);
                        float minAngle = Mathf.Max(5f, Mathf.Abs(branchAngle) * 0.4f);
                        float maxAngle = Mathf.Max(minAngle + 1f, Mathf.Abs(branchAngle) * 1.6f);
                        float splitAngle = Mathf.Clamp(baseAngle, minAngle, maxAngle);
                        Quaternion pitchRot = Quaternion.AngleAxis(splitAngle, pitchAxis);
                        randomRotation = pitchRot * yawRot * toTrunk;
                        direction = randomRotation * Vector3.up;
                    }

                    List<Vector3> bendPositions = new List<Vector3>();

                    GameObject branch = new GameObject("Branch" + i);
                    branch.transform.SetParent(branchesParent.transform);
                    branch.transform.position = branchPosition;
                    branch.transform.up = direction;
                    branch.transform.rotation = randomRotation;

                    MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = branch.AddComponent<MeshRenderer>();

                    meshRenderer.sharedMaterial = GetTrunkMaterial();
                    meshFilter.mesh = CreateBranchMesh(gravity, adjustedBranchRadius, adjustedBranchLength, branchBending, direction, branchPosition, randomRotation, bendPositions, branchSegments, branchSubdivision, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);

                    branches.Add(new Branch(branchPosition, direction, adjustedBranchLength, adjustedBranchRadius, randomRotation, bendPositions, i));
                }

                return;
            }

            for (int i = 0; i < numberOfBranches; i++)
            {
                float height = Random.Range(branchHeightMin * trunkHeight, branchHeightMax * trunkHeight);
                if (height > maxBranchHeight)
                {
                    continue;
                }

                float adjustedBranchLength = branchLength;
                if (adjustBranchLengthByHeight == true)
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
                if (useGrowthForDimensions)
                {
                    adjustedBranchRadius *= Mathf.Clamp01(growthFactor);
                }

                float normalizedHeightAlongTrunk = height / trunkHeight;
                int index1 = Mathf.FloorToInt(normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1));
                int index2 = Mathf.Min(index1 + 1, trunk.trunkBendPositions.Count - 1);

                Vector3 trunkBendPosition1 = trunk.trunkBendPositions[index1];
                Vector3 trunkBendPosition2 = trunk.trunkBendPositions[index2];
                float t = normalizedHeightAlongTrunk * (trunk.trunkBendPositions.Count - 1) - index1;

                Vector3 branchPosition = Vector3.Lerp(trunkBendPosition1, trunkBendPosition2, t) + trunkObject.transform.position;
                branchPosition.y = height + trunkObject.transform.position.y;

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
                Vector3 direction = Vector3.forward;
                direction = Quaternion.Euler(-90f, 0f, 0f) * direction;
                direction = randomRotation * direction;

                List<Vector3> bendPositions = new List<Vector3>();

                GameObject branch = new GameObject("Branch" + i);
                branch.transform.SetParent(branchesParent.transform);
                branch.transform.position = branchPosition;
                branch.transform.up = direction;
                branch.transform.rotation = randomRotation;

                MeshFilter meshFilter = branch.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = branch.AddComponent<MeshRenderer>();

                meshRenderer.sharedMaterial = GetTrunkMaterial();
                meshFilter.mesh = CreateBranchMesh(gravity, adjustedBranchRadius, adjustedBranchLength, branchBending, direction, branchPosition, randomRotation, bendPositions, branchSegments, branchSubdivision, ref vertexBranchCount, ref triangleBranchCount, ref edgeBranchCount);

                branches.Add(new Branch(branchPosition, direction, adjustedBranchLength, adjustedBranchRadius, randomRotation, bendPositions, i));
            }
        }

        private Mesh CreateBranchMesh(float gravity, float adjustedBranchRadius, float adjustedBranchLength, float branchBending, Vector3 direction, Vector3 branchPosition, Quaternion randomRotation, List<Vector3> bendPositions, int branchSegments, int branchSubdivision, ref int vertexBranchCount, ref int triangleBranchCount, ref int edgeBranchCount)
        {
            Mesh mesh = new Mesh();
            mesh.name = "HW_BranchMesh";

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
                Vector3 bendPositionWorld = branchPosition + direction * heightFraction * adjustedBranchLength + worldBendOffset;
                bendPositions.Add(bendPositionWorld);

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

                    vertices[y * (radialSegments + 1) + x] = new Vector3(xPos + bendOffset, adjustedBranchLength * heightFraction, zPos + gravityBend);

                    float u = (float)x / radialSegments;
                    float v = heightFraction * adjustedBranchLength;
                    uvs[y * (radialSegments + 1) + x] = new Vector2(u, v);
                }
            }

            int topRingStartIndex = horizontalSegments * (radialSegments + 1);
            Vector3 topVertexPosition = vertices[topRingStartIndex];

            int pointedTipIndex = verticesCount - 1;
            Vector3 pointedTipPosition = topVertexPosition + new Vector3(0, 0f, 0);

            float topRadiusDiameter = topRadius * 2;
            pointedTipPosition.x -= topRadiusDiameter / 2;

            vertices[pointedTipIndex] = pointedTipPosition;
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
            mesh.RecalculateBounds();

            vertexBranchCount += mesh.vertexCount;
            triangleBranchCount += mesh.triangles.Length / 3;
            edgeBranchCount += CalculateEdges(mesh);

            return mesh;
        }

        private void GenerateTreeBranchlets(bool adjustBranchletLengthByHeight, float gravityBranchlets, float branchletAngle, float branchletLength, float branchletForwardAngle, float branchletBending, int branchletSegments, int branchletSubdivision, ref int vertexBranchletCount, ref int triangleBranchletCount, ref int edgeBranchletCount)
        {

            vertexBranchletCount = 0;
            triangleBranchletCount = 0;
            edgeBranchletCount = 0;

            if (branchletsParent != null)
            {
                DestroyObject(branchletsParent);
            }

            branchletsParent = new GameObject("TreeBranchlets");
            branchletsParent.transform.SetParent(trunkObject.transform);

            branchlets.Clear();

            float growthFactor = useGrowthForDimensions ? Mathf.Clamp01(growth01) : 1f;
            float maxBranchletHeight = trunkHeight * Mathf.Clamp01(growthFactor);

            if (usePersistentGraph && persistentGraph != null && persistentGraph.branches.Count > 0)
            {
                foreach (Branch branch in branches)
                {
                    if (branch.persistentIndex < 0 || branch.persistentIndex >= persistentGraph.branches.Count)
                    {
                        continue;
                    }

                    PersistentBranch branchData = persistentGraph.branches[branch.persistentIndex];
                    float branchGrowth = branchesAtTipOnly
                        ? Mathf.Clamp01((growthFactor - minGrowth01ForBranches) / Mathf.Max(0.0001f, 1f - minGrowth01ForBranches))
                        : Mathf.Clamp01(Mathf.InverseLerp(branchData.heightFrac, Mathf.Max(branchData.heightFrac + 0.0001f, 1f), growthFactor));

                    foreach (PersistentBranchlet branchletData in branchData.branchlets)
                    {
                        if (!branchesAtTipOnly && branchletData.alongFrac > branchGrowth)
                        {
                            continue;
                        }

                        float branchletGrowth = branchesAtTipOnly
                            ? Mathf.Clamp01((growthFactor - minGrowth01ForBranchlets) / Mathf.Max(0.0001f, 1f - minGrowth01ForBranchlets))
                            : Mathf.Clamp01(Mathf.InverseLerp(branchletData.alongFrac, 1f, branchGrowth));
                        float adjustedBranchletLength = branchletData.baseLength * branchletData.lengthScale * branchletGrowth;
                        float branchletRadius = branchletData.baseRadius * Mathf.Max(0.05f, branchletGrowth);

                        Vector3 branchletPosition = Vector3.zero;
                        if (branchesAtTipOnly)
                        {
                            if (branch.bendPositions.Count > 0)
                            {
                                branchletPosition = branch.bendPositions[branch.bendPositions.Count - 1];
                            }
                            else
                            {
                                branchletPosition = branch.position + branch.direction.normalized * branch.length;
                            }
                        }
                        else
                        {
                            float targetLength = Mathf.Clamp01(branchletData.alongFrac) * branch.length;
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

                                float desiredLength = Mathf.Clamp(targetLength, 0f, totalLength);
                                float accumulatedLength = 0f;
                                for (int k = 0; k < segmentLengths.Count; k++)
                                {
                                    if (accumulatedLength + segmentLengths[k] >= desiredLength)
                                    {
                                        float segmentFactor = (desiredLength - accumulatedLength) / Mathf.Max(0.0001f, segmentLengths[k]);
                                        branchletPosition = Vector3.Lerp(
                                            branch.bendPositions[k],
                                            branch.bendPositions[k + 1],
                                            segmentFactor
                                        );
                                        break;
                                    }
                                    accumulatedLength += segmentLengths[k];
                                }
                            }
                            else
                            {
                                branchletPosition = branch.position + branch.direction.normalized * targetLength;
                            }
                        }

                        if (branchletPosition.y > maxBranchletHeight)
                        {
                            continue;
                        }

                        Vector3 forwardDirection = branch.direction.normalized;
                        if (branch.bendPositions.Count > 1)
                        {
                            forwardDirection = (branch.bendPositions[Mathf.Min(branch.bendPositions.Count - 1, 1)] - branch.bendPositions[0]).normalized;
                        }
                        Quaternion branchRotation = Quaternion.LookRotation(forwardDirection);

                        float adjustedRotationAngle = branchletData.sideFactor * branchletForwardAngle * branchletData.angleScale;
                        Quaternion fixedRotation = branchRotation;
                        fixedRotation = Quaternion.AngleAxis(adjustedRotationAngle, branchRotation * Vector3.forward) * fixedRotation;
                        fixedRotation = Quaternion.AngleAxis(branchletAngle * branchletData.angleScale, branchRotation * Vector3.right) * fixedRotation;

                        Vector3 direction = Vector3.up;
                        direction = fixedRotation * direction;

                        List<Vector3> bendBranchletPositions = new List<Vector3>();

                        GameObject branchlet = new GameObject("Branchlet" + branchlets.Count);
                        branchlet.transform.SetParent(branchletsParent.transform);
                        branchlet.transform.position = branchletPosition;
                        branchlet.transform.up = forwardDirection;
                        branchlet.transform.rotation = fixedRotation;
                        MeshFilter meshFilter = branchlet.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = branchlet.AddComponent<MeshRenderer>();

                        meshRenderer.sharedMaterial = GetTrunkMaterial();
                        meshFilter.mesh = CreateBranchletMesh(adjustedBranchletLength, gravityBranchlets, branchletRadius, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount, direction, branchletPosition, fixedRotation, branchRotation, bendBranchletPositions);

                        BranchletsX created = new BranchletsX(branchletPosition, direction, adjustedBranchletLength, branchletRadius, branchletData.sideFactor, fixedRotation, bendBranchletPositions, branchletData.childSeed);
                        branchlets.Add(created);

                        if (enableBranchletChildren && branchletChildrenPerBranchlet > 0 && branchletGrowth >= branchletPromoteThreshold)
                        {
                            GenerateChildBranchlets(created, branchletGrowth, gravityBranchlets, branchletAngle, branchletForwardAngle, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount);
                        }
                    }
                }

                return;
            }

            int branchletPerBranch = branchesAtTipOnly ? Mathf.Max(0, branchletsPerBranch) : Mathf.FloorToInt((float)numberOfBranchlets / branches.Count);
            int remainderBranchlets = branchesAtTipOnly ? 0 : numberOfBranchlets % branches.Count;

            int branchletCounter = 0;

            float minHeight = Mathf.Min(branches.ConvertAll(branch => branch.position.y).ToArray());
            float maxHeight = Mathf.Max(branches.ConvertAll(branch => branch.position.y).ToArray());


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

                    float branchletHeight = branchesAtTipOnly ? 1f : Random.Range(branchletHeightMin, branchletHeightMax);
                    branchletHeight = Mathf.Clamp01(branchletHeight);

                    Vector3 branchletPosition = Vector3.zero;

                    if (branch.bendPositions.Count > 1)
                    {
                        if (branchesAtTipOnly)
                        {
                            branchletPosition = branch.bendPositions[branch.bendPositions.Count - 1];
                        }
                        else
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
                                    branchletPosition = Vector3.Lerp(
                                        branch.bendPositions[k],
                                        branch.bendPositions[k + 1],
                                        segmentFactor
                                    );

                                    break;
                                }
                                accumulatedLength += segmentLengths[k];
                            }
                        }
                    }
                    else
                    {
                        branchletPosition = branch.position + branch.direction.normalized * branch.length * branchletHeight;
                    }

                    if (branchletPosition.y > maxBranchletHeight)
                    {
                        continue;
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


                    Vector3 direction = Vector3.up;

                    direction = fixedRotation * direction;

                    float branchletRadius = CalculateBranchletRadius(branchletPosition, branch);
                    if (useGrowthForDimensions)
                    {
                        branchletRadius *= Mathf.Clamp01(growthFactor);
                    }

                    float branchletMaxRadius = branchletRadius;

                    List<Vector3> bendBranchletPositions = new List<Vector3>();

                    GameObject branchlet = new GameObject("Branchlet" + branchletCounter++);
                    branchlet.transform.SetParent(branchletsParent.transform);
                    branchlet.transform.position = branchletPosition;
                    branchlet.transform.up = forwardDirection;
                    branchlet.transform.rotation = fixedRotation;
                    MeshFilter meshFilter = branchlet.AddComponent<MeshFilter>();
                    MeshRenderer meshRenderer = branchlet.AddComponent<MeshRenderer>();

                    meshRenderer.sharedMaterial = GetTrunkMaterial();
                    meshFilter.mesh = CreateBranchletMesh(adjustedBranchletLength, gravityBranchlets, branchletRadius, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount, direction, branchletPosition, fixedRotation, branchRotation, bendBranchletPositions);

                    branchlets.Add(new BranchletsX(branchletPosition, direction, branchletLength, branchletMaxRadius, sideFactor, fixedRotation, bendBranchletPositions, Random.Range(int.MinValue, int.MaxValue)));
                }
            }
        }

        private void GenerateSpaceColonization(float growthT)
        {
            vertexBranchletCount = 0;
            triangleBranchletCount = 0;
            edgeBranchletCount = 0;
            if (!scInitialized)
            {
                InitializeSpaceColonization();
            }

            if (spaceColonizationParent != null)
            {
                DestroyObject(spaceColonizationParent);
            }

            spaceColonizationParent = new GameObject("SpaceColonization");
            spaceColonizationParent.transform.SetParent(trunkObject.transform);
            branchletsParent = spaceColonizationParent;
            branchletsParent = spaceColonizationParent;
            branchlets.Clear();

            // [BIO-MIMETIC FIX] Sync SC Root Nodes with dynamic Hybrid Branch tips
            // Because L-System branches grow/move, we must update the SC start points.
            if (scInitialized && branches.Count > 0 && scRootIndices.Count > 0)
            {
                int count = Mathf.Min(branches.Count, scRootIndices.Count);
                for(int i=0; i<count; i++)
                {
                   int nodeIdx = scRootIndices[i];
                   if (nodeIdx < 0 || nodeIdx >= scNodes.Count) continue;
                   
                   Branch branch = branches[i];
                   Vector3 tipWorld = branch.bendPositions.Count > 0 ? branch.bendPositions[branch.bendPositions.Count - 1] : (branch.position + branch.direction.normalized * branch.length);
                   scNodes[nodeIdx].position = tipWorld - scOrigin;
                   // Also update direction if needed?
                   scNodes[nodeIdx].direction = branch.direction.normalized; 
                }
            }

            UpdateSpaceColonizationGrowth(growthT);
            if (scThicknessTimer >= scThicknessUpdateInterval)
            {
                scThicknessTimer = 0f;
                RecalculateSpaceColonizationRadii();
            }

            Vector3 origin = scOrigin;
            for (int i = 0; i < scNodes.Count; i++)
            {
                SCNode node = scNodes[i];
                if (node.parent < 0) continue;

                SCNode parent = scNodes[node.parent];
                Vector3 segment = node.position - parent.position;
                float length = segment.magnitude;
                if (length <= 0.0001f) continue;

                Vector3 direction = segment / length;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);

                Vector3 parentWorld = origin + parent.position;
                Vector3 nodeWorld = origin + node.position;
                List<Vector3> bendPositions = new List<Vector3> { parentWorld, nodeWorld };

                GameObject branchlet = new GameObject("SC_Branchlet" + i);
                branchlet.transform.SetParent(spaceColonizationParent.transform);
                branchlet.transform.position = parentWorld;
                branchlet.transform.up = direction;
                branchlet.transform.rotation = rotation;
                MeshFilter meshFilter = branchlet.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = branchlet.AddComponent<MeshRenderer>();

                meshRenderer.sharedMaterial = GetTrunkMaterial();
                meshFilter.mesh = CreateBranchletMesh(length, 0f, Mathf.Max(0.001f, node.radius), 0f, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount, direction, parentWorld, rotation, rotation, bendPositions);

                branchlets.Add(new BranchletsX(parentWorld, direction, length, Mathf.Max(0.001f, node.radius), 1f, rotation, bendPositions, 0));
            }
        }

        private void InitializeSpaceColonization()
        {
            scInitialized = true;
            scIterationCount = 0;
            scAttractors.Clear();
            scNodes.Clear();
            scRootIndices.Clear();
            scRootCaps.Clear();

            int seed = persistentGraphSeed != 0 ? persistentGraphSeed : randomSeed;
            scRng = new System.Random(seed);

            scOrigin = trunkObject.transform.position;
            Vector3 trunkTip = trunk.trunkBendPositions[trunk.trunkBendPositions.Count - 1] + scOrigin;
            Vector3 crownCenter = trunkTip + scCrownCenterOffset;
            Vector3 crownCenterLocal = crownCenter - scOrigin;

            for (int i = 0; i < Mathf.Max(0, scAttractorCount); i++)
            {
                Vector3 inside = NextInsideUnitSphere(scRng);
                if (scUpperHemisphereOnly)
                {
                    inside.y = Mathf.Abs(inside.y);
                }
                float mag = inside.magnitude;
                if (mag > 0.0001f)
                {
                    Vector3 norm = inside / mag;
                    float biasedY = Mathf.Lerp(norm.y, 1f, Mathf.Clamp01(scAttractorUpBias));
                    norm.y = biasedY;
                    norm.Normalize();
                    inside = norm * mag;
                }
                Vector3 local = new Vector3(inside.x * scCrownSize.x, inside.y * scCrownSize.y, inside.z * scCrownSize.z);
                scAttractors.Add(new SCAttractor(crownCenterLocal + local));
            }

            if (branches.Count > 0)
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    Branch branch = branches[i];
                    Vector3 tipWorld = branch.bendPositions.Count > 0 ? branch.bendPositions[branch.bendPositions.Count - 1] : (branch.position + branch.direction.normalized * branch.length);
                    Vector3 tip = tipWorld - scOrigin;
                    Vector3 dir = branch.direction.normalized;
                    int nodeIndex = scNodes.Count;
                    scNodes.Add(new SCNode(tip, dir, -1));
                    scRootIndices.Add(nodeIndex);
                    float tipRadius = branch.adjustedBranchRadius * Mathf.Clamp01(1f - branchRadiusCurvature);
                    scRootCaps.Add(Mathf.Max(0.001f, tipRadius));
                }
            }
            else
            {
                int nodeIndex = scNodes.Count;
                scNodes.Add(new SCNode(trunkTip - scOrigin, Vector3.up, -1));
                scRootIndices.Add(nodeIndex);
                scRootCaps.Add(Mathf.Max(0.001f, trunkRadius * Mathf.Clamp01(1f - trunkRadiusCurvature)));
            }

            RecalculateSpaceColonizationRadii();
        }

        private void UpdateSpaceColonizationGrowth(float growthFactor)
        {
            float start = Mathf.Clamp01(spaceColonizationStartGrowth01);
            float progress = Mathf.Clamp01((growthFactor - start) / Mathf.Max(0.0001f, 1f - start));
            int targetIterations = Mathf.RoundToInt(scIterationsPerFullGrowth * progress);

            while (scIterationCount < targetIterations)
            {
                bool grew = StepSpaceColonization();
                scIterationCount++;
                if (!grew)
                {
                    break;
                }
            }
        }

        private bool StepSpaceColonization()
        {
            if (scAttractors.Count == 0 || scNodes.Count == 0) return false;

            Vector3[] directionSum = new Vector3[scNodes.Count];
            int[] directionCount = new int[scNodes.Count];
            List<int> removeIndices = new List<int>();

            for (int i = 0; i < scAttractors.Count; i++)
            {
                SCAttractor attractor = scAttractors[i];
                float closestDistSq = float.MaxValue;
                int closestIndex = -1;

                for (int n = 0; n < scNodes.Count; n++)
                {
                    Vector3 diff = attractor.position - scNodes[n].position;
                    float distSq = diff.sqrMagnitude;
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestIndex = n;
                    }
                }

                if (closestIndex < 0) continue;
                float dist = Mathf.Sqrt(closestDistSq);
                if (dist <= scKillRadius)
                {
                    removeIndices.Add(i);
                    continue;
                }

                if (dist <= scInfluenceRadius)
                {
                    Vector3 dir = (attractor.position - scNodes[closestIndex].position).normalized;
                    directionSum[closestIndex] += dir;
                    directionCount[closestIndex] += 1;
                }
            }

            for (int i = removeIndices.Count - 1; i >= 0; i--)
            {
                int index = removeIndices[i];
                if (index >= 0 && index < scAttractors.Count)
                {
                    scAttractors.RemoveAt(index);
                }
            }

            List<SCNode> newNodes = new List<SCNode>();
            for (int i = 0; i < scNodes.Count; i++)
            {
                if (directionCount[i] == 0) continue;
                Vector3 averageDir = directionSum[i] / directionCount[i];
                if (averageDir.sqrMagnitude <= 0.0001f) continue;
                Vector3 dir = (averageDir + Vector3.up * Mathf.Clamp01(scUpBias)).normalized;
                Vector3 newPos = scNodes[i].position + dir * scSegmentLength;

                if (IsSpaceColonizationPositionOccupied(newPos))
                {
                    continue;
                }

                SCNode node = new SCNode(newPos, dir, i);
                newNodes.Add(node);
            }

            for (int i = 0; i < newNodes.Count; i++)
            {
                int index = scNodes.Count;
                scNodes.Add(newNodes[i]);
                scNodes[newNodes[i].parent].children.Add(index);
            }

            return newNodes.Count > 0;
        }

        private bool IsSpaceColonizationPositionOccupied(Vector3 position)
        {
            float minDistSq = (scSegmentLength * 0.5f) * (scSegmentLength * 0.5f);
            for (int i = 0; i < scNodes.Count; i++)
            {
                if ((scNodes[i].position - position).sqrMagnitude <= minDistSq)
                {
                    return true;
                }
            }
            return false;
        }

        private void RecalculateSpaceColonizationRadii()
        {
            for (int i = 0; i < scNodes.Count; i++)
            {
                scNodes[i].radius = 0f;
            }

            for (int i = scNodes.Count - 1; i >= 0; i--)
            {
                SCNode node = scNodes[i];
                if (node.children.Count == 0)
                {
                    node.radius = scTipRadius;
                }
                else
                {
                    float sum = 0f;
                    for (int c = 0; c < node.children.Count; c++)
                    {
                        float childRadius = scNodes[node.children[c]].radius;
                        sum += childRadius * childRadius;
                    }
                    node.radius = Mathf.Sqrt(sum);
                }
            }

            float rootSum = 0f;
            for (int i = 0; i < scRootIndices.Count; i++)
            {
                int index = scRootIndices[i];
                if (index < 0 || index >= scNodes.Count) continue;
                float cap = i < scRootCaps.Count ? scRootCaps[i] : scNodes[index].radius;
                scNodes[index].radius = Mathf.Min(scNodes[index].radius, cap);
                rootSum += scNodes[index].radius * scNodes[index].radius;
            }

            scRootRadiusHint = Mathf.Sqrt(rootSum);
        }

        private Vector3 NextInsideUnitSphere(System.Random rng)
        {
            while (true)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                float y = (float)(rng.NextDouble() * 2.0 - 1.0);
                float z = (float)(rng.NextDouble() * 2.0 - 1.0);
                Vector3 v = new Vector3(x, y, z);
                if (v.sqrMagnitude <= 1f)
                {
                    return v;
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
            mesh.name = "HW_BranchletMesh";

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

                float bendNoise = Mathf.PerlinNoise(branchletBendNoiseSeed, heightFraction * 2f); // Lower frequency
                float noiseFactor1 = Mathf.Lerp(0.8f, 1.2f, bendNoise);
                // Removed high-freq sine wave (30 * ...) to fix "squiggly" look
                float bendOffset = (bendNoise - 0.5f) * branchletBending * adjustedBranchletLength * 0.2f;

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

                    vertices[y * (radialSegments + 1) + x] = new Vector3(xPos + bendOffset, heightFraction * adjustedBranchletLength, zPos + gravityBend);

                    float u = (float)x / radialSegments;
                    float v = heightFraction * adjustedBranchletLength;
                    uvs[y * (radialSegments + 1) + x] = new Vector2(u, v);
                }
            }

            int topRingStartIndex = horizontalSegments * (radialSegments + 1);
            Vector3 topVertexPosition = vertices[topRingStartIndex];

            int pointedTipIndex = verticesCount - 1;
            Vector3 pointedTipPosition = topVertexPosition + new Vector3(0, 0f, 0);

            float topRadiusDiameter = topRadius * 2;
            pointedTipPosition.x -= topRadiusDiameter / 2;

            vertices[pointedTipIndex] = pointedTipPosition;
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
            mesh.RecalculateBounds();

            vertexBranchletCount += mesh.vertexCount;
            triangleBranchletCount += mesh.triangles.Length / 3;
            edgeBranchletCount += CalculateEdges(mesh);

            return mesh;
        }

        private void GenerateChildBranchlets(BranchletsX parent, float parentGrowth, float gravityBranchlets, float branchletAngle, float branchletForwardAngle, float branchletBending, int branchletSegments, int branchletSubdivision, ref int vertexBranchletCount, ref int triangleBranchletCount, ref int edgeBranchletCount)
        {
            if (parent == null || parent.bendPositions == null || parent.bendPositions.Count < 2) return;

            Vector3 forwardDirection = parent.direction.normalized;
            if (parent.bendPositions.Count > 1)
            {
                forwardDirection = (parent.bendPositions[Mathf.Min(parent.bendPositions.Count - 1, 1)] - parent.bendPositions[0]).normalized;
            }
            Quaternion branchRotation = Quaternion.LookRotation(forwardDirection);

            float totalLength = 0f;
            List<float> segmentLengths = new List<float>();
            for (int k = 0; k < parent.bendPositions.Count - 1; k++)
            {
                float segmentLength = Vector3.Distance(parent.bendPositions[k], parent.bendPositions[k + 1]);
                segmentLengths.Add(segmentLength);
                totalLength += segmentLength;
            }

            int seed = parent.childSeed;
            System.Random rng = new System.Random(seed);

            for (int i = 0; i < branchletChildrenPerBranchlet; i++)
            {
                float alongFrac = branchesAtTipOnly ? 1f : Mathf.Lerp(branchletHeightMin, branchletHeightMax, (float)rng.NextDouble());
                if (alongFrac > parentGrowth)
                {
                    continue;
                }

                float asym = Mathf.Pow((float)rng.NextDouble(), Mathf.Max(0.1f, yDominantBias)) * Mathf.Clamp01(maxAsymmetry);
                float sideFactor = (i % 2 == 0) ? 1f : -1f;
                float lengthScale = sideFactor > 0f ? (1f + asym) : (1f - asym);
                float angleScale = sideFactor > 0f ? (1f + asym) : (1f - asym);

                float childGrowth = Mathf.Clamp01(Mathf.InverseLerp(alongFrac, 1f, parentGrowth));
                float adjustedLength = parent.length * 0.7f * lengthScale * childGrowth;
                float radius = parent.branchletMaxRadius * 0.7f * Mathf.Max(0.05f, childGrowth);

                float desiredLength = Mathf.Clamp01(alongFrac) * totalLength;
                Vector3 childPosition = branchesAtTipOnly ? parent.bendPositions[parent.bendPositions.Count - 1] : parent.bendPositions[0];

                float accumulatedLength = 0f;
                if (!branchesAtTipOnly)
                {
                    for (int k = 0; k < segmentLengths.Count; k++)
                    {
                        if (accumulatedLength + segmentLengths[k] >= desiredLength)
                        {
                            float segmentFactor = (desiredLength - accumulatedLength) / Mathf.Max(0.0001f, segmentLengths[k]);
                            childPosition = Vector3.Lerp(parent.bendPositions[k], parent.bendPositions[k + 1], segmentFactor);
                            break;
                        }
                        accumulatedLength += segmentLengths[k];
                    }
                }

                float adjustedRotationAngle = sideFactor * branchletForwardAngle * angleScale;
                Quaternion fixedRotation = branchRotation;
                fixedRotation = Quaternion.AngleAxis(adjustedRotationAngle, branchRotation * Vector3.forward) * fixedRotation;
                fixedRotation = Quaternion.AngleAxis(branchletAngle * angleScale, branchRotation * Vector3.right) * fixedRotation;

                Vector3 direction = Vector3.up;
                direction = fixedRotation * direction;

                List<Vector3> bendPositions = new List<Vector3>();

                GameObject branchlet = new GameObject("BranchletChild");
                branchlet.transform.SetParent(branchletsParent.transform);
                branchlet.transform.position = childPosition;
                branchlet.transform.up = forwardDirection;
                branchlet.transform.rotation = fixedRotation;
                MeshFilter meshFilter = branchlet.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = branchlet.AddComponent<MeshRenderer>();

                meshRenderer.sharedMaterial = GetTrunkMaterial();
                meshFilter.mesh = CreateBranchletMesh(adjustedLength, gravityBranchlets, radius, branchletBending, branchletSegments, branchletSubdivision, ref vertexBranchletCount, ref triangleBranchletCount, ref edgeBranchletCount, direction, childPosition, fixedRotation, branchRotation, bendPositions);

                branchlets.Add(new BranchletsX(childPosition, direction, adjustedLength, radius, sideFactor, fixedRotation, bendPositions, rng.Next()));
            }
        }

        private void GenerateLeafPlanes(Vector3 leafBranchPositioning, Vector3 leafBranchSizeV3, Material leafMaterial, int numberOfLeaves, ref int vertexBranchLeavesCount, ref int triangleBranchLeavesCount, ref int edgeBranchLeavesCount, GameObject leafPrefab, float leafSizeBranchRandom, float leafSize, float leafPositionMin, float leafPositionMax, float leafForwardRotation, float leafRotation, float leafRandomizeRotation, float leafBranchRandomPositioning)
        {
            vertexBranchLeavesCount = 0;
            triangleBranchLeavesCount = 0;
            edgeBranchLeavesCount = 0;

            if (branchesParent == null)
            {
                Debug.LogWarning("Branches must be generated before generating leaves.");
                return;
            }

            leafPositionMin = Mathf.Clamp01(leafPositionMin);
            leafPositionMax = Mathf.Clamp01(leafPositionMax);

            if (leafPositionMin > leafPositionMax)
            {
                Debug.LogWarning("leafPositionMin cannot be greater than leafPositionMax. Swapping the values.");
                (leafPositionMin, leafPositionMax) = (leafPositionMax, leafPositionMin);
            }

            Transform oldLeaves = trunkObject.transform.Find("Leaves Branch");
            if (oldLeaves != null)
            {
                DestroyObject(oldLeaves.gameObject);
            }

            GameObject leavesParent = new GameObject("Leaves Branch");
            leavesParent.transform.SetParent(trunkObject.transform);
            leavesBranch = leavesParent;

            foreach (Branch branch in branches)
            {
                for (int i = 0; i < numberOfLeaves; i++)
                {
                    float adjustedLeafRotation = (i % 2 == 0) ? leafRotation : -leafRotation;

                    float randomizedLeafForwardRotation = leafForwardRotation + Random.Range(-360f, 360f) * leafRandomizeRotation;
                    float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafRandomizeRotation;

                    float tMin = leafPositionMin;
                    float tMax = leafPositionMax;
                    if (useLeafEndDistance && branch.length > 0.0001f)
                    {
                        float endT = 1f - (leafEndDistanceMeters / branch.length);
                        tMin = Mathf.Max(tMin, Mathf.Clamp01(endT));
                        tMax = Mathf.Max(tMin, Mathf.Clamp01(tMax));
                    }

                    float t = Random.Range(tMin, tMax);

                    int segmentCount = branch.bendPositions.Count - 1;
                    int segmentIndex = Mathf.FloorToInt(t * segmentCount);
                    float segmentT = (t * segmentCount) - segmentIndex;

                    segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);

                    Vector3 leafPosition = Vector3.Lerp(
                        branch.bendPositions[segmentIndex],
                        branch.bendPositions[segmentIndex + 1],
                        segmentT
                    );

                    leafPosition += leafBranchPositioning;

                    Quaternion leafRotationQuaternion = branch.randomRotation;

                    leafRotationQuaternion = Quaternion.Euler(randomizedLeafRotation, randomizedLeafForwardRotation, 0f);

                    Vector3 localDirection = (branch.bendPositions[segmentIndex + 1] - branch.bendPositions[segmentIndex]).normalized;

                    Quaternion branchRotation = Quaternion.LookRotation(localDirection);
                    Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, 0f);
                    Quaternion finalRotation = branchRotation * customRotation;


                    Vector3 leafDirection = branch.direction.normalized;

                    leafDirection = leafRotationQuaternion * leafDirection;

                    GameObject leaf = Instantiate(leafPrefab);
                    leaf.name = "Leaf";
                    leaf.transform.SetParent(leavesParent.transform);

                    leaf.transform.position = leafPosition;
                    leaf.transform.up = leafDirection;
                    leaf.transform.rotation = finalRotation;


                    float randomSizeMultiplier = 1 + (Random.Range(-leafSizeBranchRandom, leafSizeBranchRandom));
                    leaf.transform.localScale = leafBranchSizeV3 * leafSize * randomSizeMultiplier;


                    MeshRenderer leafRenderer = leaf.GetComponent<MeshRenderer>();
                    leafRenderer.sharedMaterial = leafMaterial;

                    leaf.transform.position += new Vector3(
                        Random.Range(0f, leafBranchRandomPositioning),
                        Random.Range(0f, leafBranchRandomPositioning),
                        Random.Range(0f, leafBranchRandomPositioning)
                    );

                    MeshFilter leafMeshFilter = leaf.GetComponent<MeshFilter>();
                    if (leafMeshFilter != null && leafMeshFilter.sharedMesh != null)
                    {
                        Mesh leafMesh = leafMeshFilter.sharedMesh;

                        vertexBranchLeavesCount += leafMesh.vertexCount;
                        triangleBranchLeavesCount += leafMesh.triangles.Length / 3;
                        edgeBranchLeavesCount += CalculateEdges(leafMesh);
                    }
                }
            }
        }

        private void GenerateLeafBranchletPlanes(Vector3 leafBranchletPositioning, Vector3 leafBranchletSizeV3, Material leafMaterial, int numberOfLeavesBranchlet, ref int vertexBranchletLeavesCount, ref int triangleBranchletLeavesCount, ref int edgeBranchletLeavesCount, GameObject leafPrefab, float leafSizeBranchletRandom, float leafBranchletSize, float leafBranchletPositionMin, float leafBranchletPositionMax, float leafBranchletForwardRotation, float leafBranchletRotation, float leafBranchletRandomizeRotation, float leafBranchletRandomPositioning)
        {
            vertexBranchletLeavesCount = 0;
            triangleBranchletLeavesCount = 0;
            edgeBranchletLeavesCount = 0;

            if (branchletsParent == null)
            {
                Debug.LogWarning("Branchlets must be generated before generating leaves.");
                return;
            }

            leafBranchletPositionMin = Mathf.Clamp01(leafBranchletPositionMin);
            leafBranchletPositionMax = Mathf.Clamp01(leafBranchletPositionMax);

            if (leafBranchletPositionMin > leafBranchletPositionMax)
            {
                Debug.LogWarning("leafBranchletPositionMin cannot be greater than leafBranchletPositionMax. Swapping the values.");
                (leafBranchletPositionMin, leafBranchletPositionMax) = (leafBranchletPositionMax, leafBranchletPositionMin);
            }

            Transform oldLeaves = trunkObject.transform.Find("Leaves Branchlet");
            if (oldLeaves != null)
            {
                DestroyObject(oldLeaves.gameObject);
            }

            GameObject leavesParent = new GameObject("Leaves Branchlet");
            leavesParent.transform.SetParent(trunkObject.transform);
            leavesBranchlet = leavesParent;

            foreach (BranchletsX branchlet in branchlets)
            {
                for (int i = 0; i < numberOfLeavesBranchlet; i++)
                {
                    float adjustedLeafRotation = (i % 2 == 0) ? leafBranchletRotation : -leafBranchletRotation;

                    float randomizedLeafForwardRotation = leafBranchletForwardRotation + Random.Range(-360f, 360f) * leafBranchletRandomizeRotation;
                    float randomizedLeafRotation = adjustedLeafRotation + Random.Range(-180f, 180f) * leafBranchletRandomizeRotation;

                    float tMin = leafBranchletPositionMin;
                    float tMax = leafBranchletPositionMax;
                    if (useBranchletLeafEndDistance && branchlet.length > 0.0001f)
                    {
                        float endT = 1f - (branchletLeafEndDistanceMeters / branchlet.length);
                        tMin = Mathf.Max(tMin, Mathf.Clamp01(endT));
                        tMax = Mathf.Max(tMin, Mathf.Clamp01(tMax));
                    }

                    float t = Random.Range(tMin, tMax);

                    int segmentCount = branchlet.bendPositions.Count - 1;
                    int segmentIndex = Mathf.FloorToInt(t * segmentCount);
                    float segmentT = (t * segmentCount) - segmentIndex;
                    segmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);

                    Vector3 leafBranchletPosition = Vector3.Lerp(
                        branchlet.bendPositions[segmentIndex],
                        branchlet.bendPositions[segmentIndex + 1],
                        segmentT
                    );

                    leafBranchletPosition += leafBranchletPositioning;


                    Quaternion leafBranchletRotationQuaternion = branchlet.fixedRotation;

                    leafBranchletRotationQuaternion = Quaternion.Euler(randomizedLeafRotation, randomizedLeafForwardRotation, 0f);

                    Vector3 localDirection = (branchlet.bendPositions[segmentIndex + 1] - branchlet.bendPositions[segmentIndex]).normalized;

                    Quaternion branchletRotation = Quaternion.LookRotation(localDirection);
                    Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, 0f);
                    Quaternion finalRotation = branchletRotation * customRotation;


                    Vector3 leafDirection = branchlet.direction.normalized;

                    leafDirection = leafBranchletRotationQuaternion * leafDirection;

                    GameObject leaf = Instantiate(leafPrefab);
                    leaf.name = "Leaf";
                    leaf.transform.SetParent(leavesParent.transform);

                    leaf.transform.position = leafBranchletPosition;
                    leaf.transform.up = leafDirection;
                    leaf.transform.rotation = finalRotation;


                    float randomSizeMultiplier = 1 + (Random.Range(-leafSizeBranchletRandom, leafSizeBranchletRandom));
                    leaf.transform.localScale = leafBranchletSizeV3 * leafBranchletSize * randomSizeMultiplier;


                    MeshRenderer leafRenderer = leaf.GetComponent<MeshRenderer>();
                    leafRenderer.sharedMaterial = leafMaterial;

                    leaf.transform.position += new Vector3(
                        Random.Range(0f, leafBranchletRandomPositioning),
                        Random.Range(0f, leafBranchletRandomPositioning),
                        Random.Range(0f, leafBranchletRandomPositioning)
                    );

                    MeshFilter leafMeshFilter = leaf.GetComponent<MeshFilter>();
                    if (leafMeshFilter != null && leafMeshFilter.sharedMesh != null)
                    {
                        Mesh leafMesh = leafMeshFilter.sharedMesh;

                        vertexBranchletLeavesCount += leafMesh.vertexCount;
                        triangleBranchletLeavesCount += leafMesh.triangles.Length / 3;
                        edgeBranchletLeavesCount += CalculateEdges(leafMesh);
                    }
                }
            }
        }

        private void GenerateLeafTrunkPlanes(Vector3 leafTrunkPositioning, Vector3 leafSizeTrunkV3, Material leafMaterial, int numberOfLeavesTrunk, ref int vertexTrunkLeavesCount, ref int triangleTrunkLeavesCount, ref int edgeTrunkLeavesCount, GameObject leafPrefab, float leafSizeTrunkRandom, float leafTrunkSize, float leafTrunkPositionMin, float leafTrunkPositionMax, float leafTrunkForwardRotation, float leafTrunkRotation, float leafTrunkRandomizeRotation, float leafTrunkRandomPositioning)
        {
            vertexTrunkLeavesCount = 0;
            triangleTrunkLeavesCount = 0;
            edgeTrunkLeavesCount = 0;

            if (trunkObject == null || trunk == null)
            {
                Debug.LogWarning("Trunk must be generated before generating leaves.");
                return;
            }

            leafTrunkPositionMin = Mathf.Clamp01(leafTrunkPositionMin);
            leafTrunkPositionMax = Mathf.Clamp01(leafTrunkPositionMax);

            if (leafTrunkPositionMin > leafTrunkPositionMax)
            {
                Debug.LogWarning("leafTrunkPositionMin cannot be greater than leafTrunkPositionMax. Swapping the values.");
                (leafTrunkPositionMin, leafTrunkPositionMax) = (leafTrunkPositionMax, leafTrunkPositionMin);
            }

            Transform oldLeaves = trunkObject.transform.Find("Leaves Trunk");
            if (oldLeaves != null)
            {
                DestroyObject(oldLeaves.gameObject);
            }

            GameObject leavesParent = new GameObject("Leaves Trunk");
            leavesParent.transform.SetParent(trunkObject.transform);
            leavesTrunk = leavesParent;

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
                Vector3 leafTrunkPosition = Vector3.Lerp(trunkBendPosition1, trunkBendPosition2, segmentT) + trunkObject.transform.position;

                leafTrunkPosition += leafTrunkPositioning;

                float segmentAngle = 360f / numberOfLeavesTrunk;
                float baseRotationAngle = i * segmentAngle;
                float randomVariation = Random.Range(-segmentAngle / 4f, segmentAngle / 4f);
                float randomRotationAngle = baseRotationAngle + randomVariation;

                Vector3 localDirection = (trunkBendPosition2 - trunkBendPosition1).normalized;

                Quaternion trunkRotation = Quaternion.LookRotation(localDirection);
                Quaternion customRotation = Quaternion.Euler(randomizedLeafForwardRotation, randomizedLeafRotation, randomRotationAngle);
                Quaternion finalRotation = trunkRotation * customRotation;

                GameObject leaf = Instantiate(leafPrefab);
                leaf.name = "Leaf";
                leaf.transform.SetParent(leavesParent.transform);

                leaf.transform.position = leafTrunkPosition;
                leaf.transform.rotation = finalRotation;

                float randomSizeMultiplier = 1 + (Random.Range(-leafSizeTrunkRandom, leafSizeTrunkRandom));
                leaf.transform.localScale = leafSizeTrunkV3 * leafTrunkSize * randomSizeMultiplier;

                MeshRenderer leafRenderer = leaf.GetComponent<MeshRenderer>();
                leafRenderer.sharedMaterial = leafMaterial;

                leaf.transform.position += new Vector3(
                    Random.Range(0f, leafTrunkRandomPositioning),
                    Random.Range(0f, leafTrunkRandomPositioning),
                    Random.Range(0f, leafTrunkRandomPositioning)
                );

                MeshFilter leafMeshFilter = leaf.GetComponent<MeshFilter>();
                if (leafMeshFilter != null && leafMeshFilter.sharedMesh != null)
                {
                    Mesh leafMesh = leafMeshFilter.sharedMesh;

                    vertexTrunkLeavesCount += leafMesh.vertexCount;
                    triangleTrunkLeavesCount += leafMesh.triangles.Length / 3;
                    edgeTrunkLeavesCount += CalculateEdges(leafMesh);
                }
            }
        }

        private void GenerateTrueLeaves(GameObject prefab, Material material, ref int vertexCount, ref int triangleCount, ref int edgeCount)
        {
            vertexCount = 0;
            triangleCount = 0;
            edgeCount = 0;

            if (trunkObject == null || trunk == null) return;

            Transform oldLeaves = trunkObject.transform.Find("True Leaves");
            if (oldLeaves != null) DestroyObject(oldLeaves.gameObject);

            GameObject leavesParent = new GameObject("True Leaves");
            leavesParent.transform.SetParent(trunkObject.transform);

            for (int i = 0; i < trueLeavesPairs; i++)
            {
                float currentHeight = trueLeavesStartHeight + (i * trueLeavesInterval);
                if (currentHeight > 1.0f) break;

                // Find position on trunk
                int segmentCount = trunk.trunkBendPositions.Count - 1;
                int index1 = Mathf.FloorToInt(currentHeight * segmentCount);
                int index2 = Mathf.Clamp(index1 + 1, 0, segmentCount);
                float segmentT = (currentHeight * segmentCount) - index1;

                Vector3 p1 = trunk.trunkBendPositions[index1];
                Vector3 p2 = trunk.trunkBendPositions[index2];
                Vector3 pos = Vector3.Lerp(p1, p2, segmentT) + trunkObject.transform.position;
                // Calculate Directions
                Vector3 trunkUp = (p2 - p1).normalized;
                Quaternion trunkRot = Quaternion.LookRotation(trunkUp); // World Rotation of trunk segment

                // --- Pair 1 (Left) ---
                float angle1 = i * trueLeavesAngleOffset;
                
                // 1. Outward Direction (Radial)
                Vector3 radialLocal1 = Quaternion.Euler(0, 0, angle1) * Vector3.right; 
                Vector3 outwardDir1 = trunkRot * radialLocal1;

                // 2. Surface Position
                float topRadius = trunkRadius * Mathf.Clamp01(1 - trunkRadiusCurvature);
                float bottomRadius = trunkRadius * Mathf.Clamp01(1 + trunkRadiusCurvature);
                float radiusAtHeight = Mathf.Lerp(bottomRadius, topRadius, currentHeight);
                Vector3 surfacePos1 = pos + (outwardDir1 * radiusAtHeight);
                
                // 3. Rotation (Z=Outward, Y=TrunkUp) + User Offset
                Quaternion naturalRot1 = Quaternion.LookRotation(outwardDir1, trunkUp);
                Quaternion userRot = Quaternion.Euler(trueLeavesForwardRotation, 0, trueLeavesRotation); 
                // Random Twist
                float randomTwist1 = Random.Range(-trueLeavesRotationRandom, trueLeavesRotationRandom);
                Quaternion finalRot1 = naturalRot1 * userRot * Quaternion.Euler(0, 0, randomTwist1);

                SpawnTrueLeaf(prefab, material, leavesParent, surfacePos1, finalRot1, ref vertexCount, ref triangleCount, ref edgeCount);

                // --- Pair 2 (Right) ---
                float angle2 = angle1 + 180f;
                // 1. Outward
                Vector3 radialLocal2 = Quaternion.Euler(0, 0, angle2) * Vector3.right;
                Vector3 outwardDir2 = trunkRot * radialLocal2;
                // 2. Position
                Vector3 surfacePos2 = pos + (outwardDir2 * radiusAtHeight);
                // 3. Rotation
                Quaternion naturalRot2 = Quaternion.LookRotation(outwardDir2, trunkUp);
                float randomTwist2 = Random.Range(-trueLeavesRotationRandom, trueLeavesRotationRandom);
                Quaternion finalRot2 = naturalRot2 * userRot * Quaternion.Euler(0, 0, randomTwist2);
                
                SpawnTrueLeaf(prefab, material, leavesParent, surfacePos2, finalRot2, ref vertexCount, ref triangleCount, ref edgeCount);
            }
        }

        private void SpawnTrueLeaf(GameObject prefab, Material mat, GameObject parent, Vector3 pos, Quaternion rot, ref int vCount, ref int tCount, ref int eCount)
        {
            GameObject leaf = Instantiate(prefab);
            leaf.name = "TrueLeaf";
            leaf.transform.SetParent(parent.transform);
            leaf.transform.position = pos;
            leaf.transform.rotation = rot;
            leaf.transform.localScale = trueLeavesSizeV3 * trueLeavesSize;

            MeshRenderer mr = leaf.GetComponent<MeshRenderer>();
            if(mr) mr.sharedMaterial = mat;

            MeshFilter mf = leaf.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                vCount += mf.sharedMesh.vertexCount;
                tCount += mf.sharedMesh.triangles.Length / 3;
                eCount += CalculateEdges(mf.sharedMesh);
            }
        }

        private void GenerateCotyledons(GameObject prefab, Material material, float scale, ref int vertexCount, ref int triangleCount, ref int edgeCount)
        {
            if (prefab == null || trunk == null) return;
            
            Transform container = trunkObject.transform.Find("Cotyledons");
            if (container != null) DestroyObject(container.gameObject);
            
            if (scale <= 0.01f) return;

            GameObject leavesParent = new GameObject("Cotyledons");
            leavesParent.transform.SetParent(trunkObject.transform);

            // Cotyledons are always 2, opposite, near the top, or at specific height.
            // Let's place them at 90% of current height if young.
            float heightFrac = 0.9f; 
            Vector3 pos = GetTrunkPositionAt(heightFrac);
            Vector3 dir = GetTrunkDirectionAt(heightFrac);
            
            // Perpendicular to trunk direction
            Vector3 right = Vector3.Cross(dir, Vector3.forward).normalized;
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;

            for(int i=0; i<2; i++)
            {
                GameObject leaf = Instantiate(prefab);
                leaf.transform.SetParent(leavesParent.transform);
                leaf.transform.position = pos;
                
                // Opposite: 0 and 180 degrees
                float angle = i * 180f;
                // Rotate vector around 'dir'
                Vector3 leafDir = Quaternion.AngleAxis(angle, dir) * right;
                
                // Orient leaf to face out
                leaf.transform.rotation = Quaternion.LookRotation(leafDir, dir) * Quaternion.Euler(60f, 0f, 0f); // Tilt up slightly
                
                leaf.transform.localScale = Vector3.one * scale * cotyledonStartSize;
                
                MeshRenderer mr = leaf.GetComponent<MeshRenderer>();
                if (mr && material != null) mr.sharedMaterial = material;

                // Mesh counting skip for brevity or add if needed
            }
        }

        private void GenerateDecussateLeaves(GameObject prefab, Material material, float startHeightFrac, float interval, ref int vertexCount, ref int triangleCount, ref int edgeCount)
        {
            if (prefab == null || trunk == null) return;

            Transform container = trunkObject.transform.Find("TrueLeaves");
            if (container != null) DestroyObject(container.gameObject);

            GameObject leavesParent = new GameObject("TrueLeaves");
            leavesParent.transform.SetParent(trunkObject.transform);

            // Decussate: Pairs at intervals, rotated 90 deg
            float currentFrac = startHeightFrac;
            int pairIndex = 0;
            
            // Truncate at top (leave room for tip)
            float maxFrac = 0.95f;

            while(currentFrac < maxFrac)
            {
                Vector3 pos = GetTrunkPositionAt(currentFrac);
                Vector3 dir = GetTrunkDirectionAt(currentFrac);
                
                // Reference vector for rotation
                Vector3 refRight = Vector3.Cross(dir, Vector3.forward).normalized; 
                if (refRight.sqrMagnitude < 0.01f) refRight = Vector3.right;

                float pairRotation = pairIndex * 90f;
                
                for(int i=0; i<2; i++)
                {
                    GameObject leaf = Instantiate(prefab);
                    leaf.transform.SetParent(leavesParent.transform);
                    leaf.transform.position = pos;
                    
                    float angle = (i * 180f) + pairRotation;
                    Vector3 leafDir = Quaternion.AngleAxis(angle, dir) * refRight;
                    
                    leaf.transform.rotation = Quaternion.LookRotation(leafDir, dir) * Quaternion.Euler(45f, 0f, 0f); // 45 deg angle
                    leaf.transform.localScale = trueLeavesSizeV3 * trueLeavesSize; 
                    
                    MeshRenderer mr = leaf.GetComponent<MeshRenderer>();
                    if (mr && material != null) mr.sharedMaterial = material;
                }
                
                currentFrac += interval;
                pairIndex++;
            }
        }

        private Vector3 GetTrunkPositionAt(float t)
        {
            if (trunk == null || trunk.trunkBendPositions.Count == 0) return trunkObject.transform.position;
            int count = trunk.trunkBendPositions.Count;
            // t is 0..1
            // Mapping to list index
            float floatIndex = t * (count - 1);
            int idx = Mathf.FloorToInt(floatIndex);
            float frac = floatIndex - idx;
            
            idx = Mathf.Clamp(idx, 0, count - 1);
            int next = Mathf.Clamp(idx + 1, 0, count - 1);
            
            Vector3 p1 = trunk.trunkBendPositions[idx];
            Vector3 p2 = trunk.trunkBendPositions[next];
            
            // These points are usually local offsets if trunkObject moves, but let's check CreateTrunkMesh.
            // In CreateTrunkMesh: trunkBendPositions.Add(bendPosition); where bendPosition usually is Height relative?
            // Actually they seem to be local positions relative to trunkObject.
            
            return trunkObject.transform.position + Vector3.Lerp(p1, p2, frac);
        }

        private Vector3 GetTrunkDirectionAt(float t)
        {
            if (trunk == null || trunk.trunkBendPositions.Count < 2) return Vector3.up;
            int count = trunk.trunkBendPositions.Count;
            float floatIndex = t * (count - 1);
            int idx = Mathf.FloorToInt(floatIndex);
            idx = Mathf.Clamp(idx, 0, count - 2);
            return (trunk.trunkBendPositions[idx + 1] - trunk.trunkBendPositions[idx]).normalized;
        }

        // --- Stage 2: Hybrid L-System Logic ---

        private void GenerateHybridBranches(float heightScale, ref int vertexCount, ref int triangleCount, ref int edgeCount)
        {
            if (trunkObject == null) return;
            
            // 1. Derive String
            string derived = DeriveLString(lsystemAxiom, lSystemIterations);
            
            // 2. Interpret
            float stepLen = lSystemStepLength * heightScale; // Scale with growth
            
            if (branchesParent) DestroyObject(branchesParent);
            branchesParent = new GameObject("HybridBranches");
            branchesParent.transform.SetParent(trunkObject.transform);
            
            branches.Clear();
            
            // "Turtle" State
            Stack<TurtleState> stack = new Stack<TurtleState>();
            TurtleState current = new TurtleState
            {
                pos = GetTrunkPositionAt(1.0f), // Start at top of trunk
                rot = Quaternion.LookRotation(GetTrunkDirectionAt(1.0f)),
                thickness = trunkRadius * 0.8f,
                growth = 0
            };
            
            // We need to match the trunk top position/rotation exactly
            // If the trunk is grown via Logistic Growth, we start L-System from the new tip.
            
            for(int i=0; i<derived.Length; i++)
            {
                char c = derived[i];
                if(c == 'F')
                {
                    // Move Forward + Draw
                    Vector3 startPos = current.pos;
                    
                    // Hybrid Direction
                    Vector3 idealDir = current.rot * Vector3.up;
                    Vector3 scDir = GetSpaceColonizationDirection(current.pos);
                    
                    Vector3 finalDir = idealDir;
                    if (useSpaceColonization && scDir.sqrMagnitude > 0.001f)
                    {
                        finalDir = Vector3.Lerp(idealDir, scDir, scDirectionBias).normalized;
                        current.rot = Quaternion.LookRotation(finalDir); // Re-orient turtle
                    }
                    
                    Vector3 endPos = startPos + finalDir * stepLen;
                    
                    // Create Branch Segment (Mesh)
                    CreateBranchSegment(startPos, endPos, current.thickness, ref vertexCount, ref triangleCount, ref edgeCount);
                    
                    // Store as Branch Data for Leaves
                    branches.Add(new Branch(startPos, finalDir, stepLen, current.thickness, current.rot, new List<Vector3>{startPos, endPos}, 0));
                    
                    current.pos = endPos;
                }
                else if(c == 'X') { /* Content */ }
                else if(c == '+') { current.rot *= Quaternion.Euler(lSystemAngle + Random.Range(-lSystemAngleRandomness, lSystemAngleRandomness), 0, 0); } // Pitch +
                else if(c == '-') { current.rot *= Quaternion.Euler(-lSystemAngle + Random.Range(-lSystemAngleRandomness, lSystemAngleRandomness), 0, 0); } // Pitch -
                else if(c == '&') { current.rot *= Quaternion.Euler(0, lSystemAngle + Random.Range(-lSystemAngleRandomness, lSystemAngleRandomness), 0); } // Yaw +
                else if(c == '^') { current.rot *= Quaternion.Euler(0, -lSystemAngle + Random.Range(-lSystemAngleRandomness, lSystemAngleRandomness), 0); } // Yaw -
                else if(c == '\\') { current.rot *= Quaternion.Euler(0, 0, lSystemAngle + Random.Range(-lSystemAngleRandomness, lSystemAngleRandomness)); } // Roll +
                else if(c == '/') { current.rot *= Quaternion.Euler(0, 0, -lSystemAngle + Random.Range(-lSystemAngleRandomness, lSystemAngleRandomness)); } // Roll -
                else if(c == '[')
                {
                    stack.Push(current);
                    current.thickness *= 0.7f; // Thinning
                }
                else if(c == ']')
                {
                    if(stack.Count > 0) current = stack.Pop();
                }
            }
        }
        
        private string DeriveLString(string axiom, int n)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(axiom);
            for(int k=0; k<n; k++)
            {
                string s = sb.ToString();
                sb.Clear();
                foreach(char c in s)
                {
                    if (c == 'X') sb.Append(lsystemRuleX);
                    else if (c == 'F') sb.Append(lsystemRuleF);
                    else sb.Append(c);
                }
            }
            return sb.ToString();
        }
        
        private Vector3 GetSpaceColonizationDirection(Vector3 pos)
        {
            if (!scInitialized || scNodes.Count == 0) return Vector3.zero;
            // Simplified query: Find attractors nearby?
            // Or just return 0 if no SC data yet. 
            // In hybrid mode, we might need an "Attraction Cloud" already populated.
            // For now, return random upward bias if SC is not fully running, or zero.
            return Vector3.zero; 
        }

        private void CreateBranchSegment(Vector3 p1, Vector3 p2, float radius, ref int vCount, ref int tCount, ref int eCount)
        {
             GameObject segment = new GameObject("HybBranchSeg");
             segment.transform.SetParent(branchesParent.transform);
             segment.transform.position = p1;
             
             Vector3 dir = (p2 - p1);
             float len = dir.magnitude;
             if (len < 0.001f) return;
             
             segment.transform.up = dir.normalized;
             segment.transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(90, 0, 0); // Align Up to Dir? Check rotation logic

             MeshFilter mf = segment.AddComponent<MeshFilter>();
             MeshRenderer mr = segment.AddComponent<MeshRenderer>();
             mr.sharedMaterial = GetTrunkMaterial();
             
             // Use CreateBranchMesh for better visuals (tapering, bending)
             // We treat this segment as a mini-branch with 1 segment?
             // Or reuse the CreateBranchMesh which generates a full branch with bending.
             // Since this is just one L-System segment, we might want minimal bending here, 
             // but CreateBranchMesh expects to generate the WHOLE branch curve.
             
             // Simplification: We use CreateBranchMesh but with p1->p2 as the "Direction".
             // We pass bendPositions empty first.
             List<Vector3> bendPositions = new List<Vector3>();
             
             // CreateBranchMesh generates geometry based on length and direction.
             // It will calculate bend positions internally.
             // But we want it to go exactly from p1 to p2?
             // CreateBranchMesh generates randomly based on length. It might NOT end at p2.
             
             // Fallback: CreateCylinderMesh but add Tapering.
             Mesh mesh = CreateTaperedCylinderMesh(len, radius, radius * 0.7f); 
             mf.mesh = mesh;
             
             vCount += mesh.vertexCount;
             tCount += mesh.triangles.Length/3;
        }

        private Mesh CreateTaperedCylinderMesh(float length, float bottomRadius, float topRadius)
        {
             Mesh mesh = new Mesh();
             int radialSegments = 6;
             int verticesCount = (radialSegments + 1) * 2 + 2; // +2 for caps center if needed, or just open?
             Vector3[] vertices = new Vector3[verticesCount];
             int[] triangles = new int[radialSegments * 6];
             Vector2[] uvs = new Vector2[verticesCount];
             
             for(int x=0; x<=radialSegments; x++)
             {
                 float u = (float)x / radialSegments;
                 float angle = u * Mathf.PI * 2;
                 float ca = Mathf.Cos(angle);
                 float sa = Mathf.Sin(angle);
                 
                 // Bottom ring
                 vertices[x] = new Vector3(ca * bottomRadius, 0, sa * bottomRadius);
                 uvs[x] = new Vector2(u, 0);
                 
                 // Top ring
                 vertices[x + radialSegments + 1] = new Vector3(ca * topRadius, length, sa * topRadius);
                 uvs[x + radialSegments + 1] = new Vector2(u, 1);
             }
             
             int tri = 0;
             for(int x=0; x<radialSegments; x++)
             {
                 int current = x;
                 int next = x + 1;
                 int top = x + radialSegments + 1;
                 int topNext = next + radialSegments + 1;
                 
                 triangles[tri++] = current;
                 triangles[tri++] = top;
                 triangles[tri++] = next;
                 
                 triangles[tri++] = next;
                 triangles[tri++] = top;
                 triangles[tri++] = topNext;
             }
             
             mesh.vertices = vertices;
             mesh.triangles = triangles;
             mesh.uv = uvs;
             mesh.RecalculateNormals();
             
             return mesh; 
        }

        private Mesh CreateCylinderMesh(float length, float radius)
        {
             Mesh mesh = new Mesh();
             int radialSegments = 6;
             int verticesCount = (radialSegments + 1) * 2;
             Vector3[] vertices = new Vector3[verticesCount];
             int[] triangles = new int[radialSegments * 6];
             Vector2[] uvs = new Vector2[verticesCount];
             
             for(int x=0; x<=radialSegments; x++)
             {
                 float u = (float)x / radialSegments;
                 float angle = u * Mathf.PI * 2;
                 float ca = Mathf.Cos(angle);
                 float sa = Mathf.Sin(angle);
                 
                 // Bottom ring
                 vertices[x] = new Vector3(ca * radius, 0, sa * radius);
                 uvs[x] = new Vector2(u, 0);
                 
                 // Top ring
                 vertices[x + radialSegments + 1] = new Vector3(ca * radius, length, sa * radius);
                 uvs[x + radialSegments + 1] = new Vector2(u, 1);
             }
             
             int tri = 0;
             for(int x=0; x<radialSegments; x++)
             {
                 int current = x;
                 int next = x + 1;
                 int top = x + radialSegments + 1;
                 int topNext = next + radialSegments + 1;
                 
                 triangles[tri++] = current;
                 triangles[tri++] = top;
                 triangles[tri++] = next;
                 
                 triangles[tri++] = next;
                 triangles[tri++] = top;
                 triangles[tri++] = topNext;
             }
             
             mesh.vertices = vertices;
             mesh.triangles = triangles;
             mesh.uv = uvs;
             mesh.RecalculateNormals();
             
             return mesh; 
        }

        private struct TurtleState
        {
            public Vector3 pos;
            public Quaternion rot;
            public float thickness;
            public float growth;
        }

        private class Trunk
        {
            public List<Vector3> trunkBendPositions;

            public Trunk(List<Vector3> trunkBendPositions)
            {
                this.trunkBendPositions = trunkBendPositions;
            }
        }

        private class Branch
        {
            public Vector3 position;
            public Vector3 direction;
            public float length;
            public float adjustedBranchRadius;
            public Quaternion randomRotation;
            public List<Vector3> bendPositions;
            public int persistentIndex;

            public Branch(Vector3 position, Vector3 direction, float length, float adjustedBranchRadius, Quaternion randomRotation, List<Vector3> bendPositions, int persistentIndex)
            {
                this.position = position;
                this.direction = direction;
                this.length = length;
                this.randomRotation = randomRotation;
                this.bendPositions = bendPositions;
                this.adjustedBranchRadius = adjustedBranchRadius;
                this.persistentIndex = persistentIndex;
            }
        }

        public class BranchletsX
        {
            public Vector3 branchletPosition { get; set; }
            public Vector3 direction { get; set; }
            public float length { get; set; }
            public float branchletMaxRadius { get; set; }
            public float sideFactor { get; set; }
            public Quaternion fixedRotation;
            public List<Vector3> bendPositions { get; set; }
            public int childSeed { get; set; }

            public BranchletsX(Vector3 position, Vector3 dir, float len, float branchletMaxRadius, float sideFactor, Quaternion fixedRotation, List<Vector3> bendBranchletPositions, int childSeed)
            {
                this.branchletPosition = position;
                this.direction = dir;
                this.length = len;
                this.branchletMaxRadius = branchletMaxRadius;
                this.sideFactor = sideFactor;
                this.fixedRotation = fixedRotation;
                this.bendPositions = bendBranchletPositions;
                this.childSeed = childSeed;
            }
        }
    }
}
