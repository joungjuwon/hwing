using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [CreateAssetMenu(menuName = "MysticForge/Growth Preset", fileName = "MysticForgeGrowthPreset")]
    public class MysticForgeGrowthPreset : ScriptableObject
    {
        public bool loopStages = false;
        public float timeScale = 1f;
        public float regenerateInterval = 0.5f;
        public bool clampRadiiToPrevious = true;
        public List<HW_TreeGrowthController.GrowthStage> stages = new List<HW_TreeGrowthController.GrowthStage>();

        public void ApplyTo(HW_TreeGrowthController controller)
        {
            if (controller == null) return;

            controller.loopStages = loopStages;
            controller.timeScale = timeScale;
            controller.regenerateInterval = regenerateInterval;
            controller.clampRadiiToPrevious = clampRadiiToPrevious;

            controller.stages.Clear();
            foreach (var stage in stages)
            {
                // shallow copy is sufficient for inspector-authored data
                controller.stages.Add(stage);
            }
        }
    }
}
