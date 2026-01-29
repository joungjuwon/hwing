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

        private readonly List<Node> nodes = new List<Node>();
        private readonly List<int> buds = new List<int>();
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

            var root = new Node
            {
                id = 0,
                parent = -1,
                pos = Vector3.zero,
                dir = Vector3.up,
                radius = general.baseRadius,
                isTerminal = true
            };
            nodes.Add(root);
            buds.Add(0);

            age = 0f;
            stageIndex = 0;
            stageTime = 0f;

            var stage = GetStage();
            if (stage != null && stage.allowCotyledons)
            {
                AddCotyledons(stage.leafSize);
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
                        leafCycle = true,
                        leafCyclePeriod = 2.2f,
                        leafVisibleFraction = 0.55f,
                        removeCotyledonsOnEnter = false
                    },
                    new GrowthStageSettings
                    {
                        name = "Sapling",
                        duration = 20f,
                        stepLength = 0.25f,
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
                        leafCycle = true,
                        leafCyclePeriod = 2.6f,
                        leafVisibleFraction = 0.6f,
                        pruneInnerLeavesOnEnter = false
                    },
                    new GrowthStageSettings
                    {
                        name = "Juvenile",
                        duration = 40f,
                        stepLength = 0.3f,
                        branchProbability = 0.35f,
                        apicalDominance = 0.5f,
                        mainAxisRadiusScale = 0.985f,
                        branchRadiusScale = 0.8f,
                        branchLengthScale = 1.0f,
                        lateralBias = 0.25f,
                        crownDepth = 2.2f,
                        innerLeafDensity = 0.6f,
                        leafCountPerNode = 4,
                        leafCycle = true,
                        leafCyclePeriod = 3.0f,
                        leafVisibleFraction = 0.6f,
                        pruneInnerLeavesOnEnter = true
                    },
                    new GrowthStageSettings
                    {
                        name = "Mature",
                        duration = 999f,
                        stepLength = 0.2f,
                        branchProbability = 0.15f,
                        apicalDominance = 0.4f,
                        mainAxisRadiusScale = 0.98f,
                        branchRadiusScale = 0.75f,
                        branchLengthScale = 0.7f,
                        lateralBias = 0.25f,
                        crownDepth = 2.5f,
                        innerLeafDensity = 0.5f,
                        leafCountPerNode = 4,
                        leafCycle = true,
                        leafCyclePeriod = 3.2f,
                        leafVisibleFraction = 0.6f,
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

            float step = Mathf.Max(0.01f, stage.stepLength > 0 ? stage.stepLength : general.baseStepLength);
            float radiusFalloff = stage.radiusFalloffOverride > 0f ? stage.radiusFalloffOverride : general.radiusFalloff;

            var newBuds = new List<int>();

            for (int i = 0; i < buds.Count; i++)
            {
                int budId = buds[i];
                if (budId < 0 || budId >= nodes.Count) continue;

                Node bud = nodes[budId];
                Vector3 baseDir = bud.dir;
                float upBias = Mathf.Clamp01(1f - stage.apicalDominance) * 0.15f;
                if (upBias > 0f)
                {
                    baseDir = Vector3.Slerp(baseDir, Vector3.up, upBias).normalized;
                }

                int mainChild = TryAddChild(bud, baseDir, step, stage.mainAxisRadiusScale, radiusFalloff);
                if (mainChild >= 0) newBuds.Add(mainChild);

                if (NextFloat() < stage.branchProbability)
                {
                    Vector3 branchDir = RandomBranchDirection(baseDir, stage.branchAngleRange);
                    if (stage.lateralBias > 0f)
                    {
                        Vector3 lateral = Vector3.ProjectOnPlane(RandomUnitVector(), Vector3.up);
                        if (lateral.sqrMagnitude > 0.0001f)
                        {
                            branchDir = Vector3.Slerp(branchDir, lateral.normalized, Mathf.Clamp01(stage.lateralBias)).normalized;
                        }
                    }
                    if (stage.branchGravity > 0f)
                    {
                        Vector3 gravityDir = Vector3.down * stage.branchGravity;
                        branchDir = (branchDir + gravityDir).normalized;
                    }
                    float branchStep = step * Mathf.Max(0.1f, stage.branchLengthScale);
                    int branchChild = TryAddChild(bud, branchDir, branchStep, stage.branchRadiusScale, radiusFalloff);
                    if (branchChild >= 0) newBuds.Add(branchChild);
                }
            }

            buds.Clear();
            buds.AddRange(newBuds);

            RecalculateRadii();
        }

        private int TryAddChild(Node parent, Vector3 dir, float step, float radiusScale, float radiusFalloff)
        {
            if (nodes.Count >= general.maxNodes) return -1;

            Vector3 normalizedDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.up;
            int id = nodes.Count;

            float falloff = Mathf.Max(0.0001f, radiusFalloff);
            float childRadius = parent.radius * Mathf.Max(0.01f, radiusScale) * falloff;
            childRadius = Mathf.Max(general.minRadius, childRadius);

            var child = new Node
            {
                id = id,
                parent = parent.id,
                pos = parent.pos + normalizedDir * step,
                dir = normalizedDir,
                radius = childRadius,
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
                if (node.children.Count == 0) continue;

                float sum = 0f;
                for (int c = 0; c < node.children.Count; c++)
                {
                    float r = nodes[node.children[c]].radius;
                    sum += Mathf.Pow(r, exponent);
                }
                node.radius = Mathf.Pow(sum, 1f / exponent);
            }
        }

        private void UpdateLeaves(float dt)
        {
            var stage = GetStage();
            if (stage == null) return;
            float maxY = GetMaxHeight();

            for (int i = leaves.Count - 1; i >= 0; i--)
            {
                Leaf leaf = leaves[i];
                if (!IsTerminalNode(leaf.nodeId) && !leaf.isCotyledon)
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

            if (meshRenderer != null && meshRenderer.enabled)
            {
                var barkMesh = TreeMeshBuilder.BuildTubeMesh(positions, radii, parents, general.radialSegments);
                var leafMesh = BuildLeafMesh();

                bool useLeaves = leafMaterial != null && leafMesh.vertexCount > 0;
                var combine = new CombineInstance[useLeaves ? 2 : 1];
                combine[0] = new CombineInstance { mesh = barkMesh, transform = Matrix4x4.identity };
                if (useLeaves)
                {
                    combine[1] = new CombineInstance { mesh = leafMesh, transform = Matrix4x4.identity };
                }

                var finalMesh = new Mesh();
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                finalMesh.CombineMeshes(combine, false, false);

                meshFilter.sharedMesh = finalMesh;

                if (useLeaves)
                {
                    meshRenderer.sharedMaterials = new[] { barkMaterial, leafMaterial };
                }
                else
                {
                    meshRenderer.sharedMaterials = new[] { barkMaterial };
                }
            }

            if (lineMeshRenderer != null && lineMeshRenderer.enabled)
            {
                lineMeshRenderer.sharedMaterial = lineMaterial;
                lineMeshFilter.sharedMesh = TreeMeshBuilder.BuildLineMesh(segments);
            }
        }

        private Mesh BuildLeafMesh()
        {
            if (leafPrefab == null) return new Mesh();
            Mesh leafMeshAsset = leafPrefab.GetComponent<MeshFilter>()?.sharedMesh;
            if (leafMeshAsset == null || leaves.Count == 0) return new Mesh();

            var stage = GetStage();
            bool cycle = stage != null && stage.leafCycle && stage.leafCyclePeriod > 0f;
            float period = stage != null ? stage.leafCyclePeriod : 0f;
            float visibleFraction = stage != null ? Mathf.Clamp01(stage.leafVisibleFraction) : 1f;

            var combine = new List<CombineInstance>(leaves.Count);
            for (int i = 0; i < leaves.Count; i++)
            {
                Leaf leaf = leaves[i];
                if (leaf.dropping) continue;
                if (leaf.nodeId < 0 || leaf.nodeId >= nodes.Count) continue;

                if (cycle)
                {
                    float t = (age + leaf.cycleOffset) % period;
                    if (t > period * visibleFraction)
                    {
                        continue;
                    }
                }

                Node node = nodes[leaf.nodeId];
                Vector3 pos = node.pos + node.dir * 0.05f;
                Quaternion rot = Quaternion.LookRotation(node.dir, Vector3.up);
                Vector3 scale = Vector3.one * leaf.size;

                var matrix = Matrix4x4.TRS(pos, rot, scale);
                combine.Add(new CombineInstance { mesh = leafMeshAsset, transform = matrix });
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            if (combine.Count > 0)
            {
                mesh.CombineMeshes(combine.ToArray(), true, true);
            }
            return mesh;
        }
    }
}
