using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    public class TreeGrowthManager : MonoBehaviour
    {
        [Tooltip("Seconds between growth ticks.")]
        public float tickInterval = 0.5f;
        public bool useUnscaledTime = false;

        private static TreeGrowthManager instance;
        private readonly List<TreeGrowthController> trees = new List<TreeGrowthController>();
        private float accumulator = 0f;

        public static TreeGrowthManager Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public static void Register(TreeGrowthController tree)
        {
            if (tree == null) return;

            if (instance == null)
            {
                instance = FindAnyObjectByType<TreeGrowthManager>();
                if (instance == null)
                {
                    Debug.LogWarning("[TreeGrowthManager] No manager found in scene.");
                    return;
                }
            }

            if (!instance.trees.Contains(tree))
            {
                instance.trees.Add(tree);
            }
        }

        public static void Unregister(TreeGrowthController tree)
        {
            if (instance == null || tree == null) return;
            instance.trees.Remove(tree);
        }

        private void Update()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            accumulator += dt;

            if (tickInterval <= 0f)
            {
                Tick(dt);
                accumulator = 0f;
                return;
            }

            while (accumulator >= tickInterval)
            {
                Tick(tickInterval);
                accumulator -= tickInterval;
            }
        }

        private void Tick(float dt)
        {
            for (int i = trees.Count - 1; i >= 0; i--)
            {
                if (trees[i] == null)
                {
                    trees.RemoveAt(i);
                    continue;
                }
                trees[i].OnGrowthTick(dt);
            }
        }
    }
}
