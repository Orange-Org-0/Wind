using UnityEngine;

namespace WindPrototype.HierarchyExperiment
{
    [DisallowMultipleComponent, RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public sealed class HierarchyWindResponse : MonoBehaviour
    {
        public enum MotionStage { StaticPose, DirectionalWind, Spring, DelayedSpring }
        public HierarchyWindField field;
        public MotionStage stage = MotionStage.DirectionalWind;
        [Range(0, 60)] public float trunkAngle = 18;
        [Range(0, 60)] public float branchAngle = 24;
        [Range(0, 1)] public float branchVariation = 0.15f;
        [Range(0.1f, 5)] public float springFrequency = 1.1f;
        [Range(0.05f, 2)] public float dampingRatio = 0.38f;
        [Range(0, 2)] public float delaySeconds = 0.18f;
        public Vector2 staticPose = new Vector2(0.6f, 0);
        [Min(0)] public float boundsPadding = 4;

        const float Step = 1f / 120f;
        const int Capacity = 512;
        static readonly int ResponseId = Shader.PropertyToID("_HWResponse");
        static readonly int DelayedId = Shader.PropertyToID("_HWDelayed");
        static readonly int AnglesId = Shader.PropertyToID("_HWAngles");
        readonly Vector2[] history = new Vector2[Capacity];
        MaterialPropertyBlock block;
        MeshRenderer target;
        Vector2 value, velocity;
        float accumulator;
        double simulationTime;
        int head;
        MotionStage previousStage;

        void OnEnable()
        {
            target = GetComponent<MeshRenderer>();
            block = new MaterialPropertyBlock();
            Bounds bounds = GetComponent<MeshFilter>().sharedMesh
                ? GetComponent<MeshFilter>().sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
            bounds.Expand(Mathf.Max(0, boundsPadding) * 2);
            target.localBounds = bounds;
            ResetMotion();
        }

        [ContextMenu("Reset Motion")]
        public void ResetMotion()
        {
            value = velocity = Vector2.zero;
            System.Array.Clear(history, 0, history.Length);
            accumulator = 0;
            head = 0;
            simulationTime = Time.timeAsDouble;
            previousStage = stage;
        }

        void Update()
        {
            if (previousStage != stage) ResetMotion();
            Vector2 current;
            Vector2 delayed;
            if (stage == MotionStage.StaticPose)
                current = delayed = Vector2.ClampMagnitude(staticPose, 2);
            else if (stage == MotionStage.DirectionalWind)
                current = delayed = Sample(Time.timeAsDouble);
            else
            {
                accumulator += Mathf.Min(Time.deltaTime, 0.1f);
                while (accumulator >= Step)
                {
                    simulationTime += Step;
                    Integrate(Sample(simulationTime));
                    head = (head + 1) % Capacity;
                    history[head] = value;
                    accumulator -= Step;
                }
                // The result is sampled at a fixed rate; delay is expressed in seconds.
                current = value;
                float frames = Mathf.Clamp(delaySeconds, 0, 2) / Step;
                int whole = Mathf.FloorToInt(frames);
                delayed = Vector2.Lerp(history[(head - whole + Capacity) % Capacity],
                    history[(head - whole - 1 + Capacity) % Capacity], frames - whole);
                if (stage != MotionStage.DelayedSpring) delayed = current;
            }
            target.GetPropertyBlock(block);
            block.SetVector(ResponseId, new Vector4(current.x, current.y, 0, 0));
            block.SetVector(DelayedId, new Vector4(delayed.x, delayed.y, 0, 0));
            block.SetVector(AnglesId, new Vector4(trunkAngle, branchAngle, branchVariation, 0));
            target.SetPropertyBlock(block);
        }

        Vector2 Sample(double time) => field && field.isActiveAndEnabled
            ? field.Sample(transform.position, time) : Vector2.zero;

        void Integrate(Vector2 force)
        {
            // Stable implicit integration of x'' + 2*zeta*w*x' + w*w*x = w*w*force.
            float omega = 2 * Mathf.PI * Mathf.Clamp(springFrequency, 0.1f, 5);
            float damping = 2 * Mathf.Clamp(dampingRatio, 0.05f, 2) * omega;
            float stiffness = omega * omega;
            velocity = (velocity + Step * stiffness * (force - value)) /
                       (1 + Step * damping + Step * Step * stiffness);
            value += Step * velocity;
            if (value.sqrMagnitude > 9) { value = Vector2.ClampMagnitude(value, 3); velocity = Vector2.zero; }
        }

        void OnDisable()
        {
            if (!target || block == null) return;
            target.GetPropertyBlock(block);
            block.SetVector(ResponseId, Vector4.zero);
            block.SetVector(DelayedId, Vector4.zero);
            target.SetPropertyBlock(block);
            target.ResetLocalBounds();
        }
    }
}
