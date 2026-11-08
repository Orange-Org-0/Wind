using UnityEngine;

[System.Serializable]
public sealed class SpringValue
{
    private const float PositionEpsilon = 0.0001f;
    private const float VelocityEpsilon = 0.0001f;

    public float frequency = 1.0f;
    public float dampingratio = 0.5f;

    public float value { get; private set; } = 0.0f;
    public float velocity { get; private set; }
    public float acceleration { get; private set; }
    public float stiffness { get; private set; }
    public float damping { get; private set; }

    public float dest { get; private set; } = 0.0f;

 
    public SpringValue(float Frequency, float DampingRatio)
    {
        UpdateParams(Frequency, DampingRatio);
    }

    public float UpdateValue(float deltaTime)
    {
        // Public parameters can be assigned directly, so sanitize before use.
        frequency = IsFinite(frequency) ? Mathf.Max(0f, frequency) : 1.0f;
        dampingratio = IsFinite(dampingratio) ? Mathf.Max(0f, dampingratio) : 0.5f;

        if (!IsFinite(deltaTime) || deltaTime <= 0f)
            return value;

        stiffness = Mathf.Pow(2.0f * Mathf.PI * frequency, 2.0f);
        damping = 4.0f * Mathf.PI * frequency * dampingratio;
        acceleration = stiffness * (dest - value) - damping * velocity;
        float nextVelocity = velocity + acceleration * deltaTime;
        float nextValue = value + nextVelocity * deltaTime;

        // Reject an overflowing step without poisoning the current state.
        if (!IsFinite(stiffness) || !IsFinite(damping) ||
            !IsFinite(acceleration) || !IsFinite(nextVelocity) || !IsFinite(nextValue))
        {
            stiffness = damping = acceleration = 0f;
            return value;
        }

        velocity = nextVelocity;
        value = nextValue;

        // Both displacement and speed must be small before snapping to rest.
        if (frequency > 0f && dampingratio > 0f &&
            Mathf.Abs(value - dest) < PositionEpsilon &&
            Mathf.Abs(velocity) < VelocityEpsilon)
        {
            value = dest;
            velocity = 0f;
            acceleration = 0f;
        }
        return value;
    }


    public void UpdateParams(float newFrequency, float newDampingRatio)
    {
        frequency = IsFinite(newFrequency) ? Mathf.Max(0f, newFrequency) : 1.0f;
        dampingratio = IsFinite(newDampingRatio) ? Mathf.Max(0f, newDampingRatio) : 0.5f;
    }
    private static bool IsFinite(float number)
    {
        return !float.IsNaN(number) && !float.IsInfinity(number);
    }
}
