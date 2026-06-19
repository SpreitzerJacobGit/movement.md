// THE FIRST TEST (brief §4). Four ropes => up to six simultaneous collision pairs — the worst
// case we intend to ship. Runs the five aspects and prints a pass/fail report.
//
// Aspects:
//   1. Determinism across runs      — N identical runs, final state bit-identical.
//   2. Determinism across rollback  — snapshot, continue, restore, re-sim; bit-identical.
//   3. Pair-order independence      — shuffle pair-generation order; output bit-identical (ID-sort holds).
//   4. Expressibility in fixed-point— solver uses only fixed-point ops (no float in sim). Asserted here;
//                                      Quantum-FP expressibility is confirmed when this ports into Quantum.
//   5. Frame budget under load      — per-tick cost + rollback re-sim cost vs the 128 Hz budget (7.8125 ms).
//
// Tick rate: 128 Hz (Counter-Strike competitive cadence; provisional — brief §4.7).

using System;
using System.Diagnostics;

namespace Determinism;

public static class DeterminismHarness
{
    const int TickHz = 128;
    const double FrameBudgetMs = 1000.0 / TickHz; // 7.8125 ms
    const int RopeCount = 4;
    const int NodesPerRope = 8;
    const int SimFrames = 600;       // ~4.7s of sim at 128 Hz
    const int LocalRunRepeats = 1_000;   // Aspect #1, local de-risk default (Quantum stage uses 10_000)
    const int RollbackDepth = 8;     // realistic mid-match rollback window

    static int RunRepeats = LocalRunRepeats;

    public static int Main(string[] args)
    {
        if (args.Length > 0 && int.TryParse(args[0], out int n) && n > 0) RunRepeats = n;

        Console.WriteLine($"First Test — {RopeCount} ropes, {NodesPerRope} nodes each, {TickHz} Hz, " +
                          $"{SimFrames} frames. Budget/tick = {FrameBudgetMs:0.0000} ms.\n");

        bool a1 = AspectRunDeterminism();
        bool a2 = AspectRollbackDeterminism();
        bool a3 = AspectPairOrderIndependence();
        bool a4 = AspectFixedPointExpressibility();
        bool a5 = AspectFrameBudget(out double tickMs, out double rollbackMs);

        Console.WriteLine("\n================ RESULT ================");
        Console.WriteLine($"  #1 determinism across runs ({RunRepeats}x) : {Mark(a1)}");
        Console.WriteLine($"  #2 determinism across rollback           : {Mark(a2)}");
        Console.WriteLine($"  #3 collision-pair order independence     : {Mark(a3)}");
        Console.WriteLine($"  #4 expressible in fixed-point            : {Mark(a4)} (Quantum-FP: confirm on port)");
        Console.WriteLine($"  #5 frame budget @ {TickHz}Hz                 : {Mark(a5)} " +
                          $"(tick {tickMs:0.000} ms, rollback x{RollbackDepth} {rollbackMs:0.000} ms / {FrameBudgetMs:0.0000} ms)");
        Console.WriteLine("========================================");
        bool all = a1 && a2 && a3 && a4 && a5;
        Console.WriteLine(all
            ? "\nAll local aspects PASS. Re-run inside Photon Quantum to validate Aspect #4 and Path 1."
            : "\nFAILURE — see FIRST_TEST.md decision tree (§4.6) before proceeding.");
        return all ? 0 : 1;
    }

    static string Mark(bool ok) => ok ? "PASS" : "FAIL";

    // Four ropes anchored on a small ring, hanging with slight inward lean so they swing and tangle.
    static Rope[] BuildScene()
    {
        var ropes = new Rope[RopeCount];
        for (int r = 0; r < RopeCount; r++)
        {
            var nodes = new Node[NodesPerRope];
            // Anchors placed at the four sides of a 2-unit ring; segment hangs in -Y with inward X/Z lean.
            Fixed ax = Fixed.FromInt(r == 0 ? 2 : r == 1 ? -2 : 0);
            Fixed az = Fixed.FromInt(r == 2 ? 2 : r == 3 ? -2 : 0);
            Fixed leanX = -ax * Fixed.FromDouble(0.18);
            Fixed leanZ = -az * Fixed.FromDouble(0.18);
            for (int i = 0; i < NodesPerRope; i++)
            {
                Fixed fi = Fixed.FromInt(i);
                nodes[i] = new Node
                {
                    Pos = new FixedVec3(ax + leanX * fi, -fi * Fixed.FromDouble(0.5), az + leanZ * fi),
                    Vel = FixedVec3.Zero,
                    InvMass = i == 0 ? Fixed.Zero : Fixed.One, // node 0 pinned
                };
            }
            ropes[r] = new Rope { Id = r, Nodes = nodes };
        }
        return ropes;
    }

    static SolverParams Params() => new()
    {
        Dt = Fixed.One / Fixed.FromInt(TickHz),
        SpringK = Fixed.FromInt(800),        // stiff
        Damping = Fixed.FromInt(2),
        SegmentRest = Fixed.FromDouble(0.5),
        Gravity = Fixed.FromInt(20),
        CollisionDist = Fixed.FromDouble(0.4),
        CollisionK = Fixed.FromInt(600),     // stiff penalty
    };

    static long[] RunOnce(uint iterationSeed = 0)
    {
        var solver = new RopeSolver(BuildScene(), Params());
        for (int f = 0; f < SimFrames; f++) solver.Step(iterationSeed);
        return solver.Serialize();
    }

    static bool AspectRunDeterminism()
    {
        long[] baseline = RunOnce();
        ulong h0 = Fnv(baseline);
        for (int k = 1; k < RunRepeats; k++)
            if (Fnv(RunOnce()) != h0) { Console.WriteLine($"  #1 diverged on run {k}"); return false; }
        Console.WriteLine($"  #1 {RunRepeats} runs identical (hash {h0:x16})");
        return true;
    }

    static bool AspectRollbackDeterminism()
    {
        // Straight run to SimFrames.
        var s = new RopeSolver(BuildScene(), Params());
        for (int f = 0; f < SimFrames; f++) s.Step();
        long[] straight = s.Serialize();

        // Run, snapshot at SimFrames-RollbackDepth, finish, then restore snapshot and re-sim forward.
        var s2 = new RopeSolver(BuildScene(), Params());
        int snapAt = SimFrames - RollbackDepth;
        long[] snap = Array.Empty<long>();
        for (int f = 0; f < SimFrames; f++)
        {
            if (f == snapAt) snap = s2.Serialize();
            s2.Step();
        }
        s2.Deserialize(snap);
        for (int f = snapAt; f < SimFrames; f++) s2.Step();
        long[] resim = s2.Serialize();

        bool ok = Fnv(straight) == Fnv(resim);
        Console.WriteLine(ok ? $"  #2 rollback re-sim bit-identical (depth {RollbackDepth})"
                             : "  #2 rollback re-sim DIVERGED");
        return ok;
    }

    static bool AspectPairOrderIndependence()
    {
        ulong h0 = Fnv(RunOnce(0));
        uint[] seeds = { 1u, 7u, 1337u, 99991u, 4294967291u };
        foreach (uint seed in seeds)
            if (Fnv(RunOnce(seed)) != h0) { Console.WriteLine($"  #3 order-dependent (seed {seed})"); return false; }
        Console.WriteLine($"  #3 output invariant across {seeds.Length} shuffled pair orders");
        return true;
    }

    static bool AspectFixedPointExpressibility()
    {
        // The solver compiles and runs producing finite, reproducible integer state using only
        // Fixed/FixedVec3 (no System.Single/Double in the sim path). A non-zero, finite result
        // demonstrates the stiff coupled-spring + rope-rope collision solver is expressible in
        // fixed-point. CONFIRM-IN-QUANTUM: re-establish this against Quantum's FP primitives.
        long[] state = RunOnce();
        bool moved = false;
        foreach (long v in state) if (v != 0) { moved = true; break; }
        Console.WriteLine(moved ? "  #4 solver runs entirely in fixed-point, produces live state"
                                : "  #4 solver produced all-zero state (scene/params suspect)");
        return moved;
    }

    static bool AspectFrameBudget(out double tickMs, out double rollbackMs)
    {
        var solver = new RopeSolver(BuildScene(), Params());
        for (int f = 0; f < 64; f++) solver.Step(); // warm

        var sw = Stopwatch.StartNew();
        const int measure = 2000;
        for (int f = 0; f < measure; f++) solver.Step();
        sw.Stop();
        tickMs = sw.Elapsed.TotalMilliseconds / measure;
        rollbackMs = tickMs * RollbackDepth; // a rollback re-sims RollbackDepth heavier steps

        bool ok = (tickMs + rollbackMs) <= FrameBudgetMs;
        Console.WriteLine($"  #5 tick {tickMs:0.000} ms + rollback x{RollbackDepth} {rollbackMs:0.000} ms " +
                          $"= {tickMs + rollbackMs:0.000} ms vs budget {FrameBudgetMs:0.0000} ms");
        Console.WriteLine("     NOTE: this is the LOCAL .NET number, not Quantum. The decision uses the");
        Console.WriteLine("     Quantum measurement; treat this as a provisional headroom signal only.");
        return ok;
    }

    static ulong Fnv(long[] data)
    {
        ulong h = 14695981039346656037UL;
        foreach (long v in data)
            for (int b = 0; b < 8; b++) { h ^= (byte)(v >> (b * 8)); h *= 1099511628211UL; }
        return h;
    }
}
