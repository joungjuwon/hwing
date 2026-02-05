using UnityEngine;

namespace Hwing.GrassInteraction
{
    /// <summary>
    /// Updates global shader property _GrassBendCenter for interactive grass bending.
    /// Attach to Player or any object that should bend grass.
    /// </summary>
    public class GrassBendController : MonoBehaviour
    {
        [Header("Bend Settings")]
        [Tooltip("Radius around the object that affects grass")]
        public float bendRadius = 2.0f;
        
        [Tooltip("Height offset from transform position")]
        public float heightOffset = 0.0f;

        [Header("Multiple Objects Support")]
        [Tooltip("If true, this is the primary bender. Only one should be primary.")]
        public bool isPrimary = true;

        private static readonly int GrassBendCenterID = Shader.PropertyToID("_GrassBendCenter");

        private void Update()
        {
            if (!isPrimary) return;

            Vector3 pos = transform.position;
            pos.y += heightOffset;
            
            // Set global shader property: xyz = position, w = radius
            Shader.SetGlobalVector(GrassBendCenterID, new Vector4(pos.x, pos.y, pos.z, bendRadius));
        }

        private void OnDisable()
        {
            if (isPrimary)
            {
                // Reset bend center when disabled
                Shader.SetGlobalVector(GrassBendCenterID, Vector4.zero);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.5f);
            Vector3 pos = transform.position;
            pos.y += heightOffset;
            Gizmos.DrawWireSphere(pos, bendRadius);
        }
    }
}
