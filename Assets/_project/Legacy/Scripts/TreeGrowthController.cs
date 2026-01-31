using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    public class TreeGrowthController : MonoBehaviour
    {
        public enum RenderMode
        {
            MeshAndLines,
            LinesOnly,
            MeshOnly
        }

        [Header("Preset")]
        public bool usePreset = true;
        public TreeGrowthPreset preset;
        public GrowthGeneralSettings generalOverrides = new GrowthGeneralSettings();
        public List<GrowthStageSettings> stageOverrides = new List<GrowthStageSettings>();

        [Header("Reuse (Optional)")]
        public RuntimeTreeGenerator runtimeSource;

        [Header("Rendering")]
        public RenderMode renderMode = RenderMode.MeshAndLines;
        public Material barkMaterial;
        public Material leafMaterial;
        public GameObject leafPrefab;
        public Material cotyledonMaterial;
        public GameObject cotyledonPrefab;
        public Material lineMaterial;

        [Header("Growth")]
        public bool autoInitialize = true;
        public float growthSpeed = 1f;

        private GrowthGeneralSettings general;
        private List<GrowthStageSettings> stages;

        private class Node
        {
            public int id;
            public int parent;
            public Vector3 pos;
            public Vector3 dir;
            public float radius;
            public float targetRadius;
            public bool isTerminal;
            public List<int> children = new List<int>();
        }

        private class Leaf
        {
            public int nodeId;
            public bool isCotyledon;
            public float size;
            public float lightScore;
            public float dropTimer;
            public bool dropping;
            public float cycleOffset;
        }

        private struct LeafMeshes
        {
            public Mesh leaves;
            public Mesh cotyledons;
        }

        private class Bud
        {
            public int nodeId;
            public Vector3 dir;
            public float progress;
            public float age;
            public float totalLength;
            public float branchAccumulator;
            public bool isLeader;
            public float activationDelay;
            public int pairIndex;
        }

        private readonly List<Node> nodes = new List<Node>();
        private readonly List<Bud> buds = new List<Bud>();
        private readonly List<Leaf> leaves = new List<Leaf>();

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshFilter lineMeshFilter;
        private MeshRenderer lineMeshRenderer;

        private System.Random rng;
        private float age;
        private int stageIndex;
        private float stageTime;
        private bool initialized;

        private void OnEnable()
        {
            TreeGrowthManager.Register(this);
        }

        private void OnDisable()
        {
            TreeGrowthManager.Unregister(this);
        }

        private void Start()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            SetupSettings();
            SetupRenderers();
            SeedRandom();

            nodes.Clear();
            buds.Clear();
            leaves.Clear();

            age = 0f;
            stageIndex = 0;
            stageTime = 0f;

            float growthScale = GetSecondaryGrowthScale();
            var root = new Node
            {
                id = 0,
                parent = -1,
                pos = Vector3.zero,
                dir = Vector3.up,
                radius = general.baseRadius * growthScale,
                targetRadius = general.baseRadius,
                isTerminal = true
            };
            nodes.Add(root);
            buds.Add(new Bud
            {
                nodeId = 0,
                dir = Vector3.up,
                progress = 0f,
                age = 0f,
                totalLength = 0f,
                branchAccumulator = 0f,
                isLeader = true,
                activationDelay = 0f,
                pairIndex = 0
            });

            var stage = GetStage();
            if (stage != null && stage.allowCotyledons)
            {
                float cotySize = general != null && general.cotyledonSize > 0f ? general.cotyledonSize : stage.leafSize;
                AddCotyledons(cotySize);
            }

            initialized = true;
            RebuildRender();
        }

        public void OnGrowthTick(float dt)
        {
            if (!initialized)
            {
                if (autoInitialize)
                {
                    Initialize();
                }
                else
                {
                    return;
                }
            }

            if (dt <= 0f) return;
            dt *= Mathf.Max(0f, growthSpeed);

            UpdateStage(dt);
            Grow(dt);
            UpdateLeaves(dt);
            SmoothRadii(dt);

            RebuildRender();
        }

        private void SetupSettings()
        {
            if (usePreset && preset != null)
            {
                general = preset.general;
                stages = preset.stages;
            }
            else
            {
                general = generalOverrides;
                stages = stageOverrides;
            }

            if (stages == null || stages.Count == 0)
            {
                stages = new List<GrowthStageSettings>
                {
                    new GrowthStageSettings
                    {
                        name = "Seedling",
                        duration = 10f,
                        stepLength = 0.2f,
                        elongationSpeed = 0.2f,
                        branchStartLength = 0.4f,
                        branchPairInterval = 0.6f,
                        pairedBranching = true,
                        pairRotationOffset = 0f,
                        pairRotationStep = 90f,
                        sympodialTakeoverLength = 0.5f,
                        lateralActivationDelay = 0.2f,
                        branchProbability = 0.05f,
                        apicalDominance = 0.9f,
                        mainAxisRadiusScale = 0.995f,
                        branchRadiusScale = 0.7f,
                        branchLengthScale = 0.6f,
                        lateralBias = 0.1f,
                        crownDepth = 0.6f,
                        innerLeafDensity = 0.4f,
                        allowCotyledons = true,
                        leafCountPerNode = 2,
                        leafCycle = false,
                        leafCyclePeriod = 2.2f,
                        leafVisibleFraction = 1f,
                        removeCotyledonsOnEnter = false
                    },
                    new GrowthStageSettings
                    {
                        name = "Sapling",
                        duration = 20f,
                        stepLength = 0.25f,
                        elongationSpeed = 0.25f,
                        branchStartLength = 0.5f,
                        branchPairInterval = 0.55f,
                        pairedBranching = true,
                        pairRotationOffset = 0f,
                        pairRotationStep = 90f,
                        sympodialTakeoverLength = 0.7f,
                        lateralActivationDelay = 0.25f,
                        branchProbability = 0.2f,
                        apicalDominance = 0.7f,
                        mainAxisRadiusScale = 0.99f,
                        branchRadiusScale = 0.8f,
                        branchLengthScale = 0.85f,
                        lateralBias = 0.2f,
                        crownDepth = 1.2f,
                        innerLeafDensity = 0.5f,
                        removeCotyledonsOnEnter = true,
                        leafCountPerNode = 3,
                        leafCycle = false,
                        leafCyclePeriod = 2.6f,
                        leafVisibleFraction = 1f,
                        pruneInnerLeavesOnEnter = false
                    },
                    new GrowthStageSettings
                    {
                        name = "Juvenile",
                        duration = 40f,
                        stepLength = 0.3f,
                        elongationSpeed = 0.3f,
                        branchStartLength = 0.5f,
                        branchPairInterval = 0.5f,
                        pairedBranching = true,
                        pairRotationOffset = 0f,
                        pairRotationStep = 90f,
                        sympodialTakeoverLength = 0.8f,
                        lateralActivationDelay = 0.25f,
                        branchProbability = 0.35f,
                        apicalDominance = 0.5f,
                        mainAxisRadiusScale = 0.985f,
                        branchRadiusScale = 0.8f,
                        branchLengthScale = 1.0f,
                        lateralBias = 0.25f,
                        crownDepth = 2.2f,
                        innerLeafDensity = 0.6f,
                        leafCountPerNode = 4,
                        leafCycle = false,
                        leafCyclePeriod = 3.0f,
                        leafVisibleFraction = 1f,
                        pruneInnerLeavesOnEnter = true
                    },
                    new GrowthStageSettings
                    {
                        name = "Mature",
                        duration = 999f,
                        stepLength = 0.2f,
                        elongationSpeed = 0.2f,
                        branchStartLength = 0.6f,
                        branchPairInterval = 0.6f,
                        pairedBranching = true,
                        pairRotationOffset = 0f,
                        pairRotationStep = 90f,
                        sympodialTakeoverLength = 0.9f,
                        lateralActivationDelay = 0.3f,
                        branchProbability = 0.15f,
                        apicalDominance = 0.4f,
                        mainAxisRadiusScale = 0.98f,
                        branchRadiusScale = 0.75f,
                        branchLengthScale = 0.7f,
                        lateralBias = 0.25f,
                        crownDepth = 2.5f,
                        innerLeafDensity = 0.5f,
                        leafCountPerNode = 4,
                        leafCycle = false,
                        leafCyclePeriod = 3.2f,
                        leafVisibleFraction = 1f,
                        pruneInnerLeavesOnEnter = true
                    }
                };
            }

            if (runtimeSource != null)
            {
                if (barkMaterial == null) barkMaterial = runtimeSource.trunkMaterial;
                if (leafMaterial == null) leafMaterial = runtimeSource.leafMaterial;
                if (leafPrefab == null) leafPrefab = runtimeSource.leafPrefab;
            }
        }

        private void SetupRenderers()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            var lineRoot = transform.Find("TreeLines");
            if (lineRoot == null)
            {
                var go = new GameObject("TreeLines");
                go.transform.SetParent(transform, false);
                lineRoot = go.transform;
            }
            lineMeshFilter = lineRoot.GetComponent<MeshFilter>();
            if (lineMeshFilter == null) lineMeshFilter = lineRoot.gameObject.AddComponent<MeshFilter>();
            lineMeshRenderer = lineRoot.GetComponent<MeshRenderer>();
            if (lineMeshRenderer == null) lineMeshRenderer = lineRoot.gameObject.AddComponent<MeshRenderer>();

            if (lineMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null) s = Shader.Find("Unlit/Color");
                if (s != null)
                {
                    lineMaterial = new Material(s);
                    lineMaterial.color = Color.green;
                }
            }

            ApplyRenderMode();
        }

        private void ApplyRenderMode()
        {
            bool meshOn = renderMode == RenderMode.MeshAndLines || renderMode == RenderMode.MeshOnly;
            bool linesOn = renderMode == RenderMode.MeshAndLines || renderMode == RenderMode.LinesOnly;

            if (meshRenderer != null) meshRenderer.enabled = meshOn;
            if (lineMeshRenderer != null) lineMeshRenderer.enabled = linesOn;
        }

        private void SeedRandom()
        {
            int seed = (general != null) ? general.seed : 0;
            if (seed == 0) seed = Random.Range(1, int.MaxValue);
            rng = new System.Random(seed);
        }

        private GrowthStageSettings GetStage()
        {
            if (stages == null || stages.Count == 0) return null;
            stageIndex = Mathf.Clamp(stageIndex, 0, stages.Count - 1);
            return stages[stageIndex];
        }

        private void UpdateStage(float dt)
        {
            var stage = GetStage();
            if (stage == null) return;

            age += dt;
            stageTime += dt;

            while (stageIndex < stages.Count - 1 && stageTime >= stage.duration)
            {
                stageTime -= stage.duration;
                stageIndex++;
                OnStageEnter(stages[stageIndex]);
            }
        }

        private void OnStageEnter(GrowthStageSettings stage)
        {
            if (stage.removeCotyledonsOnEnter)
            {
                for (int i = 0; i < leaves.Count; i++)
                {
                    if (leaves[i].isCotyledon)
                    {
                        leaves[i].dropping = true;
                    }
                }
            }

            if (stage.pruneInnerLeavesOnEnter)
            {
                for (int i = 0; i < leaves.Count; i++)
                {
                    if (!IsTerminalNode(leaves[i].nodeId))
                    {
                        leaves[i].dropping = true;
                    }
                }
            }
        }

        private void Grow(float dt)
        {
            var stage = GetStage();
            if (stage == null) return;
            if (nodes.Count >= general.maxNodes) return;

            float baseStep = Mathf.Max(0.01f, stage.stepLength > 0 ? stage.stepLength : general.baseStepLength);
            float radiusFalloff = stage.radiusFalloffOverride > 0f ? stage.radiusFalloffOverride : general.radiusFalloff;
            float mainAxisScale = Mathf.Max(0.01f, stage.mainAxisRadiusScale);
            float branchScale = Mathf.Max(0.01f, stage.branchRadiusScale);
            float branchLengthScale = Mathf.Max(0.1f, stage.branchLengthScale);
            float speedBase = Mathf.Max(0.001f, stage.elongationSpeed > 0f ? stage.elongationSpeed : baseStep);

            List<Bud> spawned = null;

            for (int i = 0; i < buds.Count; i++)
            {
                Bud bud = buds[i];
                if (bud == null) continue;
                if (bud.nodeId < 0 || bud.nodeId >= nodes.Count) continue;

                bud.age += dt;
                if (bud.activationDelay > 0f)
                {
                    bud.activationDelay -= dt;
                    continue;
                }

                float stepLength = bud.isLeader ? baseStep : baseStep * branchLengthScale;
                float speed = bud.isLeader ? speedBase : speedBase * branchLengthScale;
                float remaining = speed * dt;

                while (remaining > 0f && nodes.Count < general.maxNodes)
                {
                    float toGrow = Mathf.Min(remaining, stepLength - bud.progress);
                    bud.progress += toGrow;
                    bud.totalLength += toGrow;
                    bud.branchAccumulator += toGrow;
                    remaining -= toGrow;

                    if (bud.progress + 0.0001f >= stepLength)
                    {
                        int newId = TryAddChild(nodes[bud.nodeId], bud.dir, stepLength,
                            bud.isLeader ? mainAxisScale : branchScale, radiusFalloff);
                        if (newId < 0)
                        {
                            remaining = 0f;
                            break;
                        }

                        bud.nodeId = newId;
                        bud.progress = 0f;
                        bud.dir = ApplyApicalBias(bud.dir, stage);

                        if (!bud.isLeader && stage.sympodialTakeoverLength > 0f && bud.totalLength >= stage.sympodialTakeoverLength)
                        {
                            bud.isLeader = true;
                            bud.pairIndex = 0;
                        }

                        bool canBranch = bud.isLeader
                            && stage.branchProbability > 0f
                            && stage.branchPairInterval > 0f
                            && bud.totalLength >= Mathf.Max(0f, stage.branchStartLength);

                        while (canBranch && bud.branchAccumulator >= stage.branchPairInterval && nodes.Count < general.maxNodes)
                        {
                            bud.branchAccumulator -= stage.branchPairInterval;
                            if (NextFloat() <= stage.branchProbability)
                            {
                                if (spawned == null) spawned = new List<Bud>();

                                if (stage.pairedBranching)
                                {
                                    SpawnBranchPair(bud, stage, spawned);
                                }
                                else
                                {
                                    SpawnSingleBranch(bud, stage, spawned);
                                }

                                bud.pairIndex++;
                            }
                        }
                    }
                }
            }

            if (spawned != null && spawned.Count > 0)
            {
                buds.AddRange(spawned);
            }

            RecalculateRadii();
        }

        private int TryAddChild(Node parent, Vector3 dir, float step, float radiusScale, float radiusFalloff)
        {
            if (nodes.Count >= general.maxNodes) return -1;

            Vector3 normalizedDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
            int id = nodes.Count;

            float falloff = Mathf.Max(0.0001f, radiusFalloff);
            float parentRadius = parent.targetRadius > 0f ? parent.targetRadius : parent.radius;
            float childRadius = parentRadius * Mathf.Max(0.01f, radiusScale) * falloff;
            childRadius = Mathf.Max(general.minRadius, childRadius);
            float growthScale = GetSecondaryGrowthScale();

            var child = new Node
            {
                id = id,
                parent = parent.id,
                pos = parent.pos + normalizedDir * step,
                dir = normalizedDir,
                radius = childRadius * growthScale,
                targetRadius = childRadius,
                isTerminal = true
            };

            parent.children.Add(id);
            parent.isTerminal = false;

            nodes.Add(child);
            return id;
        }

        private void RecalculateRadii()
        {
            float exponent = Mathf.Max(0.1f, general.pipeExponent);
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                Node node = nodes[i];
                if (node.children.Count == 0)
                {
                    float baseRadius = node.targetRadius > 0f ? node.targetRadius : node.radius;
                    node.targetRadius = Mathf.Max(general.minRadius, baseRadius);
                    continue;
                }

                float sum = 0f;
                for (int c = 0; c < node.children.Count; c++)
                {
                    Node child = nodes[node.children[c]];
                    float r = child.targetRadius > 0f ? child.targetRadius : child.radius;
                    sum += Mathf.Pow(r, exponent);
                }
                float target = Mathf.Pow(sum, 1f / exponent);
                node.targetRadius = Mathf.Max(general.minRadius, target);
            }
        }

        private void SmoothRadii(float dt)
        {
            if (general == null || nodes.Count == 0) return;
            float speed = Mathf.Max(0f, general.radiusSmoothing);
            float scale = GetSecondaryGrowthScale();
            bool preventShrink = general.preventRadiusShrink;
            if (speed <= 0f)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    float target = nodes[i].targetRadius * scale;
                    if (preventShrink && target < nodes[i].radius)
                    {
                        target = nodes[i].radius;
                    }
                    nodes[i].radius = target;
                }
                return;
            }

            float t = 1f - Mathf.Exp(-speed * Mathf.Max(0f, dt));
            for (int i = 0; i < nodes.Count; i++)
            {
                float target = nodes[i].targetRadius * scale;
                if (preventShrink && target < nodes[i].radius)
                {
                    target = nodes[i].radius;
                }
                nodes[i].radius = Mathf.Lerp(nodes[i].radius, target, t);
            }
        }

        private float GetSecondaryGrowthScale()
        {
            if (general == null) return 1f;
            float minScale = Mathf.Clamp01(general.secondaryGrowthMinScale);
            float duration = Mathf.Max(0f, general.secondaryGrowthDuration);
            float startAge = Mathf.Max(0f, general.secondaryGrowthStartAge);
            if (duration <= 0f) return 1f;
            float t = Mathf.Clamp01((age - startAge) / duration);
            return Mathf.Lerp(minScale, 1f, t);
        }

        private void UpdateLeaves(float dt)
        {
            var stage = GetStage();
            if (stage == null) return;
            float maxY = GetMaxHeight();
            bool allowTrueLeaves = general == null || general.trueLeafStartAge <= 0f || age >= general.trueLeafStartAge;
            bool applyCotyledonSenescence = general != null && general.cotyledonSenescenceAge > 0f;
            bool allowInnerLeaves = Mathf.Clamp01(stage.innerLeafDensity) > 0f;

            for (int i = leaves.Count - 1; i >= 0; i--)
            {
                Leaf leaf = leaves[i];
                if (leaf.isCotyledon && applyCotyledonSenescence && age >= general.cotyledonSenescenceAge)
                {
                    leaf.dropping = true;
                }
                if (!leaf.isCotyledon && !IsTerminalNode(leaf.nodeId) && !allowInnerLeaves)
                {
                    leaf.dropping = true;
                }

                if (!leaf.dropping)
                {
                    leaf.lightScore = ComputeLightScore(nodes[leaf.nodeId]);
                    if (leaf.lightScore < stage.pruningLightThreshold)
                    {
                        leaf.dropping = true;
                    }
                    else if (stage.crownDepth > 0f)
                    {
                        float depth = maxY - nodes[leaf.nodeId].pos.y;
                        if (depth > stage.crownDepth)
                        {
                            leaf.dropping = true;
                        }
                    }
                }

                if (leaf.dropping)
                {
                    leaf.dropTimer += dt;
                    if (leaf.dropTimer >= general.leafDropDuration)
                    {
                        leaves.RemoveAt(i);
                        continue;
                    }
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!allowTrueLeaves) continue;
                bool inCrown = stage.crownDepth <= 0f || (maxY - nodes[i].pos.y) <= stage.crownDepth;
                if (!inCrown) continue;

                bool canAttach = nodes[i].isTerminal;
                float density = stage.leafDensity;
                if (!nodes[i].isTerminal)
                {
                    density *= Mathf.Clamp01(stage.innerLeafDensity);
                    canAttach = density > 0f;
                }

                int targetCount = Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, stage.leafCountPerNode) * Mathf.Clamp01(density)));
                if (canAttach && targetCount > 0)
                {
                    int existing = CountLeavesAtNode(i);
                    int toAdd = targetCount - existing;
                    for (int add = 0; add < toAdd; add++)
                    {
                        leaves.Add(new Leaf
                        {
                            nodeId = i,
                            isCotyledon = false,
                            size = stage.leafSize,
                            lightScore = 1f,
                            dropTimer = 0f,
                            dropping = false,
                            cycleOffset = stage.leafCyclePeriod * ((existing + add) / Mathf.Max(1f, targetCount))
                        });
                    }
                }
            }
        }

        private float ComputeLightScore(Node node)
        {
            float maxY = GetMaxHeight();
            float depth = Mathf.Max(0f, maxY - node.pos.y);
            return Mathf.Exp(-general.lightFalloff * depth);
        }

        private float GetMaxHeight()
        {
            float maxY = nodes.Count > 0 ? nodes[0].pos.y : 0f;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].pos.y > maxY) maxY = nodes[i].pos.y;
            }
            return maxY;
        }

        private bool HasLeafAtNode(int nodeId)
        {
            for (int i = 0; i < leaves.Count; i++)
            {
                if (leaves[i].nodeId == nodeId && !leaves[i].dropping)
                {
                    return true;
                }
            }
            return false;
        }

        private int CountLeavesAtNode(int nodeId)
        {
            int count = 0;
            for (int i = 0; i < leaves.Count; i++)
            {
                if (leaves[i].nodeId == nodeId && !leaves[i].dropping)
                {
                    count++;
                }
            }
            return count;
        }

        private bool IsTerminalNode(int nodeId)
        {
            if (nodeId < 0 || nodeId >= nodes.Count) return false;
            return nodes[nodeId].isTerminal;
        }

        private void AddCotyledons(float size)
        {
            leaves.Add(new Leaf { nodeId = 0, isCotyledon = true, size = size, lightScore = 1f, dropTimer = 0f, dropping = false });
            leaves.Add(new Leaf { nodeId = 0, isCotyledon = true, size = size, lightScore = 1f, dropTimer = 0f, dropping = false });
        }

        private Vector3 ApplyApicalBias(Vector3 dir, GrowthStageSettings stage)
        {
            Vector3 normalized = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
            float bias = Mathf.Clamp01(1f - stage.apicalDominance) * 0.15f;
            if (bias <= 0f) return normalized;
            return Vector3.Slerp(normalized, Vector3.up, bias).normalized;
        }

        private void SpawnBranchPair(Bud bud, GrowthStageSettings stage, List<Bud> spawned)
        {
            Vector3 axis = bud.dir.sqrMagnitude > 0.0001f ? bud.dir.normalized : Vector3.up;
            Vector3 perp = PickPerpendicular(axis);
            float yaw = stage.pairRotationOffset + (bud.pairIndex * stage.pairRotationStep);
            Vector3 lateralAxis = Quaternion.AngleAxis(yaw, axis) * perp;
            float angle = PickBranchAngle(stage);

            Vector3 dirA = Quaternion.AngleAxis(angle, lateralAxis) * axis;
            Vector3 dirB = Quaternion.AngleAxis(-angle, lateralAxis) * axis;

            dirA = ApplyBranchForces(dirA, stage);
            dirB = ApplyBranchForces(dirB, stage);

            spawned.Add(CreateBranchBud(bud.nodeId, dirA, stage.lateralActivationDelay));
            spawned.Add(CreateBranchBud(bud.nodeId, dirB, stage.lateralActivationDelay));
        }

        private void SpawnSingleBranch(Bud bud, GrowthStageSettings stage, List<Bud> spawned)
        {
            Vector3 baseDir = bud.dir.sqrMagnitude > 0.0001f ? bud.dir.normalized : Vector3.up;
            Vector3 branchDir = RandomBranchDirection(baseDir, stage.branchAngleRange);
            branchDir = ApplyBranchForces(branchDir, stage);
            spawned.Add(CreateBranchBud(bud.nodeId, branchDir, stage.lateralActivationDelay));
        }

        private Bud CreateBranchBud(int nodeId, Vector3 dir, float activationDelay)
        {
            return new Bud
            {
                nodeId = nodeId,
                dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up,
                progress = 0f,
                age = 0f,
                totalLength = 0f,
                branchAccumulator = 0f,
                isLeader = false,
                activationDelay = Mathf.Max(0f, activationDelay),
                pairIndex = 0
            };
        }

        private float PickBranchAngle(GrowthStageSettings stage)
        {
            float min = Mathf.Max(0f, stage.branchAngleRange.x);
            float max = Mathf.Max(min, stage.branchAngleRange.y);
            return Mathf.Lerp(min, max, NextFloat());
        }

        private Vector3 ApplyBranchForces(Vector3 dir, GrowthStageSettings stage)
        {
            Vector3 result = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;

            if (stage.lateralBias > 0f)
            {
                Vector3 lateral = Vector3.ProjectOnPlane(RandomUnitVector(), Vector3.up);
                if (lateral.sqrMagnitude > 0.0001f)
                {
                    result = Vector3.Slerp(result, lateral.normalized, Mathf.Clamp01(stage.lateralBias)).normalized;
                }
            }

            if (stage.branchGravity > 0f)
            {
                Vector3 gravityDir = Vector3.down * stage.branchGravity;
                result = (result + gravityDir).normalized;
            }

            return result;
        }

        private Vector3 PickPerpendicular(Vector3 axis)
        {
            Vector3 perp = Vector3.Cross(axis, Vector3.up);
            if (perp.sqrMagnitude < 0.0001f)
            {
                perp = Vector3.Cross(axis, Vector3.right);
            }
            return perp.sqrMagnitude > 0.0001f ? perp.normalized : Vector3.forward;
        }

        private Vector3 RandomBranchDirection(Vector3 baseDir, Vector2 angleRange)
        {
            float minAngle = Mathf.Max(0f, angleRange.x);
            float maxAngle = Mathf.Max(minAngle, angleRange.y);

            Vector3 axis = Vector3.Cross(baseDir, Vector3.up);
            if (axis.sqrMagnitude < 0.0001f)
            {
                axis = Vector3.Cross(baseDir, Vector3.right);
            }
            axis.Normalize();

            float angle = Mathf.Lerp(minAngle, maxAngle, NextFloat());
            float spin = NextFloat() * 360f;
            Quaternion rot = Quaternion.AngleAxis(spin, baseDir) * Quaternion.AngleAxis(angle, axis);
            return (rot * baseDir).normalized;
        }

        private Vector3 RandomUnitVector()
        {
            float z = (NextFloat() * 2f) - 1f;
            float t = NextFloat() * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - (z * z)));
            return new Vector3(r * Mathf.Cos(t), z, r * Mathf.Sin(t));
        }

        private float NextFloat()
        {
            return (float)rng.NextDouble();
        }

        private void RebuildRender()
        {
            ApplyRenderMode();

            var segments = new List<TreeMeshBuilder.Segment>(nodes.Count);
            var positions = new List<Vector3>(nodes.Count);
            var radii = new List<float>(nodes.Count);
            var parents = new List<int>(nodes.Count);
            var stage = GetStage();
            float baseStep = (stage != null && stage.stepLength > 0f) ? stage.stepLength : general.baseStepLength;
            float radiusFalloff = (stage != null && stage.radiusFalloffOverride > 0f) ? stage.radiusFalloffOverride : general.radiusFalloff;
            float mainAxisScale = stage != null ? Mathf.Max(0.01f, stage.mainAxisRadiusScale) : 1f;
            float branchScale = stage != null ? Mathf.Max(0.01f, stage.branchRadiusScale) : 1f;
            float branchLengthScale = stage != null ? Mathf.Max(0.1f, stage.branchLengthScale) : 1f;

            for (int i = 0; i < nodes.Count; i++)
            {
                positions.Add(nodes[i].pos);
                radii.Add(Mathf.Max(0.001f, nodes[i].radius));
                parents.Add(nodes[i].parent);
            }

            for (int i = 1; i < nodes.Count; i++)
            {
                Node n = nodes[i];
                if (n.parent < 0 || n.parent >= nodes.Count) continue;
                Node p = nodes[n.parent];
                segments.Add(new TreeMeshBuilder.Segment
                {
                    start = p.pos,
                    end = n.pos,
                    startRadius = Mathf.Max(0.001f, p.radius),
                    endRadius = Mathf.Max(0.001f, n.radius)
                });
            }

            for (int i = 0; i < buds.Count; i++)
            {
                Bud bud = buds[i];
                if (bud == null) continue;
                if (bud.activationDelay > 0f) continue;
                if (bud.nodeId < 0 || bud.nodeId >= nodes.Count) continue;
                if (bud.progress <= 0.0001f) continue;

                Node parent = nodes[bud.nodeId];
                Vector3 dir = bud.dir.sqrMagnitude > 0.0001f ? bud.dir.normalized : Vector3.up;
                float stepLength = bud.isLeader ? baseStep : baseStep * branchLengthScale;
                if (stepLength <= 0.0001f) continue;

                float childRadius = parent.radius * (bud.isLeader ? mainAxisScale : branchScale) * Mathf.Max(0.0001f, radiusFalloff);
                float t = Mathf.Clamp01(bud.progress / stepLength);
                float tipRadius = Mathf.Lerp(parent.radius, childRadius, t);
                Vector3 tipPos = parent.pos + dir * bud.progress;

                positions.Add(tipPos);
                radii.Add(Mathf.Max(0.001f, tipRadius));
                parents.Add(bud.nodeId);

                segments.Add(new TreeMeshBuilder.Segment
                {
                    start = parent.pos,
                    end = tipPos,
                    startRadius = Mathf.Max(0.001f, parent.radius),
                    endRadius = Mathf.Max(0.001f, tipRadius)
                });
            }

            if (meshRenderer != null && meshRenderer.enabled)
            {
                var barkMesh = TreeMeshBuilder.BuildTubeMesh(positions, radii, parents, general.radialSegments);
                LeafMeshes leafMeshes = BuildLeafMeshes();

                bool useLeaves = leafMaterial != null && leafMeshes.leaves != null && leafMeshes.leaves.vertexCount > 0;
                Material cotyMat = cotyledonMaterial != null ? cotyledonMaterial : leafMaterial;
                bool useCotyledons = cotyMat != null && leafMeshes.cotyledons != null && leafMeshes.cotyledons.vertexCount > 0;

                var combine = new List<CombineInstance>(1 + (useLeaves ? 1 : 0) + (useCotyledons ? 1 : 0));
                combine.Add(new CombineInstance { mesh = barkMesh, transform = Matrix4x4.identity });
                if (useLeaves)
                {
                    combine.Add(new CombineInstance { mesh = leafMeshes.leaves, transform = Matrix4x4.identity });
                }
                if (useCotyledons)
                {
                    combine.Add(new CombineInstance { mesh = leafMeshes.cotyledons, transform = Matrix4x4.identity });
                }

                var finalMesh = new Mesh();
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                finalMesh.CombineMeshes(combine.ToArray(), false, false);

                meshFilter.sharedMesh = finalMesh;

                var mats = new List<Material>(1 + (useLeaves ? 1 : 0) + (useCotyledons ? 1 : 0))
                {
                    barkMaterial
                };
                if (useLeaves) mats.Add(leafMaterial);
                if (useCotyledons) mats.Add(cotyMat);
                meshRenderer.sharedMaterials = mats.ToArray();
            }

            if (lineMeshRenderer != null && lineMeshRenderer.enabled)
            {
                lineMeshRenderer.sharedMaterial = lineMaterial;
                lineMeshFilter.sharedMesh = TreeMeshBuilder.BuildLineMesh(segments);
            }
        }

        private LeafMeshes BuildLeafMeshes()
        {
            var result = new LeafMeshes
            {
                leaves = new Mesh(),
                cotyledons = new Mesh()
            };
            if (leaves.Count == 0) return result;

            Mesh leafMeshAsset = GetMeshFromPrefab(leafPrefab);

            Mesh cotyMeshAsset = GetMeshFromPrefab(cotyledonPrefab);
            if (cotyMeshAsset == null)
            {
                cotyMeshAsset = leafMeshAsset;
            }

            var stage = GetStage();
            bool cycle = stage != null && stage.leafCycle && stage.leafCyclePeriod > 0f;
            float period = stage != null ? stage.leafCyclePeriod : 0f;
            float visibleFraction = stage != null ? Mathf.Clamp01(stage.leafVisibleFraction) : 1f;

            var leafCombine = new List<CombineInstance>();
            var cotyCombine = new List<CombineInstance>();

            for (int i = 0; i < leaves.Count; i++)
            {
                Leaf leaf = leaves[i];
                if (leaf.dropping) continue;
                if (leaf.nodeId < 0 || leaf.nodeId >= nodes.Count) continue;

                if (!leaf.isCotyledon && cycle)
                {
                    float t = (age + leaf.cycleOffset) % period;
                    if (t > period * visibleFraction)
                    {
                        continue;
                    }
                }

                Mesh meshAsset = leaf.isCotyledon ? cotyMeshAsset : leafMeshAsset;
                if (meshAsset == null) continue;

                Node node = nodes[leaf.nodeId];
                Vector3 pos = node.pos + node.dir * 0.05f;
                Quaternion rot = Quaternion.LookRotation(node.dir, Vector3.up);
                Vector3 scale = Vector3.one * leaf.size;

                var matrix = Matrix4x4.TRS(pos, rot, scale);
                if (leaf.isCotyledon)
                {
                    cotyCombine.Add(new CombineInstance { mesh = meshAsset, transform = matrix });
                }
                else
                {
                    leafCombine.Add(new CombineInstance { mesh = meshAsset, transform = matrix });
                }
            }

            result.leaves.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            if (leafCombine.Count > 0)
            {
                result.leaves.CombineMeshes(leafCombine.ToArray(), true, true);
            }

            result.cotyledons.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            if (cotyCombine.Count > 0)
            {
                result.cotyledons.CombineMeshes(cotyCombine.ToArray(), true, true);
            }

            return result;
        }

        private static Mesh GetMeshFromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;

            MeshFilter mf = prefab.GetComponent<MeshFilter>();
            if (mf != null) return mf.sharedMesh;

            mf = prefab.GetComponentInChildren<MeshFilter>(true);
            if (mf != null) return mf.sharedMesh;

            var skinned = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinned != null) return skinned.sharedMesh;

            return null;
        }
    }
}
