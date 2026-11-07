using UnityEngine;

namespace WindPrototype
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class WindManager : MonoBehaviour
    {
        public static WindManager Active { get; private set; }

        const float SpringDestination = 0f;
        static readonly int BendDirectionId = Shader.PropertyToID("_BendDirection");
        static readonly int BendStrengthId = Shader.PropertyToID("_BendStrength");
        Vector3 lastDirection;
        Vector3 releaseDirection = Vector3.right;
        float lastStrength;
        float springValue;
        float springVelocity;
        bool globalsWritten;
#if UNITY_EDITOR
        double lastEditorTime;
#endif

        [Header("Wind")]
        [SerializeField, Tooltip("World-space wind direction, horizontal XZ only. Zero/invalid horizontal direction falls back to (1,0,0).")]
        Vector3 windDirection = Vector3.right;
        [SerializeField, Min(0f), Tooltip("While wind is present, it holds the bend at this strength without spring oscillation.")]
        float windStrength = 1f;

        [Header("Spring return (Edit and Play Mode)")]
        [SerializeField, Range(0f, 1000f), Tooltip("Spring stiffness toward the fixed rest value of zero. This is not wind force.")]
        float springForce = 60f;
        [SerializeField, Range(0f, 100f), Tooltip("Damps return velocity. Force 60 and Damp 5 give an oscillating release.")]
        float springDamp = 5f;

        public Vector3 WindDirection { get => windDirection; set => windDirection = SafeDirection(value); }
        public float WindStrength { get => windStrength; set => windStrength = Mathf.Max(0f, value); }
        public float SpringStrength => springValue;
        public float SpringVelocity => springVelocity;

        void OnEnable()
        {
            if (Active && Active != this)
            {
                Debug.LogWarning("A WindManager is already active. This duplicate has been disabled.", this);
                enabled = false;
                return;
            }
            Active = this;
#if UNITY_EDITOR
            lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.EditorApplication.update += EditorUpdate;
#endif
            globalsWritten = false;
            springValue = springVelocity = 0f;
            releaseDirection = Vector3.right;
            UpdateShaderGlobals(0f);
        }

        void Update()
        {
            // SubsystemRegistration also runs when Domain/Scene Reload is disabled.
            // ExecuteAlways may then receive Update without another OnEnable.
            if (!Active) OnEnable();
            if (Active == this && Application.isPlaying) UpdateShaderGlobals(Time.deltaTime);
        }

        void UpdateShaderGlobals(float deltaTime)
        {
            windDirection = SafeDirection(windDirection);
            Vector3 horizontal = new Vector3(windDirection.x, 0f, windDirection.z);
            float pull = Finite(windStrength) ? Mathf.Max(0f, windStrength) : 0f;
            //if (pull > 0f)
            //{
            //    // Driven/held phase: wind sets displacement. Do not integrate an oscillating target chase.
            //    springValue = pull;
            //    springVelocity = 0f;
            //    releaseDirection = horizontal.normalized;
            //}
            //else
            //{
            //    // Both Edit and Play Mode release toward the same fixed destination of zero.
            //    SpringValue(deltaTime);
            //}
            springValue += pull * deltaTime;
            SpringValue(deltaTime);

            if (!globalsWritten || !releaseDirection.Equals(lastDirection))
            {
                Shader.SetGlobalVector(BendDirectionId,
                    new Vector4(releaseDirection.x, releaseDirection.y, releaseDirection.z, 0f));
                lastDirection = releaseDirection;
            }
            if (!globalsWritten || springValue != lastStrength)
            {
                // Preserve negative output. The shader consumer must support signed Strength separately.
                Shader.SetGlobalFloat(BendStrengthId, springValue);
                lastStrength = springValue;
            }
            globalsWritten = true;
        }

        float SpringValue(float deltaTime)
        {
            if (!Finite(deltaTime) || deltaTime <= 0f) return springValue;
            float force = Finite(springForce) ? Mathf.Clamp(springForce, 0f, 1000f) : 60f;
            float damp = Finite(springDamp) ? Mathf.Clamp(springDamp, 0f, 100f) : 5f;
            float duration = Mathf.Min(deltaTime, 0.25f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(duration * 120f));
            float dt = duration / steps;
            for (int i = 0; i < steps; i++)
            {
                float acceleration = force * (SpringDestination - springValue) - damp * springVelocity;
                springVelocity += acceleration * dt;
                springValue += springVelocity * dt;
            }
            if (force > 0f && damp > 0f && Mathf.Abs(springValue) < 0.00001f && Mathf.Abs(springVelocity) < 0.00001f)
                springValue = springVelocity = 0f;
            return springValue;
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
            if (Active != this) return;
            Shader.SetGlobalVector(BendDirectionId, Vector4.zero);
            Shader.SetGlobalFloat(BendStrengthId, 0f);
            springValue = springVelocity = 0f;
            globalsWritten = false;
            Active = null;
        }

        // Horizontal wind cannot use a zero or vertical-only direction.
        static Vector3 SafeDirection(Vector3 direction)
        {
            float horizontalSquared = direction.x * direction.x + direction.z * direction.z;
            return Finite(direction.x) && Finite(direction.y) && Finite(direction.z) &&
                Finite(horizontalSquared) && horizontalSquared > 0.000001f
                ? direction : Vector3.right;
        }

        void OnValidate()
        {
            windDirection = SafeDirection(windDirection);
        }

#if UNITY_EDITOR
        void EditorUpdate()
        {
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            float deltaTime = (float)(now - lastEditorTime);
            lastEditorTime = now;
            if (Application.isPlaying || !this || !isActiveAndEnabled) return;
            if (!Active) OnEnable();
            if (Active != this) return;

            float previousValue = springValue;
            Vector3 previousDirection = lastDirection;
            UpdateShaderGlobals(deltaTime);
            if (previousValue != springValue || previousDirection != lastDirection)
            {
                // ExecuteAlways.Update alone is not a continuous editor clock.
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
        }
#endif
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() { Active = null; }
    }
}