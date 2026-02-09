using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    [CreateAssetMenu(menuName = "HW/MysticForge Growth Preset", fileName = "MysticForgeGrowthPreset")]
    public class MysticForgeGrowthPreset : ScriptableObject
    {
        [Header("Controller")]
        public bool loopStages = false;
        public float timeScale = 1f;
        public float regenerateInterval = 0.5f;
        public bool clampRadiiToPrevious = true;

        [Header("Stages")]
        public List<HW_TreeGrowthController.GrowthStage> stages = new List<HW_TreeGrowthController.GrowthStage>();

        public void ApplyTo(HW_TreeGrowthController controller)
        {
            if (controller == null) return;

            controller.loopStages = loopStages;
            controller.timeScale = timeScale;
            controller.regenerateInterval = regenerateInterval;
            controller.clampRadiiToPrevious = clampRadiiToPrevious;
            controller.stages = CloneStages(stages);
        }

        private static List<HW_TreeGrowthController.GrowthStage> CloneStages(
            List<HW_TreeGrowthController.GrowthStage> source)
        {
            if (source == null) return new List<HW_TreeGrowthController.GrowthStage>();

            var cloned = new List<HW_TreeGrowthController.GrowthStage>(source.Count);
            foreach (var stage in source)
            {
                if (stage == null) continue;
                cloned.Add(CloneStage(stage));
            }

            return cloned;
        }

        private static HW_TreeGrowthController.GrowthStage CloneStage(HW_TreeGrowthController.GrowthStage stage)
        {
            var json = JsonUtility.ToJson(stage);
            return JsonUtility.FromJson<HW_TreeGrowthController.GrowthStage>(json);
        }
    }
}
