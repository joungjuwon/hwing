using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [AddComponentMenu("HW/Tree Growth Controller (Stages/Save)")]
    public class HW_TreeGrowthController : MonoBehaviour
    {
        [System.Serializable]
        public class GrowthStage
        {
            public string name = "Stage";
            public float duration = 5f;

            [Header("Trunk")]
            public float trunkHeight = 1f;
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
            public int numberOfBranches = 0;
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
            public int numberOfBranchlets = 0;
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
            public int numberOfLeaves = 0;
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
            public int numberOfLeavesBranchlet = 0;
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
            public int numberOfLeavesTrunk = 0;
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
        }

        [Header("Controller")]
        public HW_TreeRuntime generator;
        public bool manageGeneratorLifecycle = true;
        public bool loopStages = false;
        public float timeScale = 1f;
        public float regenerateInterval = 0.5f;
        public bool smoothInterpolation = true;
        public List<GrowthStage> stages = new List<GrowthStage>();
        [Header("Auto Save")]
        public bool enableSaveSystem = false;
        public bool autoSave = true;
        public bool loadOnStart = true;
        public float autoSaveInterval = 300f;
        public string saveKey = "";
        [Header("Clamp")]
        public bool clampRadiiToPrevious = true;
        [Header("Debug")]
        public bool manualControl = false; /* Disable auto-update of growth01 */


        private int stageIndex = 0;
        private float stageTime = 0f;
        private float regenTimer = 0f;
        private float autoSaveTimer = 0f;
        private float overallTime = 0f;
        private float lastTrunkRadius = 0f;
        private float lastBranchRadius = 0f;
        private float lastBranchletRadius = 0f;

        private void Awake()
        {
            if (generator == null)
            {
                generator = GetComponent<HW_TreeRuntime>();
            }

            if (generator != null && manageGeneratorLifecycle)
            {
                generator.regenerateEveryFrame = false;
                generator.regenerateOnValidate = false;
            }

            if (string.IsNullOrWhiteSpace(saveKey))
            {
                saveKey = $"MysticForgeGrowth_{gameObject.name}";
            }
        }

        private void Start()
        {
            if (generator == null || stages.Count == 0) return;
            stageIndex = Mathf.Clamp(stageIndex, 0, stages.Count - 1);
            stageTime = 0f;
            if (enableSaveSystem && loadOnStart)
            {
                LoadState();
            }
            overallTime = CalculateOverallTime();
            ApplyStageInterpolated(stages[stageIndex], GetNextStage(stageIndex), 0f);
            CacheRadii();
            generator.Generate();
        }

        private void Update()
        {
            if (generator == null || stages.Count == 0) return;

            float dt = Mathf.Max(0f, Time.deltaTime * Mathf.Max(0f, timeScale));
            AdvanceStage(dt);

            GrowthStage current = stages[stageIndex];
            GrowthStage next = GetNextStage(stageIndex);
            float t = GetStageT(current);
            if (smoothInterpolation)
            {
                t = t * t * (3f - 2f * t);
            }

            overallTime += dt;
            UpdateGrowth01();

            ApplyStageInterpolated(current, next, t);
            CacheRadii();

            regenTimer += dt;
            if (regenerateInterval <= 0f || regenTimer >= regenerateInterval)
            {
                regenTimer = 0f;
                generator.Generate();
            }

            if (enableSaveSystem && autoSave)
            {
                autoSaveTimer += dt;
                if (autoSaveInterval > 0f && autoSaveTimer >= autoSaveInterval)
                {
                    autoSaveTimer = 0f;
                    SaveState();
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (enableSaveSystem && autoSave)
            {
                SaveState();
            }
        }

        private void AdvanceStage(float dt)
        {
            if (stages.Count == 0) return;
            GrowthStage current = stages[stageIndex];
            float duration = Mathf.Max(0f, current.duration);
            stageTime += dt;

            if (duration <= 0f)
            {
                if (loopStages)
                {
                    stageIndex = (stageIndex + 1) % stages.Count;
                    stageTime = 0f;
                }
                return;
            }

            while (stageTime >= duration)
            {
                stageTime -= duration;
                stageIndex++;

                if (stageIndex >= stages.Count)
                {
                    if (loopStages)
                    {
                        stageIndex = 0;
                    }
                    else
                    {
                        stageIndex = stages.Count - 1;
                        stageTime = duration;
                        overallTime = Mathf.Min(overallTime, GetTotalDuration());
                        break;
                    }
                }

                current = stages[stageIndex];
                duration = Mathf.Max(0f, current.duration);
                if (duration <= 0f) break;
            }
        }

        private float GetStageT(GrowthStage current)
        {
            float duration = Mathf.Max(0f, current.duration);
            if (duration <= 0f) return 1f;
            if (!loopStages && stageIndex == stages.Count - 1)
            {
                return 1f;
            }
            return Mathf.Clamp01(stageTime / duration);
        }

        [System.Serializable]
        private struct SaveData
        {
            public int stageIndex;
            public float stageTime;
            public int randomSeed;
            public bool useRandomSeed;
            public string graphJson;
        }

        public void SaveState()
        {
            if (generator == null || stages.Count == 0) return;
            SaveData data = new SaveData
            {
                stageIndex = Mathf.Clamp(stageIndex, 0, stages.Count - 1),
                stageTime = Mathf.Max(0f, stageTime),
                randomSeed = generator.randomSeed,
                useRandomSeed = generator.useRandomSeed,
                graphJson = generator.GetPersistentGraphJson()
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
        }

        public void LoadState()
        {
            if (generator == null) return;
            if (!PlayerPrefs.HasKey(saveKey)) return;

            string json = PlayerPrefs.GetString(saveKey, "");
            if (string.IsNullOrEmpty(json)) return;

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (stages.Count == 0) return;

            stageIndex = Mathf.Clamp(data.stageIndex, 0, stages.Count - 1);
            stageTime = Mathf.Max(0f, data.stageTime);
            generator.useRandomSeed = data.useRandomSeed;
            generator.randomSeed = data.randomSeed;
            if (!string.IsNullOrEmpty(data.graphJson))
            {
                generator.SetPersistentGraphJson(data.graphJson);
            }
            overallTime = CalculateOverallTime();
        }

        private GrowthStage GetNextStage(int index)
        {
            if (stages.Count == 0) return null;
            int nextIndex = index + 1;
            if (nextIndex < stages.Count) return stages[nextIndex];
            return loopStages ? stages[0] : stages[index];
        }

        private void ApplyStageInterpolated(GrowthStage from, GrowthStage to, float t)
        {
            if (generator == null || from == null || to == null) return;

            generator.trunkHeight = Mathf.Lerp(from.trunkHeight, to.trunkHeight, t);
            float trunkRadius = Mathf.Lerp(from.trunkRadius, to.trunkRadius, t);
            generator.trunkRadiusCurvature = Mathf.Lerp(from.trunkRadiusCurvature, to.trunkRadiusCurvature, t);
            generator.trunkRadiusNoise = Mathf.Lerp(from.trunkRadiusNoise, to.trunkRadiusNoise, t);
            generator.trunkSubdivision = Mathf.RoundToInt(Mathf.Lerp(from.trunkSubdivision, to.trunkSubdivision, t));
            generator.trunkCrinkliness = Mathf.Lerp(from.trunkCrinkliness, to.trunkCrinkliness, t);
            generator.trunkSegments = Mathf.RoundToInt(Mathf.Lerp(from.trunkSegments, to.trunkSegments, t));
            generator.trunkBending = Mathf.Lerp(from.trunkBending, to.trunkBending, t);
            generator.includeStump = from.includeStump;
            generator.treeStumpStartPoint = Mathf.Lerp(from.treeStumpStartPoint, to.treeStumpStartPoint, t);
            generator.treeStumpWidth = Mathf.Lerp(from.treeStumpWidth, to.treeStumpWidth, t);
            generator.trunkMaterial = from.trunkMaterial;

            generator.numberOfBranches = Mathf.RoundToInt(Mathf.Lerp(from.numberOfBranches, to.numberOfBranches, t));
            generator.branchHeightMin = Mathf.Lerp(from.branchHeightMin, to.branchHeightMin, t);
            generator.branchHeightMax = Mathf.Lerp(from.branchHeightMax, to.branchHeightMax, t);
            float branchRadius = Mathf.Lerp(from.branchRadius, to.branchRadius, t);
            generator.branchLength = Mathf.Lerp(from.branchLength, to.branchLength, t);
            generator.branchRadiusCurvature = Mathf.Lerp(from.branchRadiusCurvature, to.branchRadiusCurvature, t);
            generator.branchRadiusNoise = Mathf.Lerp(from.branchRadiusNoise, to.branchRadiusNoise, t);
            generator.branchSubdivision = Mathf.RoundToInt(Mathf.Lerp(from.branchSubdivision, to.branchSubdivision, t));
            generator.branchCrinkliness = Mathf.Lerp(from.branchCrinkliness, to.branchCrinkliness, t);
            generator.branchSegments = Mathf.RoundToInt(Mathf.Lerp(from.branchSegments, to.branchSegments, t));
            generator.branchBending = Mathf.Lerp(from.branchBending, to.branchBending, t);
            generator.branchAngle = Mathf.Lerp(from.branchAngle, to.branchAngle, t);
            generator.adjustBranchLengthByHeight = from.adjustBranchLengthByHeight;
            generator.angleAdjustmentByHeight = from.angleAdjustmentByHeight;
            generator.gravity = Mathf.Lerp(from.gravity, to.gravity, t);

            generator.numberOfBranchlets = Mathf.RoundToInt(Mathf.Lerp(from.numberOfBranchlets, to.numberOfBranchlets, t));
            generator.branchletHeightMin = Mathf.Lerp(from.branchletHeightMin, to.branchletHeightMin, t);
            generator.branchletHeightMax = Mathf.Lerp(from.branchletHeightMax, to.branchletHeightMax, t);
            float branchletRadius = Mathf.Lerp(from.branchletRadius, to.branchletRadius, t);
            generator.branchletLength = Mathf.Lerp(from.branchletLength, to.branchletLength, t);
            generator.branchletRadiusCurvature = Mathf.Lerp(from.branchletRadiusCurvature, to.branchletRadiusCurvature, t);
            generator.branchletRadiusNoise = Mathf.Lerp(from.branchletRadiusNoise, to.branchletRadiusNoise, t);
            generator.branchletSubdivision = Mathf.RoundToInt(Mathf.Lerp(from.branchletSubdivision, to.branchletSubdivision, t));
            generator.branchletCrinkliness = Mathf.Lerp(from.branchletCrinkliness, to.branchletCrinkliness, t);
            generator.branchletSegments = Mathf.RoundToInt(Mathf.Lerp(from.branchletSegments, to.branchletSegments, t));
            generator.branchletBending = Mathf.Lerp(from.branchletBending, to.branchletBending, t);
            generator.branchletAngle = Mathf.Lerp(from.branchletAngle, to.branchletAngle, t);
            generator.branchletForwardAngle = Mathf.Lerp(from.branchletForwardAngle, to.branchletForwardAngle, t);
            generator.gravityBranchlets = Mathf.Lerp(from.gravityBranchlets, to.gravityBranchlets, t);
            generator.adjustBranchletLengthByHeight = from.adjustBranchletLengthByHeight;

            generator.generateBranchLeaves = from.generateBranchLeaves;
            generator.numberOfLeaves = Mathf.RoundToInt(Mathf.Lerp(from.numberOfLeaves, to.numberOfLeaves, t));
            generator.leafSize = Mathf.Lerp(from.leafSize, to.leafSize, t);
            generator.leafPositionMin = Mathf.Lerp(from.leafPositionMin, to.leafPositionMin, t);
            generator.leafPositionMax = Mathf.Lerp(from.leafPositionMax, to.leafPositionMax, t);
            generator.useLeafEndDistance = from.useLeafEndDistance;
            generator.leafEndDistanceMeters = Mathf.Lerp(from.leafEndDistanceMeters, to.leafEndDistanceMeters, t);
            generator.leafForwardRotation = Mathf.Lerp(from.leafForwardRotation, to.leafForwardRotation, t);
            generator.leafRotation = Mathf.Lerp(from.leafRotation, to.leafRotation, t);
            generator.leafRandomizeRotation = Mathf.Lerp(from.leafRandomizeRotation, to.leafRandomizeRotation, t);
            generator.leafBranchRandomPositioning = Mathf.Lerp(from.leafBranchRandomPositioning, to.leafBranchRandomPositioning, t);
            generator.leafBranchPositioning = Vector3.Lerp(from.leafBranchPositioning, to.leafBranchPositioning, t);
            generator.leafBranchSizeV3 = Vector3.Lerp(from.leafBranchSizeV3, to.leafBranchSizeV3, t);
            generator.leafSizeBranchRandom = Mathf.Lerp(from.leafSizeBranchRandom, to.leafSizeBranchRandom, t);

            generator.generateBranchletLeaves = from.generateBranchletLeaves;
            generator.numberOfLeavesBranchlet = Mathf.RoundToInt(Mathf.Lerp(from.numberOfLeavesBranchlet, to.numberOfLeavesBranchlet, t));
            generator.leafBranchletSize = Mathf.Lerp(from.leafBranchletSize, to.leafBranchletSize, t);
            generator.leafBranchletPositionMin = Mathf.Lerp(from.leafBranchletPositionMin, to.leafBranchletPositionMin, t);
            generator.leafBranchletPositionMax = Mathf.Lerp(from.leafBranchletPositionMax, to.leafBranchletPositionMax, t);
            generator.useBranchletLeafEndDistance = from.useBranchletLeafEndDistance;
            generator.branchletLeafEndDistanceMeters = Mathf.Lerp(from.branchletLeafEndDistanceMeters, to.branchletLeafEndDistanceMeters, t);
            generator.leafBranchletForwardRotation = Mathf.Lerp(from.leafBranchletForwardRotation, to.leafBranchletForwardRotation, t);
            generator.leafBranchletRotation = Mathf.Lerp(from.leafBranchletRotation, to.leafBranchletRotation, t);
            generator.leafBranchletRandomizeRotation = Mathf.Lerp(from.leafBranchletRandomizeRotation, to.leafBranchletRandomizeRotation, t);
            generator.leafBranchletPositioning = Vector3.Lerp(from.leafBranchletPositioning, to.leafBranchletPositioning, t);
            generator.leafBranchletRandomPositioning = Mathf.Lerp(from.leafBranchletRandomPositioning, to.leafBranchletRandomPositioning, t);
            generator.leafBranchletSizeV3 = Vector3.Lerp(from.leafBranchletSizeV3, to.leafBranchletSizeV3, t);
            generator.leafSizeBranchletRandom = Mathf.Lerp(from.leafSizeBranchletRandom, to.leafSizeBranchletRandom, t);

            generator.generateTrunkLeaves = from.generateTrunkLeaves;
            generator.numberOfLeavesTrunk = Mathf.RoundToInt(Mathf.Lerp(from.numberOfLeavesTrunk, to.numberOfLeavesTrunk, t));
            generator.leafTrunkSize = Mathf.Lerp(from.leafTrunkSize, to.leafTrunkSize, t);
            generator.leafTrunkPositionMin = Mathf.Lerp(from.leafTrunkPositionMin, to.leafTrunkPositionMin, t);
            generator.leafTrunkPositionMax = Mathf.Lerp(from.leafTrunkPositionMax, to.leafTrunkPositionMax, t);
            generator.leafTrunkForwardRotation = Mathf.Lerp(from.leafTrunkForwardRotation, to.leafTrunkForwardRotation, t);
            generator.leafTrunkRotation = Mathf.Lerp(from.leafTrunkRotation, to.leafTrunkRotation, t);
            generator.leafTrunkRandomizeRotation = Mathf.Lerp(from.leafTrunkRandomizeRotation, to.leafTrunkRandomizeRotation, t);
            generator.leafTrunkRandomPositioning = Mathf.Lerp(from.leafTrunkRandomPositioning, to.leafTrunkRandomPositioning, t);
            generator.leafTrunkPositioning = Vector3.Lerp(from.leafTrunkPositioning, to.leafTrunkPositioning, t);
            generator.leafTrunkSizeV3 = Vector3.Lerp(from.leafTrunkSizeV3, to.leafTrunkSizeV3, t);
            generator.leafSizeTrunkRandom = Mathf.Lerp(from.leafSizeTrunkRandom, to.leafSizeTrunkRandom, t);

            generator.generateTrueLeaves = from.generateTrueLeaves;
            generator.trueLeavesPairs = Mathf.RoundToInt(Mathf.Lerp(from.trueLeavesPairs, to.trueLeavesPairs, t));
            generator.trueLeavesStartHeight = Mathf.Lerp(from.trueLeavesStartHeight, to.trueLeavesStartHeight, t);
            generator.trueLeavesInterval = Mathf.Lerp(from.trueLeavesInterval, to.trueLeavesInterval, t);
            generator.trueLeavesSize = Mathf.Lerp(from.trueLeavesSize, to.trueLeavesSize, t);
            generator.trueLeavesAngleOffset = Mathf.Lerp(from.trueLeavesAngleOffset, to.trueLeavesAngleOffset, t);
            generator.trueLeavesForwardRotation = Mathf.Lerp(from.trueLeavesForwardRotation, to.trueLeavesForwardRotation, t);
            generator.trueLeavesRotation = Mathf.Lerp(from.trueLeavesRotation, to.trueLeavesRotation, t);
            generator.trueLeavesRotationRandom = Mathf.Lerp(from.trueLeavesRotationRandom, to.trueLeavesRotationRandom, t);
            generator.trueLeavesSizeV3 = Vector3.Lerp(from.trueLeavesSizeV3, to.trueLeavesSizeV3, t);
            generator.trueLeafPrefabOverride = from.trueLeafPrefabOverride;

            generator.leafPrefab = from.leafPrefab;
            generator.trunkLeafPrefabOverride = from.trunkLeafPrefabOverride;
            generator.leafMaterial = from.leafMaterial;
            generator.trunkLeafMaterialOverride = from.trunkLeafMaterialOverride;

            if (clampRadiiToPrevious)
            {
                trunkRadius = Mathf.Max(trunkRadius, lastTrunkRadius);
                branchRadius = Mathf.Max(branchRadius, lastBranchRadius);
                branchletRadius = Mathf.Max(branchletRadius, lastBranchletRadius);
            }

            generator.trunkRadius = trunkRadius;
            generator.branchRadius = branchRadius;
            generator.branchletRadius = branchletRadius;

            if (stages.Count > 0)
            {
                GrowthStage mature = stages[stages.Count - 1];
                generator.targetTrunkHeight = mature.trunkHeight;
                generator.targetTrunkRadius = mature.trunkRadius;
                generator.targetBranchLength = mature.branchLength;
                generator.targetBranchRadius = mature.branchRadius;
                generator.targetBranchletLength = mature.branchletLength;
                generator.targetBranchletRadius = mature.branchletRadius;
                generator.targetNumberOfBranches = mature.numberOfBranches;
                generator.targetNumberOfBranchlets = mature.numberOfBranchlets;
            }
        }

        private void CacheRadii()
        {
            if (!clampRadiiToPrevious || generator == null) return;
            lastTrunkRadius = generator.trunkRadius;
            lastBranchRadius = generator.branchRadius;
            lastBranchletRadius = generator.branchletRadius;
        }

        private float GetTotalDuration()
        {
            float total = 0f;
            for (int i = 0; i < stages.Count; i++)
            {
                total += Mathf.Max(0f, stages[i].duration);
            }
            return total;
        }

        private float CalculateOverallTime()
        {
            float total = 0f;
            for (int i = 0; i < stageIndex; i++)
            {
                total += Mathf.Max(0f, stages[i].duration);
            }
            total += Mathf.Clamp(stageTime, 0f, Mathf.Max(0f, stages[stageIndex].duration));
            return total;
        }

        private void UpdateGrowth01()
        {
            if (generator == null || manualControl) return;
            float total = GetTotalDuration();
            if (total <= 0f)
            {
                generator.growth01 = 1f;
                return;
            }

            float time = overallTime;
            if (loopStages)
            {
                time = Mathf.Repeat(time, total);
            }
            else
            {
                time = Mathf.Min(time, total);
            }

            float g = Mathf.Clamp01(time / total);
            if (smoothInterpolation)
            {
                g = g * g * (3f - 2f * g);
            }
            generator.growth01 = g;
        }
    }
}
