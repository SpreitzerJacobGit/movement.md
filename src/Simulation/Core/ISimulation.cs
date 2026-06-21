using Simulation.Math;

namespace Simulation.Core;

/// <summary>
/// Headless, deterministic simulation contract. Implementations must advance state
/// as a pure function of prior state plus applied inputs, so that a <see cref="Snapshot"/>
/// combined with a replay of inputs reproduces an identical tick sequence. This is what
/// makes rollback and headless re-simulation possible without a game engine.
/// </summary>
public interface ISimulation
{
    /// <summary>
    /// Advance the simulation by exactly one fixed timestep. Must be deterministic:
    /// same prior state + same pending inputs => identical post-state, bit-for-bit.
    /// </summary>
    void Tick();

    /// <summary>
    /// Return a render-readable snapshot of the current state. The returned structs are
    /// value types so the presentation layer cannot mutate simulation truth.
    /// </summary>
    SimulationState GetState();

    /// <summary>
    /// Stage inputs for the next <see cref="Tick"/>. Inputs are fixed-point / plain scalars
    /// (never floating point) so that input streams serialize identically across machines.
    /// </summary>
    void ApplyInput(PlayerInputs inputs);

    /// <summary>
    /// Produce a byte-exact serialized image of all state needed to resume simulation.
    /// Used for rollback save/restore and network load/synchronization.
    /// </summary>
    byte[] Snapshot();

    /// <summary>
    /// Replace all internal state from a buffer previously produced by <see cref="Snapshot"/>.
    /// </summary>
    void Restore(byte[] snapshot);
}

/// <summary>
/// Immutable view of simulation truth handed to the presentation layer. Only fields the
/// renderer needs belong here; authoritative sim state stays inside <see cref="ISimulation"/>.
/// </summary>
public readonly struct SimulationState
{
    public readonly int Tick;
    public readonly MoverState[] Movers;
    public readonly RopeState[] Ropes;

    public SimulationState(int tick, MoverState[] movers, RopeState[] ropes)
    {
        Tick = tick;
        Movers = movers;
        Ropes = ropes;
    }
}

/// <summary>
/// Per-mover render state. Position/Velocity use fixed-point vectors so the presentation
/// layer reads engine-agnostic truth; Yaw/Pitch are wrapped angles. Spin drives the
/// on-screen spin meter.
/// </summary>
public readonly struct MoverState
{
    public readonly FixedVec3 Position;
    public readonly FixedVec3 Velocity;
    public readonly Fixed Yaw;
    public readonly Fixed Pitch;
    public readonly FPSpin Spin;

    public MoverState(FixedVec3 position, FixedVec3 velocity, Fixed yaw, Fixed pitch, FPSpin spin)
    {
        Position = position;
        Velocity = velocity;
        Yaw = yaw;
        Pitch = pitch;
        Spin = spin;
    }
}

/// <summary>
/// Per-rope render state: the rope's stable id, the world-space node positions the renderer
/// should follow, and whether the rope is currently taut/active.
/// </summary>
public readonly struct RopeState
{
    public readonly int Id;
    public readonly FixedVec3[] Nodes;
    public readonly bool Active;

    public RopeState(int id, FixedVec3[] nodes, bool active)
    {
        Id = id;
        Nodes = nodes;
        Active = active;
    }
}
