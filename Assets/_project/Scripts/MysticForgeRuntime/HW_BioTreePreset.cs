using UnityEngine;
using UnityEngine.Serialization;

namespace MysticForgeRuntime
{
    [CreateAssetMenu(fileName = "HW_BioTreePreset", menuName = "Mystic Forge/Bio Tree Preset")]
    public class HW_BioTreePreset : ScriptableObject
    {
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
        [Range(0f, 1f)] public float gravityStrength = 0.3f;

        [Header("Foliage")]
        public GameObject leafPrefab;
        public Material leafMaterial;
        [Range(0, 10)] public int leavesPerBranch = 5;
        public float leafScale = 1.0f;

        [Header("Texture & Material")]
        public Material treeMaterial;

        public void ApplyTo(HW_BioTreeRuntime target)
        {
            if (target == null) return;

            target.growthCycle = growthCycle;
            target.secondsToFullGrowth = secondsToFullGrowth;
            target.autoGrow = autoGrow;
            target.maxTrunkHeight = maxTrunkHeight;
            target.maxTrunkThickness = maxTrunkThickness;
            target.maxRecursion = maxRecursion;
            target.lengthDecay = lengthDecay;
            target.branchingAngle = branchingAngle;
            target.noiseIntensity = noiseIntensity;
            target.lengthRandomness = lengthRandomness;
            target.maxVerticalAngle = maxVerticalAngle;
            target.sensingSamples = sensingSamples;
            target.repulsionStrength = repulsionStrength;
            target.gravityStrength = gravityStrength;
            target.leafPrefab = leafPrefab;
            target.leafMaterial = leafMaterial;
            target.leavesPerBranch = leavesPerBranch;
            target.leafScale = leafScale;
            target.treeMaterial = treeMaterial;
        }

        public void CaptureFrom(HW_BioTreeRuntime source)
        {
            if (source == null) return;

            growthCycle = source.growthCycle;
            secondsToFullGrowth = source.secondsToFullGrowth;
            autoGrow = source.autoGrow;
            maxTrunkHeight = source.maxTrunkHeight;
            maxTrunkThickness = source.maxTrunkThickness;
            maxRecursion = source.maxRecursion;
            lengthDecay = source.lengthDecay;
            branchingAngle = source.branchingAngle;
            noiseIntensity = source.noiseIntensity;
            lengthRandomness = source.lengthRandomness;
            maxVerticalAngle = source.maxVerticalAngle;
            sensingSamples = source.sensingSamples;
            repulsionStrength = source.repulsionStrength;
            gravityStrength = source.gravityStrength;
            leafPrefab = source.leafPrefab;
            leafMaterial = source.leafMaterial;
            leavesPerBranch = source.leavesPerBranch;
            leafScale = source.leafScale;
            treeMaterial = source.treeMaterial;
        }
    }
}
