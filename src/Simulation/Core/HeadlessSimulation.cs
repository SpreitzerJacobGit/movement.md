using System;
using Simulation.Math;
using Simulation.Systems;

namespace Simulation.Core;

/// <summary>
/// Pure-C# reference <see cref="ISimulation"/> implementation that drives the validated
/// fixed-point rope solver headlessly (no Unity, no Photon Quantum). This is the refactor's
/// headline capability: the simulation core runs outside any game engine.
///
/// The Photon-backed <c>Simulation</c> described in REFACTOR_GUIDE.md §1.2 will supersede
/// this class once the Quantum SDK is restored to the repo; both implement
/// <see cref="ISimulation"/> so the presentation layer is agnostic to which one is live.
///
/// Scope note: only rope truth is currently mapped into <see cref="GetState()"/>. Mover
/// state is returned empty until the pure-C# Movement / Spin systems are ported — that
/// work is tracked separately and does not affect rope determinism.
/// </summary>
public sealed class HeadlessSimulation : ISimulation
{
    private readonly RopeSolver _solver;
    private readonly SolverParams _ropeParams;
    private PlayerInputs _pendingInputs;
    private int _tick;

    public HeadlessSimulation(Rope[] ropes, SolverParams ropeParams)
    {
        _ropeParams = ropeParams;
        _solver = new RopeSolver(ropes, ropeParams);
        _tick = 0;
    }

    /// <summary>
    /// Number of ticks stepped since construction or last <see cref="Restore"/>.
    /// </summary>
    public int TickCount => _tick;

    /// <inheritdoc />
    public void Tick()
    {
        _solver.Step();
        _tick++;
    }

    /// <inheritdoc />
    public SimulationState GetState()
    {
        var solverRopes = _solver.Ropes;
        var ropes = new RopeState[solverRopes.Count];
        for (int i = 0; i < solverRopes.Count; i++)
        {
            var rope = solverRopes[i];
            var src = rope.Nodes;
            var nodes = new FixedVec3[src.Length];
            for (int n = 0; n < src.Length; n++)
            {
                nodes[n] = src[n].Pos;
            }
            ropes[i] = new RopeState(rope.Id, nodes, active: true);
        }

        return new SimulationState(_tick, Array.Empty<MoverState>(), ropes);
    }

    /// <summary>
    /// Inputs are accepted and held to preserve the <see cref="ISimulation"/> contract, but
    /// the current pure-C# rope simulation has no per-tick input path: grapple create/destroy
    /// is driven by the Quantum GrappleSystem, not yet ported. The stored value is a no-op
    /// pending that port and is intentionally unused by <see cref="Tick"/>.
    /// </summary>
    public void ApplyInput(PlayerInputs inputs)
    {
        _pendingInputs = inputs;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Layout: [4 bytes tick (little-endian int32)] [8 bytes per solver long].
    /// </remarks>
    public byte[] Snapshot()
    {
        long[] words = _solver.Serialize();
        int byteCount = 4 + checked(words.Length * 8);
        byte[] buf = new byte[byteCount];
        Buffer.BlockCopy(BitConverter.GetBytes(_tick), 0, buf, 0, 4);
        if (words.Length > 0)
        {
            Buffer.BlockCopy(words, 0, buf, 4, words.Length * 8);
        }
        return buf;
    }

    /// <inheritdoc />
    public void Restore(byte[] snapshot)
    {
        if (snapshot == null || snapshot.Length < 4)
        {
            throw new ArgumentException("Snapshot too short: must contain at least a tick header.", nameof(snapshot));
        }

        _tick = BitConverter.ToInt32(snapshot, 0);
        int wordCount = (snapshot.Length - 4) / 8;
        long[] words = new long[wordCount];
        if (wordCount > 0)
        {
            Buffer.BlockCopy(snapshot, 4, words, 0, wordCount * 8);
        }
        _solver.Deserialize(words);
    }
}
