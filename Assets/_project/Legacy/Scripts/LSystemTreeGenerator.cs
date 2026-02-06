using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    public class LSystemTreeGenerator : MonoBehaviour
    {
        public enum GrowthMode
        {
            Sequence,
            PathLength
        }

        [Header("Preset")]
        public LSystemPreset preset;
        public bool autoGenerate = true;
        public bool regenerateInEditor = true;
        public int iterationOverride = -1;
        public bool useSeedOverride = false;
        public int seedOverride = 0;

        [Header("Rendering")]
        public Material barkMaterial;
        public Material leafMaterial;
        public GameObject leafPrefab;

        [Header("Growth")]
        public bool animateGrowth = false;
        [Range(0f, 1f)]
        public float previewProgress = 1f;
        public float growthDuration = 6f;
        public bool loopGrowth = false;
        [Tooltip("When growth completes, rebuild a smooth tube mesh.")]
        public bool useTubeWhenComplete = true;
        public GrowthMode growthMode = GrowthMode.PathLength;

        private MeshFilter barkFilter;
        private MeshRenderer barkRenderer;
        private MeshFilter leafFilter;
        private MeshRenderer leafRenderer;

        private readonly List<Vector3> cachedPositions = new List<Vector3>();
        private readonly List<int> cachedParents = new List<int>();
        private readonly List<float> cachedRadii = new List<float>();
        private readonly List<SegmentRecord> cachedSegments = new List<SegmentRecord>();
        private readonly List<LeafRecord> cachedLeaves = new List<LeafRecord>();
        private readonly List<TreeMeshBuilder.Segment> segmentBuffer = new List<TreeMeshBuilder.Segment>();
        private readonly List<Matrix4x4> leafMatrixBuffer = new List<Matrix4x4>();
        private bool hasCachedData;
        private float growthStartTime;
        private float lastRenderedProgress = -1f;
        private float maxPathDistance;

        private struct TurtleState
        {
            public Vector3 position;
            public Quaternion rotation;
            public float step;
            public float radius;
            public int nodeIndex;
            public int segmentIndex;
            public float distance;
        }

        private struct LeafRecord
        {
            public Matrix4x4 matrix;
            public int birthSegmentIndex;
            public float birthDistance;
        }

        private struct SegmentRecord
        {
            public TreeMeshBuilder.Segment segment;
            public float startDistance;
            public float endDistance;
        }

        private void OnEnable()
        {
            if (autoGenerate)
            {
                Generate();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && regenerateInEditor)
            {
                Generate();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || !animateGrowth || !hasCachedData) return;

            float duration = Mathf.Max(0.01f, growthDuration);
            float t = (Time.time - growthStartTime) / duration;
            if (loopGrowth)
            {
                t = Mathf.Repeat(t, 1f);
            }
            else
            {
                t = Mathf.Clamp01(t);
            }

            if (!loopGrowth && t >= 0.999f && lastRenderedProgress >= 0.999f) return;
            RenderAtProgress(t);
        }

        public void Generate()
        {
            if (preset == null) return;
            EnsureRenderers();

            int iterations = iterationOverride >= 0 ? iterationOverride : preset.iterations;
            int seed = useSeedOverride ? seedOverride : preset.seed;
            if (seed == 0) seed = Random.Range(1, int.MaxValue);

            var rng = new System.Random(seed);
            string sequence = Expand(preset, iterations, rng);

            BuildTreeData(sequence, rng);
            lastRenderedProgress = -1f;

            if (Application.isPlaying && animateGrowth)
            {
                growthStartTime = Time.time;
                RenderAtProgress(0f);
            }
            else
            {
                float progress = (!Application.isPlaying && animateGrowth) ? Mathf.Clamp01(previewProgress) : 1f;
                RenderAtProgress(progress);
            }
        }

        private void EnsureRenderers()
        {
            if (barkFilter == null)
            {
                barkFilter = GetComponent<MeshFilter>();
                if (barkFilter == null) barkFilter = gameObject.AddComponent<MeshFilter>();
            }
            if (barkRenderer == null)
            {
                barkRenderer = GetComponent<MeshRenderer>();
                if (barkRenderer == null) barkRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            Transform leafRoot = transform.Find("Leaves");
            if (leafRoot == null)
            {
                var go = new GameObject("Leaves");
                go.transform.SetParent(transform, false);
                leafRoot = go.transform;
            }
            if (leafFilter == null)
            {
                leafFilter = leafRoot.GetComponent<MeshFilter>();
                if (leafFilter == null) leafFilter = leafRoot.gameObject.AddComponent<MeshFilter>();
            }
            if (leafRenderer == null)
            {
                leafRenderer = leafRoot.GetComponent<MeshRenderer>();
                if (leafRenderer == null) leafRenderer = leafRoot.gameObject.AddComponent<MeshRenderer>();
            }
        }

        private void BuildLeafMesh(List<Matrix4x4> leafMatrices)
        {
            if (leafPrefab == null || leafMaterial == null || leafMatrices.Count == 0)
            {
                if (leafRenderer != null) leafRenderer.enabled = false;
                if (leafFilter != null) leafFilter.sharedMesh = null;
                return;
            }

            var leafMeshAsset = leafPrefab.GetComponent<MeshFilter>()?.sharedMesh;
            if (leafMeshAsset == null)
            {
                leafMeshAsset = leafPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
            }
            if (leafMeshAsset == null)
            {
                if (leafRenderer != null) leafRenderer.enabled = false;
                return;
            }

            var combine = new CombineInstance[leafMatrices.Count];
            for (int i = 0; i < leafMatrices.Count; i++)
            {
                combine[i] = new CombineInstance
                {
                    mesh = leafMeshAsset,
                    transform = leafMatrices[i]
                };
            }

            var leafMesh = new Mesh();
            leafMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            leafMesh.CombineMeshes(combine, true, true);

            leafFilter.sharedMesh = leafMesh;
            leafRenderer.sharedMaterial = leafMaterial;
            leafRenderer.enabled = true;
        }

        private void BuildTreeData(string sequence, System.Random rng)
        {
            cachedPositions.Clear();
            cachedParents.Clear();
            cachedRadii.Clear();
            cachedSegments.Clear();
            cachedLeaves.Clear();
            hasCachedData = false;
            maxPathDistance = 0f;

            var nodeToSegment = new List<int>(Mathf.Min(preset.maxNodes, 2048));
            var nodeDistance = new List<float>(Mathf.Min(preset.maxNodes, 2048));

            Vector3 initialDir = preset.initialDirection.sqrMagnitude > 0.0001f ? preset.initialDirection.normalized : Vector3.up;
            Vector3 initialUp = preset.initialUp.sqrMagnitude > 0.0001f ? preset.initialUp.normalized : Vector3.forward;
            if (Vector3.Cross(initialDir, initialUp).sqrMagnitude < 0.0001f)
            {
                initialUp = Vector3.right;
                if (Vector3.Cross(initialDir, initialUp).sqrMagnitude < 0.0001f)
                {
                    initialUp = Vector3.forward;
                }
            }
            Quaternion rotation = Quaternion.LookRotation(initialDir, initialUp);

            Vector3 position = Vector3.zero;
            float step = Mathf.Max(0.0001f, preset.stepLength);
            float stepScale = Mathf.Max(0.0001f, preset.stepLengthScale);
            float radius = Mathf.Max(0.0001f, preset.baseRadius);
            float radiusScale = Mathf.Max(0.0001f, preset.radiusScale);
            int currentIndex = 0;
            int currentSegmentIndex = -1;
            float currentDistance = 0f;

            cachedPositions.Add(position);
            cachedParents.Add(-1);
            cachedRadii.Add(radius);
            nodeToSegment.Add(-1);
            nodeDistance.Add(0f);

            var stack = new Stack<TurtleState>();

            for (int i = 0; i < sequence.Length; i++)
            {
                if (cachedPositions.Count >= preset.maxNodes) break;
                char c = sequence[i];
                switch (c)
                {
                    case 'F':
                    case 'A':
                    case 'B':
                    {
                        Vector3 dir = rotation * Vector3.forward;
                        ApplyTropism(preset, ref dir, ref rotation);
                        if (preset.rollJitter > 0f)
                        {
                            float roll = NextRange(rng, -preset.rollJitter, preset.rollJitter);
                            rotation = Quaternion.AngleAxis(roll, dir) * rotation;
                        }

                        float stepJitter = preset.stepJitter != 0f ? 1f + NextRange(rng, -preset.stepJitter, preset.stepJitter) : 1f;
                        float actualStep = Mathf.Max(0.0001f, step * stepJitter);

                        Vector3 newPos = position + dir * actualStep;
                        float startDistance = currentDistance;
                        float endDistance = currentDistance + actualStep;

                        float parentRadius = cachedRadii[currentIndex];
                        float radiusJitter = preset.radiusJitter != 0f ? 1f + NextRange(rng, -preset.radiusJitter, preset.radiusJitter) : 1f;
                        float childRadius = Mathf.Max(preset.minRadius, radius * radiusJitter);

                        cachedPositions.Add(newPos);
                        cachedParents.Add(currentIndex);
                        cachedRadii.Add(childRadius);

                        cachedSegments.Add(new SegmentRecord
                        {
                            segment = new TreeMeshBuilder.Segment
                            {
                                start = position,
                                end = newPos,
                                startRadius = parentRadius,
                                endRadius = childRadius
                            },
                            startDistance = startDistance,
                            endDistance = endDistance
                        });
                        currentSegmentIndex = cachedSegments.Count - 1;
                        nodeToSegment.Add(currentSegmentIndex);
                        nodeDistance.Add(endDistance);
                        if (endDistance > maxPathDistance) maxPathDistance = endDistance;

                        currentIndex = cachedPositions.Count - 1;
                        position = newPos;
                        step = Mathf.Max(0.0001f, step * stepScale);
                        radius = Mathf.Max(preset.minRadius, radius * radiusScale);
                        currentDistance = endDistance;
                        break;
                    }
                    case 'f':
                    {
                        Vector3 dir = rotation * Vector3.forward;
                        ApplyTropism(preset, ref dir, ref rotation);
                        float stepJitter = preset.stepJitter != 0f ? 1f + NextRange(rng, -preset.stepJitter, preset.stepJitter) : 1f;
                        float actualStep = Mathf.Max(0.0001f, step * stepJitter);
                        position += dir * actualStep;
                        step = Mathf.Max(0.0001f, step * stepScale);
                        currentDistance += actualStep;
                        break;
                    }
                    case '+':
                        rotation = Quaternion.AngleAxis(AngleWithJitter(preset, rng), rotation * Vector3.up) * rotation;
                        break;
                    case '-':
                        rotation = Quaternion.AngleAxis(-AngleWithJitter(preset, rng), rotation * Vector3.up) * rotation;
                        break;
                    case '&':
                        rotation = Quaternion.AngleAxis(AngleWithJitter(preset, rng), rotation * Vector3.right) * rotation;
                        break;
                    case '^':
                        rotation = Quaternion.AngleAxis(-AngleWithJitter(preset, rng), rotation * Vector3.right) * rotation;
                        break;
                    case '\\':
                        rotation = Quaternion.AngleAxis(AngleWithJitter(preset, rng), rotation * Vector3.forward) * rotation;
                        break;
                    case '/':
                        rotation = Quaternion.AngleAxis(-AngleWithJitter(preset, rng), rotation * Vector3.forward) * rotation;
                        break;
                    case '|':
                        rotation = Quaternion.AngleAxis(180f, rotation * Vector3.up) * rotation;
                        break;
                    case '[':
                        stack.Push(new TurtleState
                        {
                            position = position,
                            rotation = rotation,
                            step = step,
                            radius = radius,
                            nodeIndex = currentIndex,
                            segmentIndex = currentSegmentIndex,
                            distance = currentDistance
                        });
                        break;
                    case ']':
                        if (stack.Count > 0)
                        {
                            var s = stack.Pop();
                            position = s.position;
                            rotation = s.rotation;
                            step = s.step;
                            radius = s.radius;
                            currentIndex = s.nodeIndex;
                            currentSegmentIndex = s.segmentIndex;
                            currentDistance = s.distance;
                        }
                        break;
                    case 'L':
                    {
                        float leafScale = Mathf.Max(0.0001f, preset.leafSize);
                        if (preset.leafSizeJitter > 0f)
                        {
                            leafScale *= Mathf.Max(0.01f, 1f + NextRange(rng, -preset.leafSizeJitter, preset.leafSizeJitter));
                        }
                        Matrix4x4 m = Matrix4x4.TRS(position, rotation, Vector3.one * leafScale);
                        cachedLeaves.Add(new LeafRecord
                        {
                            matrix = m,
                            birthSegmentIndex = Mathf.Max(0, currentSegmentIndex),
                            birthDistance = currentDistance
                        });
                        break;
                    }
                }
            }

            if (preset.addLeavesOnTerminals && preset.leafProbability > 0f)
            {
                AddTerminalLeaves(preset, rng, cachedPositions, cachedParents, nodeToSegment, nodeDistance, cachedLeaves);
            }

            hasCachedData = cachedPositions.Count > 0;
        }

        private void RenderAtProgress(float progress)
        {
            if (!hasCachedData || preset == null) return;

            progress = Mathf.Clamp01(progress);
            if (progress >= 0.999f && useTubeWhenComplete)
            {
                lastRenderedProgress = progress;
                RenderFullTree();
                return;
            }

            if (growthMode == GrowthMode.PathLength)
            {
                RenderByPathLength(progress);
            }
            else
            {
                RenderBySequence(progress);
            }
        }

        private void RenderBySequence(float progress)
        {
            int totalSegments = cachedSegments.Count;
            if (totalSegments == 0)
            {
                barkFilter.sharedMesh = new Mesh();
                leafMatrixBuffer.Clear();
                BuildLeafMesh(leafMatrixBuffer);
                return;
            }

            float total = progress * totalSegments;
            int fullCount = Mathf.FloorToInt(total);
            float partial = total - fullCount;

            segmentBuffer.Clear();
            int clampedFull = Mathf.Clamp(fullCount, 0, totalSegments);
            for (int i = 0; i < clampedFull; i++)
            {
                segmentBuffer.Add(cachedSegments[i].segment);
            }

            if (partial > 0f && clampedFull < totalSegments)
            {
                var s = cachedSegments[clampedFull].segment;
                segmentBuffer.Add(new TreeMeshBuilder.Segment
                {
                    start = s.start,
                    end = Vector3.Lerp(s.start, s.end, partial),
                    startRadius = s.startRadius,
                    endRadius = Mathf.Lerp(s.startRadius, s.endRadius, partial)
                });
            }

            var barkMesh = TreeMeshBuilder.BuildBarkMesh(segmentBuffer, preset.radialSegments);
            barkFilter.sharedMesh = barkMesh;
            if (barkMaterial != null)
            {
                barkRenderer.sharedMaterial = barkMaterial;
            }

            int visibleSegments = clampedFull;
            if (partial >= 0.999f)
            {
                visibleSegments = Mathf.Min(clampedFull + 1, totalSegments);
            }
            BuildLeavesForVisibleSegments(visibleSegments);
            lastRenderedProgress = progress;
        }

        private void RenderByPathLength(float progress)
        {
            int totalSegments = cachedSegments.Count;
            if (totalSegments == 0 || maxPathDistance <= 0.0001f)
            {
                barkFilter.sharedMesh = new Mesh();
                leafMatrixBuffer.Clear();
                BuildLeafMesh(leafMatrixBuffer);
                return;
            }

            float growthDistance = maxPathDistance * progress;
            segmentBuffer.Clear();

            for (int i = 0; i < totalSegments; i++)
            {
                var record = cachedSegments[i];
                if (growthDistance <= record.startDistance) continue;

                float denom = record.endDistance - record.startDistance;
                if (denom <= 0.0001f)
                {
                    segmentBuffer.Add(record.segment);
                    continue;
                }

                float t = (growthDistance - record.startDistance) / denom;
                if (t >= 1f)
                {
                    segmentBuffer.Add(record.segment);
                }
                else if (t > 0f)
                {
                    var s = record.segment;
                    segmentBuffer.Add(new TreeMeshBuilder.Segment
                    {
                        start = s.start,
                        end = Vector3.Lerp(s.start, s.end, t),
                        startRadius = s.startRadius,
                        endRadius = Mathf.Lerp(s.startRadius, s.endRadius, t)
                    });
                }
            }

            var barkMesh = TreeMeshBuilder.BuildBarkMesh(segmentBuffer, preset.radialSegments);
            barkFilter.sharedMesh = barkMesh;
            if (barkMaterial != null)
            {
                barkRenderer.sharedMaterial = barkMaterial;
            }

            BuildLeavesForDistance(growthDistance);
            lastRenderedProgress = progress;
        }

        private void RenderFullTree()
        {
            var barkMesh = TreeMeshBuilder.BuildTubeMesh(cachedPositions, cachedRadii, cachedParents, preset.radialSegments);
            barkFilter.sharedMesh = barkMesh;
            if (barkMaterial != null)
            {
                barkRenderer.sharedMaterial = barkMaterial;
            }

            leafMatrixBuffer.Clear();
            for (int i = 0; i < cachedLeaves.Count; i++)
            {
                leafMatrixBuffer.Add(cachedLeaves[i].matrix);
            }
            BuildLeafMesh(leafMatrixBuffer);
        }

        private void BuildLeavesForVisibleSegments(int visibleSegments)
        {
            leafMatrixBuffer.Clear();
            if (visibleSegments <= 0)
            {
                BuildLeafMesh(leafMatrixBuffer);
                return;
            }

            for (int i = 0; i < cachedLeaves.Count; i++)
            {
                int birth = Mathf.Max(0, cachedLeaves[i].birthSegmentIndex);
                if (birth < visibleSegments)
                {
                    leafMatrixBuffer.Add(cachedLeaves[i].matrix);
                }
            }

            BuildLeafMesh(leafMatrixBuffer);
        }

        private void BuildLeavesForDistance(float growthDistance)
        {
            leafMatrixBuffer.Clear();
            for (int i = 0; i < cachedLeaves.Count; i++)
            {
                if (cachedLeaves[i].birthDistance <= growthDistance)
                {
                    leafMatrixBuffer.Add(cachedLeaves[i].matrix);
                }
            }

            BuildLeafMesh(leafMatrixBuffer);
        }

        private static string Expand(LSystemPreset preset, int iterations, System.Random rng)
        {
            var ruleMap = BuildRuleMap(preset.rules);
            string current = preset.axiom ?? string.Empty;

            for (int i = 0; i < iterations; i++)
            {
                var sb = new StringBuilder(current.Length * 2);
                for (int c = 0; c < current.Length; c++)
                {
                    char symbol = current[c];
                    if (!ruleMap.TryGetValue(symbol, out var list))
                    {
                        sb.Append(symbol);
                        continue;
                    }

                    string replacement = PickReplacement(list, rng);
                    sb.Append(replacement);
                }
                current = sb.ToString();
            }

            return current;
        }

        private static Dictionary<char, List<LSystemRule>> BuildRuleMap(List<LSystemRule> rules)
        {
            var map = new Dictionary<char, List<LSystemRule>>();
            if (rules == null) return map;
            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                if (!map.TryGetValue(r.symbol, out var list))
                {
                    list = new List<LSystemRule>();
                    map[r.symbol] = list;
                }
                list.Add(r);
            }
            return map;
        }

        private static string PickReplacement(List<LSystemRule> rules, System.Random rng)
        {
            if (rules == null || rules.Count == 0) return string.Empty;
            if (rules.Count == 1) return rules[0].replacement ?? string.Empty;

            float total = 0f;
            for (int i = 0; i < rules.Count; i++)
            {
                total += Mathf.Max(0f, rules[i].probability);
            }
            if (total <= 0.0001f) return rules[0].replacement ?? string.Empty;

            double pick = rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < rules.Count; i++)
            {
                acc += Mathf.Max(0f, rules[i].probability);
                if (pick <= acc)
                {
                    return rules[i].replacement ?? string.Empty;
                }
            }

            return rules[0].replacement ?? string.Empty;
        }

        private static float AngleWithJitter(LSystemPreset preset, System.Random rng)
        {
            float jitter = preset.angleJitter;
            if (jitter <= 0f) return preset.angle;
            return preset.angle + NextRange(rng, -jitter, jitter);
        }

        private static float NextRange(System.Random rng, float min, float max)
        {
            return (float)(min + (rng.NextDouble() * (max - min)));
        }

        private static void ApplyTropism(LSystemPreset preset, ref Vector3 dir, ref Quaternion rotation)
        {
            if (preset.tropismStrength <= 0f) return;
            Vector3 t = preset.tropism;
            if (t.sqrMagnitude < 0.0001f) return;

            Vector3 bent = (dir + t.normalized * preset.tropismStrength).normalized;
            if (bent.sqrMagnitude < 0.0001f) return;
            dir = bent;
            rotation = Quaternion.LookRotation(dir, rotation * Vector3.up);
        }

        private static void AddTerminalLeaves(
            LSystemPreset preset,
            System.Random rng,
            List<Vector3> positions,
            List<int> parents,
            List<int> nodeToSegment,
            List<float> nodeDistance,
            List<LeafRecord> leafRecords)
        {
            int count = positions.Count;
            if (count == 0) return;

            var childCounts = new int[count];
            for (int i = 1; i < count; i++)
            {
                int p = parents[i];
                if (p >= 0 && p < count) childCounts[p]++;
            }

            for (int i = 1; i < count; i++)
            {
                if (childCounts[i] != 0) continue;
                if (rng.NextDouble() > preset.leafProbability) continue;

                float leafScale = Mathf.Max(0.0001f, preset.leafSize);
                if (preset.leafSizeJitter > 0f)
                {
                    leafScale *= Mathf.Max(0.01f, 1f + NextRange(rng, -preset.leafSizeJitter, preset.leafSizeJitter));
                }

                int p = parents[i];
                Vector3 dir = Vector3.up;
                if (p >= 0 && p < count)
                {
                    Vector3 d = positions[i] - positions[p];
                    if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
                }

                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
                var m = Matrix4x4.TRS(positions[i], rot, Vector3.one * leafScale);
                int birth = 0;
                if (i >= 0 && i < nodeToSegment.Count)
                {
                    birth = Mathf.Max(0, nodeToSegment[i]);
                }
                float birthDistance = 0f;
                if (i >= 0 && i < nodeDistance.Count)
                {
                    birthDistance = nodeDistance[i];
                }
                leafRecords.Add(new LeafRecord
                {
                    matrix = m,
                    birthSegmentIndex = birth,
                    birthDistance = birthDistance
                });
            }
        }
    }
}
