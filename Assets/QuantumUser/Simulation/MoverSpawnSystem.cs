// movement.md §5.1 — count-agnostic player spawn.
//
// Creates one Mover entity per player slot — never player1/player2 (CONVENTIONS, Decision 3). Each
// Mover carries a stable PlayerIndex linking it to that player's polled input (wired in the Movement
// step). Position lives on the built-in Transform3D at a spawn point; velocity/look start zeroed.
// OnInit runs once at startup, before any Update system, so movers exist by frame 0 — same ordering
// rationale as RopeSpawnSystem. Count-agnostic: iterates f.PlayerCount, spawns N.

namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class MoverSpawnSystem : SystemSignalsOnly
    {
        // Fixed spawn-point table (covers the 1v1..4-player design target). Avoids trig in spawn;
        // deterministic, count-agnostic cycling via modulo.
        static readonly FPVector3[] Spawns =
        {
            new FPVector3((FP)0, (FP)3, (FP)0),
            new FPVector3((FP)5, (FP)3, (FP)0),
            new FPVector3((FP)0, (FP)3, (FP)5),
            new FPVector3(-(FP)5, (FP)3, (FP)0),
        };

        public override void OnInit(Frame f)
        {
            EnsureConfig(f);
            int n = f.PlayerCount;                         // CONFIRM: Frame.PlayerCount (count-agnostic N)
            for (int p = 0; p < n; p++) SpawnMover(f, p);
        }

        static void EnsureConfig(Frame f)
        {
            // Self-contained defaults if the scene authored no MovementConfig, so the sim runs standalone.
            // Real tuning is done in a scene asset; these are deterministic integer-derived placeholders.
            if (f.Unsafe.TryGetPointerSingleton<MovementConfig>(out _)) return;  // CONFIRM: TryGetPointerSingleton<T>
            EntityRef e = f.Create();
            f.Add<MovementConfig>(e);
            var cfg = f.Unsafe.GetPointer<MovementConfig>(e);

            cfg->Gravity        = (FP)20;
            cfg->MaxSpeed       = (FP)12;
            cfg->GroundAccel    = (FP)120;
            cfg->AirAccel       = (FP)40;
            cfg->GroundFriction = (FP)100;
            cfg->JumpBase       = (FP)2;                     // weak base jump (no sink) — chain jumps to build speed
            cfg->JumpSinkScale  = (FP)1;                     // extra impulse per unit of sink
            cfg->SinkDecaySeconds = (FP)2;                   // N — sink decays over this many seconds
            cfg->SinkGain       = (FP)1 / (FP)2;             // 0.5 — sink gained per unit of impact speed
            cfg->GrappleRestFactor = (FP)1 / (FP)2;          // 0.5 — rope spawns stretched, reels player in
            cfg->GrapplePlayerMass = (FP)5;                  // effective player mass for grapple coupling
            cfg->GrappleMaxRange   = (FP)30;
            cfg->LookYawRate    = (FP)1 / (FP)300;           // ~0.0033 rad per pixel of mouse delta (tune for feel)
            cfg->LookPitchRate  = (FP)1 / (FP)300;
            cfg->PitchMin       = -((FP)15708 / (FP)10000);  // ~-pi/2
            cfg->PitchMax       =  (FP)15708 / (FP)10000;    // ~+pi/2
        }

        static void SpawnMover(Frame f, int p)
        {
            EntityRef e = f.Create();
            f.Add<Transform3D>(e);                    // CONFIRM: built-in Transform3D add + Position set
            f.Add<Mover>(e);

            var t = f.Unsafe.GetPointer<Transform3D>(e);
            t->Position = Spawns[p % Spawns.Length];

            var m = f.Unsafe.GetPointer<Mover>(e);
            m->PlayerIndex = p;                       // stable link to this player's input (count-agnostic)
            // Velocity / Yaw / Pitch / Grounded default to 0.
        }
    }
}
