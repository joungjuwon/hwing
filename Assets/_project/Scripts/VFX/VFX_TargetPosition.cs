using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VFX
{
    [ExecuteAlways]
    public class TargetPosition : MonoBehaviour
    {
        [Tooltip("The target object to track.")]
        public Transform target;

        [Header("Tracking Configuration")]
        [Tooltip("How many historical points to send to the shader.")]
        [Range(1, 10)]
        public int numberOfPoints = 2;

        [Tooltip("The time delay in seconds between each historical point.")]
        public float timeInterval = 0.2f;

        [Header("Shader Properties")]
        [Tooltip("The base name for the shader vector properties. A number will be appended (e.g., _TargetTurbulencePose1, _TargetTurbulencePose2).")]
        public string shaderPropertyBaseName = "_TargetTurbulencePose";

        [Tooltip("For backward compatibility. If true and numberOfPoints is 2, it will use '_TargetTurbulencePose' and '_TargetTurbulencePose2' instead of appending numbers.")]
        public bool useLegacyNamingForTwoPoints = true;

        private int[] shaderPropertyIDs;

        private Queue<(float time, Vector3 position)> positionHistory = new Queue<(float, Vector3)>();
        private float maxHistoryDuration;

        void OnEnable()
        {
            Initialize();
#if UNITY_EDITOR
            EditorApplication.update += UpdateInEditor;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= UpdateInEditor;
#endif
        }
        
        // OnValidate is called when the script is loaded or a value is changed in the inspector.
        void OnValidate()
        {
            // Re-initialize when properties are changed in the editor
            if (gameObject.activeInHierarchy)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            // Calculate the maximum time we need to keep in history
            maxHistoryDuration = timeInterval * (numberOfPoints > 0 ? numberOfPoints - 1 : 0) + (timeInterval * 0.5f); // Add a small buffer

            shaderPropertyIDs = new int[numberOfPoints];

            if (useLegacyNamingForTwoPoints && numberOfPoints == 2)
            {
                // Special case for legacy naming to maintain compatibility with old shaders
                shaderPropertyIDs[0] = Shader.PropertyToID(shaderPropertyBaseName); // _TargetTurbulencePose
                shaderPropertyIDs[1] = Shader.PropertyToID(shaderPropertyBaseName + "2"); // _TargetTurbulencePose2
            }
            else
            {
                for (int i = 0; i < numberOfPoints; i++)
                {
                    // If only one point, use the base name directly. Otherwise, append a number.
                    string propertyName = (numberOfPoints == 1) ? shaderPropertyBaseName : $"{shaderPropertyBaseName}{i + 1}";
                    shaderPropertyIDs[i] = Shader.PropertyToID(propertyName);
                }
            }
        }

        void FixedUpdate()
        {
            if (Application.isPlaying)
            {
                UpdateShader(Time.realtimeSinceStartup);
            }
        }

#if UNITY_EDITOR
        void UpdateInEditor()
        {
            if (!Application.isPlaying)
            {
                UpdateShader((float)EditorApplication.timeSinceStartup);
            }
        }
#endif

        void UpdateShader(float currentTime)
        {
            if (target == null) return;

            // Add current position to history
            positionHistory.Enqueue((currentTime, target.position));

            // Clean up old history entries that are no longer needed
            while (positionHistory.Count > 0 && currentTime - positionHistory.Peek().time > maxHistoryDuration)
            {
                positionHistory.Dequeue();
            }

            // Find and set the position for each required point in time
            for (int i = 0; i < numberOfPoints; i++)
            {
                float targetTime = currentTime - (i * timeInterval);
                Vector3 positionForPoint = GetClosestPosition(targetTime);
                Shader.SetGlobalVector(shaderPropertyIDs[i], positionForPoint);
            }
        }

        Vector3 GetClosestPosition(float targetTime)
        {
            // Fallback to current position if history is empty
            Vector3 closestPos = target != null ? target.position : Vector3.zero;
            float closestDiff = float.MaxValue;

            foreach (var (time, pos) in positionHistory)
            {
                float diff = Mathf.Abs(time - targetTime);
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestPos = pos;
                }
            }

            return closestPos;
        }
    }
}