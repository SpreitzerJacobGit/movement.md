// Photon Quantum 3.0.11 bootstrap for the First Test (Stage 2).
//
// Creates the deterministic scene that RopeSolverSystem operates on — a direct port of
// DeterminismHarness.BuildScene() + Params() from the local spike: the RopeSolverConfig singleton
// plus 4 rope entities of 8 nodes each (ring anchors, inward lean, node 0 pinned). Same numbers =>
// the Quantum run reproduces the local scene, so the five aspects test the identical setup.
// All API calls verified by reflection against this project's Quantum DLLs (3.0.11).
//
// In Quantum a list<RopeNode> is a QListPtr that must be EXPLICITLY allocated and filled — entities
// and their node lists are not auto-created. Without this system, RopeSolverSystem has nothing to
// iterate.
//
// Registration: added in SystemSetup.User.cs BEFORE RopeSolverSystem. OnInit runs once at startup,
// before frame-0 Updates, so the entities exist by the time RopeSolverSystem first runs. Fractions
// are built by integer division (e.g. (FP)2/(FP)5) to stay in fixed-point; never FP.FromFloat
// (non-deterministic across platforms).

using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
    public unsafe class RopeSpawnSystem : SystemSignalsOnly
    {
        const int RopeCount = 4;
        const int NodesPerRope = 8;

        public override void OnInit(Frame f)
        {
            SpawnConfig(f);
            for (int r = 0; r < RopeCount; r++) SpawnRope(f, r);
            Log.Error($"[RopeSpike] RopeSpawnSystem.OnInit: spawned {RopeCount} ropes + config singleton.");  // DIAGNOSTIC (Error severity to bypass editor log filter) — remove after #5
        }

        static void SpawnConfig(Frame f)
        {
            // Singleton tuning values — identical to the local spike's Params().
            EntityRef e = f.Create();
            f.Add<RopeSolverConfig>(e);
            var cfg = f.Unsafe.GetPointer<RopeSolverConfig>(e);

            cfg->SpringK       = (FP)800;                     // stiff
            cfg->Damping       = (FP)2;
            cfg->SegmentRest   = FP._0_50;                    // 0.5
            cfg->Gravity       = (FP)20;
            cfg->CollisionDist = (FP)2 / (FP)5;               // 0.4
            cfg->CollisionK    = (FP)600;                     // stiff penalty
            // Dt is NOT set here — it comes from SessionConfig.UpdateFPS (locked to 128 Hz).
        }

        static void SpawnRope(Frame f, int r)
        {
            EntityRef e = f.Create();
            f.Add<Rope>(e);
            var rope = f.Unsafe.GetPointer<Rope>(e);
            rope->Id = r;                                     // stable Id => drives Id-sorted collision order

            rope->Nodes = f.AllocateList<RopeNode>(NodesPerRope);
            var nodes = f.ResolveList(rope->Nodes);

            // Anchors on a 2-unit ring at the four sides; segment hangs in -Y with inward X/Z lean.
            FP ax = (FP)(r == 0 ? 2 : r == 1 ? -2 : 0);
            FP az = (FP)(r == 2 ? 2 : r == 3 ? -2 : 0);
            FP lean = (FP)18 / (FP)100;                        // 0.18
            FP leanX = -ax * lean;
            FP leanZ = -az * lean;

            for (int i = 0; i < NodesPerRope; i++)
            {
                FP fi = (FP)i;
                var node = new RopeNode
                {
                    Pos = new FPVector3(ax + leanX * fi, -fi * FP._0_50, az + leanZ * fi),
                    Vel = FPVector3.Zero,
                    InvMass = i == 0 ? FP._0 : FP._1,          // node 0 pinned (immovable anchor)
                    Force = FPVector3.Zero,
                };
                nodes.Add(node);
            }
        }
    }
}
