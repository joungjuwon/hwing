using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [CreateAssetMenu(menuName = "ProceduralTree/L-System Preset")]
    public class LSystemPreset : ScriptableObject
    {
        [Header("Grammar")]
        [TextArea]
        public string axiom = "F";
        public int iterations = 4;
        public List<LSystemRule> rules = new List<LSystemRule>
        {
            new LSystemRule { symbol = 'F', replacement = "F[+F]F[-F]F", probability = 1f }
        };

        [Header("Turtle")]
        public float angle = 25f;
        [Tooltip("Random angle variation per turn (degrees).")]
        public float angleJitter = 0f;
        public float stepLength = 0.2f;
        [Tooltip("Multiplier applied after each forward step.")]
        public float stepLengthScale = 1f;
        [Tooltip("Random step variation per forward (fraction of stepLength).")]
        public float stepJitter = 0f;
        public Vector3 initialDirection = Vector3.up;
        public Vector3 initialUp = Vector3.forward;
        [Tooltip("Small roll variation per forward step (degrees).")]
        public float rollJitter = 0f;

        [Header("Radius")]
        public float baseRadius = 0.08f;
        [Tooltip("Multiplier applied after each forward step.")]
        public float radiusScale = 0.9f;
        [Tooltip("Random radius variation per forward (fraction of radius).")]
        public float radiusJitter = 0f;
        public float minRadius = 0.01f;

        [Header("Mesh")]
        public int radialSegments = 6;
        public int maxNodes = 5000;

        [Header("Leaves")]
        public float leafSize = 0.2f;
        [Tooltip("Random leaf size variation (fraction of leafSize).")]
        public float leafSizeJitter = 0f;
        public bool addLeavesOnTerminals = true;
        [Range(0f, 1f)]
        public float leafProbability = 0.8f;

        [Header("Tropism")]
        public Vector3 tropism = new Vector3(0f, -1f, 0f);
        [Range(0f, 1f)]
        public float tropismStrength = 0f;

        [Header("Random")]
        public int seed = 0;
    }

    [Serializable]
    public class LSystemRule
    {
        public char symbol = 'F';
        [TextArea]
        public string replacement = "F";
        [Range(0f, 1f)]
        public float probability = 1f;
    }
}
