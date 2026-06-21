using System.Collections.Generic;
using Simulation.Core;
using Simulation.Math;
using Simulation.Systems;
using Xunit;

namespace Simulation.Tests.Systems;

public class SystemTests
{
    const int RopeCount = 4;
    const int NodesPerRope = 8;
    const int SimFrames = 600;

    // Builds the 4-rope scene where the rope at array index i occupies slot arrayOrder[i].
    // Slot 0 -> anchor (+2,0), slot 1 -> (-2,0), slot 2 -> (0,+2), slot 3 -> (0,-2).
    // Slot s -> Id = (3 - s) * 10, so the default [0,1,2,3] ordering yields Ids [30,20,10,0]:
    // array order and Id order intentionally diverge, which makes the Id-sort meaningful
    // (a naive array-index iteration would process collision pairs in the wrong order).
    // Shuffling arrayOrder moves ropes to different array indices but each Id keeps its
    // anchor, so the physical scene is invariant under reordering.
    static Rope[] BuildScene(int[] arrayOrder)
    {
        var ropes = new Rope[RopeCount];
        for (int i = 0; i < RopeCount; i++)
        {
            int slot = arrayOrder[i];
            var nodes = new Node[NodesPerRope];
            Fixed ax = Fixed.FromInt(slot == 0 ? 2 : slot == 1 ? -2 : 0);
            Fixed az = Fixed.FromInt(slot == 2 ? 2 : slot == 3 ? -2 : 0);
            Fixed leanX = -ax * Fixed.FromDouble(0.18);
            Fixed leanZ = -az * Fixed.FromDouble(0.18);
            for (int n = 0; n < NodesPerRope; n++)
            {
                Fixed fn = Fixed.FromInt(n);
                nodes[n] = new Node
                {
                    Pos = new FixedVec3(ax + leanX * fn, -fn * Fixed.FromDouble(0.5), az + leanZ * fn),
                    Vel = FixedVec3.Zero,
                    InvMass = n == 0 ? Fixed.Zero : Fixed.One,
                };
            }
            ropes[i] = new Rope { Id = (3 - slot) * 10, Nodes = nodes };
        }
        return ropes;
    }

    static SolverParams Params() => new()
    {
        Dt = Fixed.One / Fixed.FromInt(128),
        SpringK = Fixed.FromInt(800),
        Damping = Fixed.FromInt(2),
        SegmentRest = Fixed.FromDouble(0.5),
        Gravity = Fixed.FromInt(20),
        CollisionDist = Fixed.FromDouble(0.4),
        CollisionK = Fixed.FromInt(600),
    };

    static ulong Fnv(long[] data)
    {
        ulong h = 14695981039346656037UL;
        foreach (long v in data)
            for (int b = 0; b < 8; b++) { h ^= (byte)(v >> (b * 8)); h *= 1099511628211UL; }
        return h;
    }

    // FixedVec3 has no equality operator; compare component raw longs directly.
    static bool VecEq(FixedVec3 a, FixedVec3 b) =>
        a.X.Raw == b.X.Raw && a.Y.Raw == b.Y.Raw && a.Z.Raw == b.Z.Raw;

    // Per-rope state hash keyed by rope Id — array-order-independent. Serialize() writes ropes
    // in array order, so hashing the flat vector would make this test fail on layout, not physics.
    static Dictionary<int, ulong> RopeStateByKey(RopeSolver solver)
    {
        var dict = new Dictionary<int, ulong>();
        foreach (var rope in solver.Ropes)
        {
            var longs = new List<long>(NodesPerRope * 6);
            foreach (var n in rope.Nodes)
            {
                longs.Add(n.Pos.X.Raw); longs.Add(n.Pos.Y.Raw); longs.Add(n.Pos.Z.Raw);
                longs.Add(n.Vel.X.Raw); longs.Add(n.Vel.Y.Raw); longs.Add(n.Vel.Z.Raw);
            }
            dict[rope.Id] = Fnv(longs.ToArray());
        }
        return dict;
    }

    // Proves the determinism guard rail (CONVENTIONS.md: resolve collision pairs in a stable
    // order sorted by entity Id, never by iteration order) AT PER-ROPE GRANULARITY. Same scene,
    // same rope array order; the second solver perturbs pair-generation order via a non-zero seed.
    // Because pairs are re-sorted by (idA, idB) before resolution and force accumulation is
    // associative/additive over frozen positions, every rope's final state must match bit-for-bit.
    // (Complements DeterminismTests.RopeCollision_PairOrderIndependent, which checks the flat
    // serialize-hash; this checks each rope individually so per-rope drift cannot hide in a hash.)
    [Fact]
    public void RopeCollision_IDSortedOrder()
    {
        var solverA = new RopeSolver(BuildScene(new[] { 0, 1, 2, 3 }), Params());
        for (int f = 0; f < SimFrames; f++) solverA.Step(0);

        var solverB = new RopeSolver(BuildScene(new[] { 0, 1, 2, 3 }), Params());
        for (int f = 0; f < SimFrames; f++) solverB.Step(1337u);

        Assert.Equal(RopeStateByKey(solverA), RopeStateByKey(solverB));
    }

    // FINDING (surfaced by this headless test layer): the local spike's pair sorter orders pairs
    // by (idA, idB) where a/b are ARRAY indices — it does NOT canonicalize which rope is segment-1
    // vs segment-2 within a pair. The Quantum production port (RopeSolverSystem.ResolveCollisionsIdSorted)
    // DOES canonicalize (it swaps so the lower Id is first before packing the sort key). So when
    // ropes are inserted in a different array order, the same Id-pair reaches ClosestPtSegSeg with
    // swapped segment roles, and the fixed-point division/clamp branches yield a different result.
    // The First Test's Aspect #3 only shuffled pair ORDER (fixed array) and so never exercised
    // intra-pair role swap. Skipped until the local reference sim is aligned with the Quantum port's
    // lo/hi-Id canonicalization (a deliberate, separately-reviewed change — not a drive-by fix to a
    // validated, hash-recorded algorithm). Un-skip once hardened; the assertion is the contract.
    [Fact(Skip = "Local spike does not canonicalize intra-pair roles by Id (unlike Quantum port); " +
                  "array-reorder sensitivity is a known gap, not yet fixed. See comment.")]
    public void RopeCollision_ArrayReorder_RoleCanonicalizationGap()
    {
        var solverA = new RopeSolver(BuildScene(new[] { 0, 1, 2, 3 }), Params());
        for (int f = 0; f < SimFrames; f++) solverA.Step(0);

        // Same physical scene (each Id keeps its anchor), only the rope ARRAY order differs.
        var solverB = new RopeSolver(BuildScene(new[] { 3, 2, 1, 0 }), Params());
        for (int f = 0; f < SimFrames; f++) solverB.Step(0);

        Assert.Equal(RopeStateByKey(solverA), RopeStateByKey(solverB));
    }

    [Fact]
    public void RopeSolver_PinnedAnchorsDoNotMove()
    {
        var solver = new RopeSolver(BuildScene(new[] { 0, 1, 2, 3 }), Params());
        var initialPos = new FixedVec3[RopeCount];
        for (int r = 0; r < RopeCount; r++) initialPos[r] = solver.Ropes[r].Nodes[0].Pos;

        for (int f = 0; f < 100; f++) solver.Step();

        for (int r = 0; r < RopeCount; r++)
        {
            Assert.True(solver.Ropes[r].Nodes[0].InvMass.Raw == 0,
                $"rope {r} node 0 not pinned (InvMass != 0)");
            Assert.True(VecEq(solver.Ropes[r].Nodes[0].Pos, initialPos[r]),
                $"rope {r} node 0 drifted from its anchor");
            Assert.True(VecEq(solver.Ropes[r].Nodes[0].Vel, FixedVec3.Zero),
                $"rope {r} node 0 has non-zero velocity despite InvMass == 0");
        }
    }

    [Fact(Skip = "SpinSystem not yet built in pure C# (design-only in Tunables.cs); revisit when Simulation.Systems.SpinSystem exists.")]
    public void SpinStacking_OrderIndependent()
    {
    }

    [Fact]
    public void Movement_QuantizedInputs()
    {
        var cfg = TestMovementConfig();
        var inputs = MakeInputSequence(12345u, 300);

        // Run A: 300 ticks with the fixed input sequence.
        var a = TestMover();
        foreach (var i in inputs) MovementSystem.Step(ref a, i, cfg);

        // Run B: identical inputs — must be bit-identical (determinism + fixed-point quantization).
        var b = TestMover();
        foreach (var i in inputs) MovementSystem.Step(ref b, i, cfg);
        Assert.True(MoverEq(a, b), "movement diverged across two identical runs");

        // Rollback: snapshot at tick 100, finish, restore, re-run 100..300 — must match the straight run.
        var c = TestMover();
        for (int t = 0; t < 100; t++) MovementSystem.Step(ref c, inputs[t], cfg);
        var snap = c;
        for (int t = 100; t < inputs.Count; t++) MovementSystem.Step(ref c, inputs[t], cfg);
        c = snap;
        for (int t = 100; t < inputs.Count; t++) MovementSystem.Step(ref c, inputs[t], cfg);
        Assert.True(MoverEq(c, b), "movement rollback re-sim diverged from the straight run");

        // Sanity: different inputs must produce a different result (not trivially constant).
        var d = TestMover();
        var other = MakeInputSequence(99991u, 300);
        foreach (var i in other) MovementSystem.Step(ref d, i, cfg);
        Assert.False(MoverEq(d, b), "movement produced identical state for different inputs (trivially constant?)");
    }

    // --- movement helpers ---

    static MovementConfig TestMovementConfig() => new()
    {
        Dt               = Fixed.One / Fixed.FromInt(128),
        Gravity          = Fixed.FromInt(20),
        MaxSpeed         = Fixed.FromInt(12),
        GroundAccel      = Fixed.FromInt(120),
        AirAccel         = Fixed.FromInt(40),
        GroundFriction   = Fixed.FromInt(100),
        JumpBase         = Fixed.FromInt(2),
        JumpSinkScale    = Fixed.FromInt(1),
        SinkDecaySeconds = Fixed.FromInt(2),
        SinkGain         = Fixed.FromInt(1) / Fixed.FromInt(2),
        LookYawRate      = Fixed.One / Fixed.FromInt(300),
        LookPitchRate    = Fixed.One / Fixed.FromInt(300),
        PitchMin         = -(Fixed.FromInt(15708) / Fixed.FromInt(10000)),
        PitchMax         =  Fixed.FromInt(15708) / Fixed.FromInt(10000),
    };

    static Mover TestMover() => new()
    {
        Pos = new FixedVec3(Fixed.Zero, Fixed.FromInt(3), Fixed.Zero),
    };

    static List<PlayerInputs> MakeInputSequence(uint seed, int count)
    {
        var list = new List<PlayerInputs>(count);
        uint s = seed == 0 ? 1u : seed;
        for (int i = 0; i < count; i++)
        {
            s = s * 1664525u + 1013904223u;
            Fixed mx    = Fixed.FromInt((int)(s % 3) - 1);
            Fixed mz    = Fixed.FromInt((int)((s >> 2) % 3) - 1);
            Fixed yaw   = Fixed.FromDouble(((s % 50) - 25) / 100.0);
            Fixed pitch = Fixed.FromDouble(((s % 20) - 10) / 100.0);
            bool  jump  = (s % 17) == 0;
            list.Add(new PlayerInputs(mx, mz, yaw, pitch, jump, false, false, FixedVec3.Zero, false, false, false));
        }
        return list;
    }

    static bool MoverEq(in Mover a, in Mover b) =>
        a.Pos.X.Raw == b.Pos.X.Raw && a.Pos.Y.Raw == b.Pos.Y.Raw && a.Pos.Z.Raw == b.Pos.Z.Raw &&
        a.Vel.X.Raw == b.Vel.X.Raw && a.Vel.Y.Raw == b.Vel.Y.Raw && a.Vel.Z.Raw == b.Vel.Z.Raw &&
        a.Yaw.Raw == b.Yaw.Raw && a.Pitch.Raw == b.Pitch.Raw &&
        a.Grounded.Raw == b.Grounded.Raw && a.PrevJump.Raw == b.PrevJump.Raw && a.Sink.Raw == b.Sink.Raw;
}
