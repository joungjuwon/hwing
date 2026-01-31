using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [CreateAssetMenu(menuName = "ProceduralTree/Tree Growth Preset")]
    public class TreeGrowthPreset : ScriptableObject
    {
        public GrowthGeneralSettings general = new GrowthGeneralSettings();
        public List<GrowthStageSettings> stages = new List<GrowthStageSettings>();
    }

    [Serializable]
    public class GrowthGeneralSettings
    {
        public int seed = 0;
        public float baseStepLength = 0.2f;
        public float baseRadius = 0.08f;
        public float minRadius = 0.01f;
        public float radiusFalloff = 0.85f;
        public float pipeExponent = 2f;
        public int maxNodes = 2000;
        public int radialSegments = 6;
        public float lightFalloff = 0.15f;
        public float leafDropDuration = 2f;
        public float radiusSmoothing = 8f;
        public bool preventRadiusShrink = true;
        public float cotyledonSize = 0.2f;
        public float trueLeafStartAge = 1.2f;
        public float cotyledonSenescenceAge = 3.5f;
        public float secondaryGrowthStartAge = 0f;
        public float secondaryGrowthDuration = 6f;
        public float secondaryGrowthMinScale = 0.2f;
    }

    [Serializable]
    public class GrowthStageSettings
    {
        public string name = "Seedling";
        public float duration = 10f;
        public float stepLength = 0.2f;
        public float elongationSpeed = 0.25f;
        public float branchStartLength = 0.4f;
        public float branchPairInterval = 0.6f;
        public bool pairedBranching = true;
        public float pairRotationOffset = 0f;
        public float pairRotationStep = 90f;
        public float sympodialTakeoverLength = 0.6f;
        public float lateralActivationDelay = 0.2f;
        public float branchProbability = 0.1f;
        public Vector2 branchAngleRange = new Vector2(15f, 45f);
        public float mainAxisRadiusScale = 0.98f;
        public float branchRadiusScale = 0.8f;
        public float branchLengthScale = 0.9f;
        public float radiusFalloffOverride = 0f;
        public float branchGravity = 0.0f;
        public float lateralBias = 0.0f;
        public float crownDepth = 1.5f;
        public float innerLeafDensity = 0.5f;
        public float apicalDominance = 0.8f;
        public float pruningLightThreshold = 0.2f;
        public float leafDensity = 1f;
        public float leafSize = 0.2f;
        public int leafCountPerNode = 3;
        public bool leafCycle = true;
        public float leafCyclePeriod = 3f;
        public float leafVisibleFraction = 0.6f;
        public bool allowCotyledons = false;
        public bool removeCotyledonsOnEnter = false;
        public bool pruneInnerLeavesOnEnter = false;
    }
}
