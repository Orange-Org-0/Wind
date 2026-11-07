using System;

namespace WindPrototype
{
    public enum WindBendState { WindDriven, Recovering }

    /// <summary>Per-instance response state, independent of Unity and rendering.</summary>
    public sealed class WindBendStateMachine
    {
        public WindBendState State { get; private set; }
        public double BendX { get; private set; }
        public double BendZ { get; private set; }
        public double VelocityX { get; private set; }
        public double VelocityZ { get; private set; }
        public double RecoveryTime { get; private set; }

        public double MaximumAngle = 100.0;
        public double StrengthForMaximum = 1.0;
        public double DrivenResponseSeconds = 0.5;
        public double RecoveryDuration = 6.0;
        public int RecoveryCycles = 4;
        public double RestAngleThreshold = 0.05;
        public double RestSpeedThreshold = 0.1;
        public double WindOffThreshold = 0.0001;
        public double WindOnThreshold = 0.0002;

        bool windPresent;
        double capturedDuration, capturedDecay, capturedFrequency;

        public double BendAngle
        {
            get { return Math.Sqrt(BendX * BendX + BendZ * BendZ); }
        }

        public void Reset()
        {
            State = WindBendState.WindDriven;
            BendX = BendZ = VelocityX = VelocityZ = RecoveryTime = 0.0;
            windPresent = false;
        }

        public void Step(double directionX, double directionZ, double strength, double deltaTime)
        {
            if (!Finite(directionX) || !Finite(directionZ) || !Finite(strength)
                || !Finite(deltaTime) || deltaTime < 0.0)
                return;

            double length = Math.Sqrt(directionX * directionX + directionZ * directionZ);
            double off = Math.Max(0.0, WindOffThreshold);
            double on = Math.Max(off + 1e-8, WindOnThreshold);
            double threshold = windPresent ? off : on;
            windPresent = length > threshold && strength > threshold;

            if (windPresent)
            {
                State = WindBendState.WindDriven;
                RecoveryTime = 0.0;
                double amount = Clamp(strength / Math.Max(1e-6, StrengthForMaximum), 0.0, 1.0)
                              * Clamp(MaximumAngle, 0.0, 110.0);
                double targetX = directionX / length * amount;
                double targetZ = directionZ / length * amount;
                double rate = 6.0 / Math.Max(0.01, DrivenResponseSeconds);
                double x = BendX, z = BendZ, vx = VelocityX, vz = VelocityZ;
                Critical(ref x, ref vx, targetX, rate, deltaTime);
                Critical(ref z, ref vz, targetZ, rate, deltaTime);
                BendX = x; BendZ = z; VelocityX = vx; VelocityZ = vz;
                return;
            }

            if (State == WindBendState.WindDriven)
            {
                if (AtRest())
                {
                    Rest();
                    return;
                }
                State = WindBendState.Recovering;
                RecoveryTime = 0.0;
                capturedDuration = Math.Max(0.1, RecoveryDuration);
                // Reach about 0.1% of the initial envelope after the chosen duration.
                capturedDecay = Math.Log(1000.0) / capturedDuration;
                capturedFrequency = 2.0 * Math.PI * Math.Max(3, Math.Min(5, RecoveryCycles))
                                  / capturedDuration;
            }

            double remaining = Math.Max(0.0, capturedDuration - RecoveryTime);
            double dt = Math.Min(deltaTime, remaining);
            double rx = BendX, rz = BendZ, rvx = VelocityX, rvz = VelocityZ;
            Oscillate(ref rx, ref rvx, capturedDecay, capturedFrequency, dt);
            Oscillate(ref rz, ref rvz, capturedDecay, capturedFrequency, dt);
            BendX = rx; BendZ = rz; VelocityX = rvx; VelocityZ = rvz;
            RecoveryTime += dt;
            // A finite completion point prevents endless tiny state changes.
            // After duration the envelope is tiny; final snap is bounded by that decay.
            if (RecoveryTime >= capturedDuration)
                Rest();
        }

        bool AtRest()
        {
            return BendAngle <= Math.Max(0.0, RestAngleThreshold)
                && Math.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ)
                   <= Math.Max(0.0, RestSpeedThreshold);
        }

        void Rest()
        {
            BendX = BendZ = VelocityX = VelocityZ = RecoveryTime = 0.0;
            State = WindBendState.WindDriven;
        }

        static void Critical(ref double x, ref double v, double target, double rate, double dt)
        {
            double offset = x - target;
            double c = v + rate * offset;
            double decay = Math.Exp(-rate * dt);
            x = target + (offset + c * dt) * decay;
            v = (v - rate * c * dt) * decay;
        }

        static void Oscillate(ref double x, ref double v, double decay, double frequency, double dt)
        {
            double a = x;
            double b = (v + decay * x) / frequency;
            double phase = frequency * dt;
            double cs = Math.Cos(phase), sn = Math.Sin(phase);
            double envelope = Math.Exp(-decay * dt);
            double value = a * cs + b * sn;
            x = envelope * value;
            v = envelope * ((-a * frequency * sn + b * frequency * cs) - decay * value);
        }

        static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}