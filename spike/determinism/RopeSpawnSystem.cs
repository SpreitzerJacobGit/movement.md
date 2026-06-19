// Photon Quantum 3.0.x bootstrap for the First Test (Stage 2) — DRAFT, staged before SDK import.
//
// Creates the deterministic scene that RopeSolverSystem operates on. This is a direct port of
// DeterminismHarness.BuildScene() + Params() from the local spike: the RopeSolverConfig singleton
// plus 4 rope entities of 8 nodes each (ring anchors, inward lean, node 0 pinned). Same numbers =>
// the Quantum run reproduces the local scene, so the 5 aspects test the identical setup.
//
// WHY THIS EXISTS: in Quantum a `list<RopeNode>` is a QListPtr that must be EXPLICITLY allocated
// and filled — entities and their node lists are not auto-created. Without this system,
// RopeSolverSystem has nothing to iterate.
//
// HEAVIER CONFIRM SURFACE: entity-create, component-add, and list-allocate/-add are the calls
// Quantum renamed most across the 3.0.x line. Reconcile every `CONFIRM:` against the installed SDK
// on first compile. The VALUES and ORDERING are correct and proven; only the API spelling may move.
//
// Registration: put this BEFORE RopeSolverSystem in the SystemsConfig asset. OnInit runs once at
// startup, before frame-0 Updates, so the entities exist by the time RopeSolverSystem first runs.

using Photon.Deterministic;
using Quantum.Collections;   // QList<T>            CONFIRM: namespace for QList in your SDK build

namespace Quantum
{
    public unsafe class RopeSpawnSystem : SystemSignalsOnly   // CONFIRM: base type exposing OnInit
    {
        const int RopeCount = 4;
        const int NodesPerRope = 8;

        public override void OnInit(Frame f)
        {
            SpawnConfig(f);
            for (int r = 0; r < RopeCount; r++) SpawnRope(f, r);
        }

        static void SpawnConfig(Frame f)
        {
            // Singleton tuning values — identical to the local spike's Params().
            // Fractions are built by integer division (e.g. 2/5) to stay in fixed-point; never
            // FP.FromFloat (non-deterministic across platforms).
            EntityRef e = f.Create();                         // CONFIRM: Frame.Create()
            f.Add<RopeSolverConfig>(e);                       // CONFIRM: Frame.Add<T>(EntityRef)
            var cfg = f.Unsafe.GetPointer<RopeSolverConfig>(e);

            cfg->SpringK       = (FP)800;                     // stiff
            cfg->Damping       = (FP)2;
            cfg->SegmentRest   = FP._0_50;                    // 0.5
            cfg->Gravity       = (FP)20;
            cfg->CollisionDist = (FP)2 / (FP)5;               // 0.4
            cfg->CollisionK    = (FP)600;                     // stiff penalty
            // Dt is NOT set here — it comes from the SimulationConfig (lock it to 128 Hz).

            // CONFIRM: if your SDK prefers explicit singleton assignment, use
            //   f.SetSingleton(*cfg);   // instead of / in addition to the Add above
        }

        static void SpawnRope(Frame f, int r)
        {
            EntityRef e = f.Create();
            f.Add<Rope>(e);
            var rope = f.Unsafe.GetPointer<Rope>(e);
            rope->Id = r;                                     // stable Id => drives Id-sorted collision order

            rope->Nodes = f.AllocateList<RopeNode>(NodesPerRope);  // CONFIRM: AllocateList capacity arg
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
                nodes.Add(node);                              // CONFIRM: QList<T>.Add(T)
            }
        }
    }
}
