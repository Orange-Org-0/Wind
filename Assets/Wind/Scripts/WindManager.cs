using UnityEngine;

namespace WindPrototype
{
    [DisallowMultipleComponent]
    public sealed class WindManager : MonoBehaviour
    {
        public static WindManager Active { get; private set; }

        static readonly int BendDirectionId = Shader.PropertyToID("_BendDirection");
        Vector3 lastDirection;
        bool globalsWritten;

        [Header("Wind")]
        [SerializeField, Tooltip("World-space wind direction, horizontal XZ only. Zero/invalid horizontal direction falls back to (1,0,0).")]
        Vector3 windDirection = Vector3.right;
        [SerializeField, Min(0f), Tooltip("Wind strength.")]
        float windStrength = 1f;


        public Vector4[] WindHistory;

        public Vector3 WindDirection { get => windDirection; set => windDirection = SafeDirection(value); }
        public float WindStrength { get => windStrength; set => windStrength = Mathf.Max(0f, value); }

        void OnEnable()
        {
            if (Active && Active != this)
            {
                Debug.LogWarning("A WindManager is already active. This duplicate has been disabled.", this);
                enabled = false;
                return;
            }
            Active = this;
            globalsWritten = false;
            UpdateShaderGlobals();
            WindHistory = new Vector4[10];
        }

        void Update()
        {
            // SubsystemRegistration also runs when Domain/Scene Reload is disabled.
            // Re-register on the first frame if OnEnable was not called again.
            if (!Active) OnEnable();
            if (Active == this) UpdateShaderGlobals();
            
        }

        void UpdateShaderGlobals()
        {
            windDirection = SafeDirection(windDirection);
            Vector3 horizontal = new Vector3(windDirection.x, 0f, windDirection.z).normalized;
            if (!globalsWritten || !horizontal.Equals(lastDirection))
            {
                Shader.SetGlobalVector(BendDirectionId,
                    new Vector4(horizontal.x, horizontal.y, horizontal.z, 0f));
                lastDirection = horizontal;
            }
            globalsWritten = true;
        }

        void OnDisable()
        {
            if (Active != this) return;
            Shader.SetGlobalVector(BendDirectionId, Vector4.zero);
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

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() { Active = null; }
    }


}