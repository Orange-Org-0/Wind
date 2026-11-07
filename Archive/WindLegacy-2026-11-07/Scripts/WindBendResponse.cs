using UnityEngine;

namespace WindPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReedBendMeshData))]
    public sealed class WindBendResponse : MonoBehaviour
    {
        [Header("Wind source (optional; otherwise uses active manager)")]
        [SerializeField] WindManager windSource;

        [Header("Driven response")]
        [SerializeField, Range(0f, 110f)] float maximumAngle = 100f;
        [SerializeField, Min(0.001f)] float strengthForMaximum = 1f;
        [SerializeField, Min(0.01f)] float drivenResponseSeconds = 0.5f;

        [Header("Recovery")]
        [SerializeField, Range(3, 5)] int recoveryCycles = 4;
        [SerializeField, Min(0.1f)] float recoveryDuration = 6f;

        [Header("Runtime output (read only while playing)")]
        [SerializeField] WindBendState currentState;
        [SerializeField] float currentBendAngle;
        [SerializeField] Vector3 currentBendDirection = Vector3.right;
        [SerializeField] Vector2 currentAngularVelocity;
        [SerializeField] float recoveryElapsed;

        [Header("Scene debug")]
        [SerializeField] bool showDebugGizmo = true;
        [SerializeField, Min(0.01f)] float debugLength = 2f;

        static readonly int BendDirectionId = Shader.PropertyToID("_BendDir");
        static readonly int BendAngleId = Shader.PropertyToID("_BendStrength");
        Renderer targetRenderer;
        MaterialPropertyBlock rendererProperties;
        Vector4 originalDirection;
        float originalAngle;
        Vector4 lastDirection;
        float lastAngle;
        bool rendererBound;
        bool hasWrittenRenderer;
        WindBendStateMachine machine;
        public WindBendState CurrentState => machine != null ? machine.State : WindBendState.WindDriven;
        public float CurrentBendAngle => currentBendAngle;
        public Vector3 CurrentBendDirection => currentBendDirection;
        public Vector2 CurrentBendVector => machine != null
            ? new Vector2((float)machine.BendX, (float)machine.BendZ) : Vector2.zero;
        public Vector2 CurrentAngularVelocity => currentAngularVelocity;

        void OnEnable()
        {
            BindRenderer();
            if (machine == null) machine = new WindBendStateMachine();
            machine.Reset();
            currentBendDirection = Vector3.right;
            SyncOutput();
            ApplyBendToRenderer();
        }

        void Update()
        {
            machine.MaximumAngle = maximumAngle;
            machine.StrengthForMaximum = strengthForMaximum;
            machine.DrivenResponseSeconds = drivenResponseSeconds;
            machine.RecoveryCycles = recoveryCycles;
            machine.RecoveryDuration = recoveryDuration;

            WindManager source = windSource ? windSource : WindManager.Active;
            Vector3 direction = source && source.isActiveAndEnabled ? source.WindDirection : Vector3.zero;
            float strength = source && source.isActiveAndEnabled ? source.WindStrength : 0f;
            machine.Step(direction.x, direction.z, strength, Time.deltaTime);
            SyncOutput();
            ApplyBendToRenderer();
        }

        void SyncOutput()
        {
            currentState = machine.State;
            currentBendAngle = (float)machine.BendAngle;
            currentAngularVelocity = new Vector2((float)machine.VelocityX, (float)machine.VelocityZ);
            recoveryElapsed = (float)machine.RecoveryTime;
            if (currentBendAngle > 0.00001f)
                currentBendDirection = new Vector3((float)machine.BendX, 0f, (float)machine.BendZ)
                                     / currentBendAngle;
        }

        void BindRenderer()
        {
            rendererBound = false;
            hasWrittenRenderer = false;
            if (!TryGetComponent(out targetRenderer)) return;
            Material material = targetRenderer.sharedMaterial;
            if (!material || !material.HasProperty(BendDirectionId)
                          || !material.HasProperty(BendAngleId))
            {
                Debug.LogWarning("WindBendResponse requires _BendDir and _BendStrength on the renderer material.", this);
                return;
            }
            if (rendererProperties == null) rendererProperties = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(rendererProperties);
            originalDirection = rendererProperties.HasProperty(BendDirectionId)
                ? rendererProperties.GetVector(BendDirectionId) : material.GetVector(BendDirectionId);
            originalAngle = rendererProperties.HasProperty(BendAngleId)
                ? rendererProperties.GetFloat(BendAngleId) : material.GetFloat(BendAngleId);
            rendererBound = true;
        }

        void ApplyBendToRenderer()
        {
            if (!rendererBound || !targetRenderer) return;
            Vector4 direction = new Vector4(currentBendDirection.x, 0f, currentBendDirection.z, 0f);
            if (hasWrittenRenderer && direction.Equals(lastDirection) && currentBendAngle == lastAngle)
                return;
            // Merge the latest block so ReedBendMeshData's bounds remain intact.
            targetRenderer.GetPropertyBlock(rendererProperties);
            rendererProperties.SetVector(BendDirectionId, direction);
            rendererProperties.SetFloat(BendAngleId, currentBendAngle);
            targetRenderer.SetPropertyBlock(rendererProperties);
            lastDirection = direction;
            lastAngle = currentBendAngle;
            hasWrittenRenderer = true;
        }

        void OnDisable()
        {
            if (!hasWrittenRenderer || !targetRenderer) return;
            targetRenderer.GetPropertyBlock(rendererProperties);
            rendererProperties.SetVector(BendDirectionId, originalDirection);
            rendererProperties.SetFloat(BendAngleId, originalAngle);
            targetRenderer.SetPropertyBlock(rendererProperties);
            hasWrittenRenderer = false;
        }
        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmo) return;
            Gizmos.color = currentState == WindBendState.Recovering ? Color.cyan : Color.yellow;
            Vector3 direction = currentBendDirection.sqrMagnitude > 0.001f
                ? currentBendDirection.normalized : Vector3.right;
            float angle = currentBendAngle * Mathf.Deg2Rad;
            Vector3 tipDirection = Vector3.up * Mathf.Cos(angle) + direction * Mathf.Sin(angle);
            Gizmos.DrawLine(transform.position, transform.position + tipDirection * debugLength);
            Gizmos.DrawWireSphere(transform.position + tipDirection * debugLength, debugLength * 0.025f);
        }
    }
}