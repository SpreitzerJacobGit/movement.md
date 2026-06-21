// Photon Quantum 3.0.11 — creates the RopeSolverConfig singleton at init.
//
// The 4 standalone First-Test ropes are retired (the test is complete and recorded in
// spike/determinism/FIRST_TEST.md). Grapple ropes are created on demand by GrappleSystem; this system
// now only ensures the solver tuning singleton exists. API calls verified by reflection (3.0.11).

using Photon.Deterministic;

namespace Quantum
{
    public unsafe class RopeSpawnSystem : SystemSignalsOnly
    {
        public override void OnInit(Frame f)
        {
            SpawnConfig(f);
        }

        static void SpawnConfig(Frame f)
        {
            if (f.Unsafe.TryGetPointerSingleton<RopeSolverConfig>(out _)) return;
            EntityRef e = f.Create();
            f.Add<RopeSolverConfig>(e);
            var cfg = f.Unsafe.GetPointer<RopeSolverConfig>(e);

            cfg->SpringK       = (FP)800;                     // stiff rubber-band
            cfg->Damping       = (FP)2;
            cfg->SegmentRest   = FP._0_50;                    // default (grapple ropes override per-rope)
            cfg->Gravity       = (FP)20;
            cfg->CollisionDist = (FP)2 / (FP)5;               // 0.4
            cfg->CollisionK    = (FP)600;
        }
    }
}
