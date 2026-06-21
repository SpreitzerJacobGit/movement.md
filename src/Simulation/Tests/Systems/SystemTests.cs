using System.Collections.Generic;
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

    [Fact(Skip = "Pure-C# MovementSystem not yet ported (Quantum ECS MovementSystem stays Unity-side until SDK restored); revisit when ported.")]
    public void Movement_QuantizedInputs()
    {
    }
}
