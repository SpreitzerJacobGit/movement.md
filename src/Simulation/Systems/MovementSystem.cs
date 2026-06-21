// Pure-C# port of the §5 MovementSystem (the Quantum ECS version in
// Assets/QuantumUser/Simulation/MovementSystem.cs). Same algorithm — gravity, yaw-relative
// accel (grounded/air), sink decay, aim-direction sink-scaled jump, analytical-Y=0 ground pass —
// expressed in Fixed/FixedVec3 instead of Quantum FP/FPVector3. No Unity, no Quantum ECS:
// runs headless, bit-identical across runs. Grapple coupling is intentionally NOT ported here
// (it lives with the rope solver); movement is tested standalone.

using Simulation.Core;
using Simulation.Math;

namespace Simulation.Systems;

public struct Mover
{
    public FixedVec3 Pos;
    public FixedVec3 Vel;
    public Fixed Yaw;
    public Fixed Pitch;
    public Fixed Grounded;   // 0 = airborne, 1 = grounded
    public Fixed PrevJump;   // 0/1 — previous-tick jump input for edge detection
    public Fixed Sink;       // momentum sink built by hard landings, decays over SinkDecaySeconds
}

public sealed class MovementConfig
{
    public Fixed Dt;
    public Fixed Gravity;
    public Fixed MaxSpeed;
    public Fixed GroundAccel;
    public Fixed AirAccel;
    public Fixed GroundFriction;
    public Fixed JumpBase;
    public Fixed JumpSinkScale;
    public Fixed SinkDecaySeconds;
    public Fixed SinkGain;
    public Fixed LookYawRate;
    public Fixed LookPitchRate;
    public Fixed PitchMin;
    public Fixed PitchMax;
}

public static class MovementSystem
{
    public static void Step(ref Mover m, in PlayerInputs input, MovementConfig cfg)
    {
        Fixed dt = cfg.Dt;
        bool grounded = m.Grounded > Fixed.Zero;

        // --- Look (yaw + pitch, clamped). Non-inverted: mouse-up -> pitch up. ---
        m.Yaw += input.AimYaw * cfg.LookYawRate;
        m.Pitch = m.Pitch + input.AimPitch * cfg.LookPitchRate;
        if (m.Pitch < cfg.PitchMin) m.Pitch = cfg.PitchMin;
        if (m.Pitch > cfg.PitchMax) m.Pitch = cfg.PitchMax;

        // --- Horizontal movement: accel toward the yaw-relative wish dir (grounded vs air). ---
        Fixed sinY = Fixed.Sin(m.Yaw);
        Fixed cosY = Fixed.Cos(m.Yaw);
        FixedVec3 fwd   = new FixedVec3( sinY, Fixed.Zero,  cosY);
        FixedVec3 right = new FixedVec3( cosY, Fixed.Zero, -sinY);
        FixedVec3 wishDir = right * input.MoveX + fwd * input.MoveZ;
        Fixed wishMag = wishDir.Length;
        if (wishMag > Fixed.One) wishDir = wishDir * (Fixed.One / wishMag);

        Fixed accel;
        FixedVec3 wishVel;
        if (grounded)
        {
            bool hasInput = wishMag > Fixed.Zero;
            accel   = hasInput ? cfg.GroundAccel : cfg.GroundFriction;
            wishVel = hasInput ? wishDir * cfg.MaxSpeed : FixedVec3.Zero;
        }
        else
        {
            accel   = cfg.AirAccel;
            wishVel = wishDir * cfg.MaxSpeed;
        }

        FixedVec3 horiz = new FixedVec3(m.Vel.X, Fixed.Zero, m.Vel.Z);
        FixedVec3 diff  = wishVel - horiz;
        Fixed maxStep = accel * dt;
        Fixed diffMag = diff.Length;
        if (diffMag > maxStep && diffMag > Fixed.Zero) diff = diff * (maxStep / diffMag);
        horiz = new FixedVec3(m.Vel.X + diff.X, Fixed.Zero, m.Vel.Z + diff.Z);

        // --- Sink decay + jump (toward aim dir, scaled by sink — weak without it). ---
        m.Sink = m.Sink - m.Sink * (dt / cfg.SinkDecaySeconds);
        if (m.Sink < Fixed.Zero) m.Sink = Fixed.Zero;

        // Set horizontal velocity from the accel step.
        m.Vel = new FixedVec3(horiz.X, m.Vel.Y, horiz.Z);

        // Jump: impulse in the aim direction, magnitude scaled by sink.
        if (input.Jump && m.PrevJump.Raw == 0 && grounded)
        {
            FixedVec3 aim = AimDir(m.Yaw, m.Pitch);
            Fixed force = cfg.JumpBase + m.Sink * cfg.JumpSinkScale;
            m.Vel = m.Vel + aim * force;
        }
        m.PrevJump = input.Jump ? Fixed.One : Fixed.Zero;

        // --- Gravity + integrate (semi-implicit Euler). ---
        m.Vel = new FixedVec3(m.Vel.X, m.Vel.Y - cfg.Gravity * dt, m.Vel.Z);
        m.Pos = m.Pos + m.Vel * dt;

        // --- Ground pass (analytical Y=0 plane). Landing impact builds sink (fuels next jump). ---
        if (m.Pos.Y <= Fixed.Zero)
        {
            Fixed impact = m.Vel.Length;
            Fixed gained = impact * cfg.SinkGain;
            if (gained > m.Sink) m.Sink = gained;
            m.Pos = new FixedVec3(m.Pos.X, Fixed.Zero, m.Pos.Z);
            if (m.Vel.Y < Fixed.Zero) m.Vel = new FixedVec3(m.Vel.X, Fixed.Zero, m.Vel.Z);
            m.Grounded = Fixed.One;
        }
        else
        {
            m.Grounded = Fixed.Zero;
        }
    }

    static FixedVec3 AimDir(Fixed yaw, Fixed pitch)
    {
        Fixed cp = Fixed.Cos(pitch);
        return new FixedVec3(cp * Fixed.Sin(yaw), Fixed.Sin(pitch), cp * Fixed.Cos(yaw));
    }
}
