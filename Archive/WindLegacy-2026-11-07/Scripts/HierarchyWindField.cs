using UnityEngine;

namespace WindPrototype.HierarchyExperiment
{
    [DisallowMultipleComponent]
    public sealed class HierarchyWindField : MonoBehaviour
    {
        [Tooltip("World-space direction; horizontal XZ only.")]
        public Vector3 direction = Vector3.right;
        [Min(0)] public float strength = 0.6f;
        [Range(0, 1)] public float gustAmplitude = 0.25f;
        [Min(0)] public float gustSpeed = 0.65f;
        [Min(0)] public float spatialFrequency = 0.3f;

        public Vector2 Sample(Vector3 position, double time)
        {
            Vector2 horizontal = new Vector2(direction.x, direction.z);
            if (horizontal.sqrMagnitude < 0.000001f || strength <= 0) return Vector2.zero;
            horizontal.Normalize();
            float phase = (position.x * 0.73f + position.z * 1.17f) * spatialFrequency;
            float t = (float)(time % 100000.0) * gustSpeed;
            float noise = 0.65f * Mathf.Sin(t + phase) +
                          0.35f * Mathf.Sin(t * 1.731f + phase * 1.43f);
            return horizontal * Mathf.Clamp(strength * (1 + gustAmplitude * noise), 0, 2);
        }
    }
}
