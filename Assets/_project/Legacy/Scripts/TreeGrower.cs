using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [RequireComponent(typeof(RuntimeTreeGenerator))]
    public class TreeGrower : MonoBehaviour
    {
        public float growthDuration = 5f;
        public bool loop = false;
        public bool autoStart = true;

        private RuntimeTreeGenerator generator;
        private float timer = 0f;
        private bool isGrowing = false;

        // Target values (captured from the generator at start)
        private float targetTrunkHeight;
        private float targetTrunkRadius;
        private float targetBranchLength;
        private float targetBranchRadius;
        private float targetBranchletLength;
        private float targetBranchletRadius;
        private float targetLeafSize;
        private float targetLeafBranchletSize;
        private float targetLeafTrunkSize;

        private void Start()
        {
            generator = GetComponent<RuntimeTreeGenerator>();
            
            // Capture target values
            targetTrunkHeight = generator.trunkHeight;
            targetTrunkRadius = generator.trunkRadius;
            targetBranchLength = generator.branchLength;
            targetBranchRadius = generator.branchRadius;
            targetBranchletLength = generator.branchletLength;
            targetBranchletRadius = generator.branchletRadius;
            targetLeafSize = generator.leafSize;
            targetLeafBranchletSize = generator.leafBranchletSize;
            targetLeafTrunkSize = generator.leafTrunkSize;

            // Set initial values to 0
            ResetToZero();

            if (autoStart)
            {
                StartGrowth();
            }
        }

        public void StartGrowth()
        {
            ResetToZero();
            isGrowing = true;
            timer = 0f;
        }

        private void ResetToZero()
        {
            generator.trunkHeight = 0.1f; // Minimum to avoid errors
            generator.trunkRadius = 0.01f;
            generator.branchLength = 0f;
            generator.branchRadius = 0f;
            generator.branchletLength = 0f;
            generator.branchletRadius = 0f;
            generator.leafSize = 0f;
            generator.leafBranchletSize = 0f;
            generator.leafTrunkSize = 0f;
            
            generator.GenerateTree();
        }

        private void Update()
        {
            if (!isGrowing) return;

            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / growthDuration);

            // Apply easing if desired (e.g., smoothstep)
            float t = Mathf.SmoothStep(0f, 1f, progress);

            // Interpolate values
            // We can stagger the growth: Trunk first, then branches, then leaves
            
            // Trunk grows from 0 to 1
            generator.trunkHeight = Mathf.Lerp(0.1f, targetTrunkHeight, t);
            generator.trunkRadius = Mathf.Lerp(0.01f, targetTrunkRadius, t);

            // Branches start growing after 20%
            float branchT = Mathf.InverseLerp(0.2f, 0.8f, t);
            generator.branchLength = Mathf.Lerp(0f, targetBranchLength, branchT);
            generator.branchRadius = Mathf.Lerp(0f, targetBranchRadius, branchT);

            // Branchlets start growing after 40%
            float branchletT = Mathf.InverseLerp(0.4f, 0.9f, t);
            generator.branchletLength = Mathf.Lerp(0f, targetBranchletLength, branchletT);
            generator.branchletRadius = Mathf.Lerp(0f, targetBranchletRadius, branchletT);

            // Leaves start growing after 60%
            float leafT = Mathf.InverseLerp(0.6f, 1.0f, t);
            generator.leafSize = Mathf.Lerp(0f, targetLeafSize, leafT);
            generator.leafBranchletSize = Mathf.Lerp(0f, targetLeafBranchletSize, leafT);
            generator.leafTrunkSize = Mathf.Lerp(0f, targetLeafTrunkSize, leafT);

            generator.GenerateTree();

            if (progress >= 1f)
            {
                if (loop)
                {
                    StartGrowth();
                }
                else
                {
                    isGrowing = false;
                }
            }
        }
    }
}
