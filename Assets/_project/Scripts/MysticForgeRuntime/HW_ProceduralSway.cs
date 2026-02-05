using System.Collections.Generic;
using UnityEngine;

namespace MysticForgeRuntime
{
    public class HW_ProceduralSway : MonoBehaviour
    {
        [Header("Sway Settings")]
        public float speed = 1.0f;
        public float baseSwayAmount = 2.0f; // Degrees
        public float noiseScale = 0.5f;
        
        [Header("Wind Direction")]
        public Vector3 windDirection = new Vector3(1, 0, 1);
        
        [Header("Hierarchy Influence")]
        [Range(0f, 2f)] public float depthMultiplier = 1.2f; // Sway increases with depth
        
        private List<Transform> _bones = new List<Transform>();
        private List<Quaternion> _initialRotations = new List<Quaternion>();
        private List<float> _boneOffsets = new List<float>();
        private List<int> _boneDepths = new List<int>();
        private List<float> _stiffness = new List<float>();

        public void BindBones(List<Transform> boneList, List<float> stiffnessList = null)
        {
            _bones = new List<Transform>(boneList);
            _initialRotations.Clear();
            _boneOffsets.Clear();
            _boneDepths.Clear();
            _stiffness = (stiffnessList != null && stiffnessList.Count == boneList.Count) ? new List<float>(stiffnessList) : new List<float>();

            for (int i = 0; i < _bones.Count; i++)
            {
                Transform bone = _bones[i];
                if(bone == null) continue;
                _initialRotations.Add(bone.localRotation);
                
                // Deterministic offset based on position/index
                float stableHash = (bone.localPosition.x * 73.1f + bone.localPosition.y * 19.3f + bone.localPosition.z * 5.7f);
                _boneOffsets.Add(stableHash);
                
                int depth = 0;
                Transform p = bone.parent;
                while(p != null && p != this.transform) { depth++; p = p.parent; }
                _boneDepths.Add(depth);
                
                // Default stiffness if not provided
                if (_stiffness.Count <= i) _stiffness.Add(0f);
            }
        }

        void Update()
        {
            if (_bones == null || _bones.Count == 0) return;

            float time = Time.time * speed;
            Vector3 windAxis = Vector3.Cross(windDirection.normalized, Vector3.up);

            for (int i = 0; i < _bones.Count; i++)
            {
                if (_bones[i] == null) continue;

                float offset = _boneOffsets[i] * noiseScale;
                int depth = _boneDepths[i];
                float stiff = _stiffness[i]; // 0 = flexible, 1 = rigid
                
                // Sway calculation
                // Apply Stiffness: Thick branches (stiffness ~ 1) sway very little
                float flexibility = Mathf.Pow(1f - stiff, 2f); // Non-linear curve for better feel
                
                float angle = Mathf.Sin(time + offset + depth * 0.5f) * baseSwayAmount * flexibility;
                float noiseAngle = Mathf.Cos(time * 0.7f + offset) * baseSwayAmount * 0.5f * flexibility;

                Quaternion swayRot = Quaternion.AngleAxis(angle, windAxis) * Quaternion.AngleAxis(noiseAngle, Vector3.forward);
                
                _bones[i].localRotation = _initialRotations[i] * swayRot;
            }
        }
    }
}
