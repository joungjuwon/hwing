using UnityEngine;

namespace ProceduralTreeGeneratorByMysticForge
{
    public class HW_TreeLeafWither : MonoBehaviour
    {
        public float fallSpeed = 0.6f;
        public float swayAmplitude = 0.1f;
        public float swayFrequency = 1.4f;
        public float spinSpeed = 90f;
        public float lifetime = 12f;
        public float shrinkStart = 8f;
        public float shrinkDuration = 3f;

        private float timeAlive = 0f;
        private Vector3 swayAxis;

        private void Awake()
        {
            swayAxis = Random.onUnitSphere;
            if (swayAxis.sqrMagnitude <= 0.0001f)
            {
                swayAxis = Vector3.right;
            }
        }

        private void Update()
        {
            timeAlive += Time.deltaTime;

            float sway = Mathf.Sin(timeAlive * swayFrequency) * swayAmplitude;
            Vector3 offset = swayAxis * sway;
            transform.position += (Vector3.down * fallSpeed + offset) * Time.deltaTime;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

            if (timeAlive >= shrinkStart)
            {
                float t = Mathf.Clamp01((timeAlive - shrinkStart) / Mathf.Max(0.001f, shrinkDuration));
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, t);
            }

            if (timeAlive >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
