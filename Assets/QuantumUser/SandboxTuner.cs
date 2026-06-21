// movement.md §5 sandbox — live tuning tool for jump / grapple / air-control.
//
// Select this component during Play and adjust fields in the inspector — values are pushed into the
// sim's config singletons every frame, so changes apply LIVE.
// SANDBOX ONLY: writing sim config from Unity is not netcode-safe (no rollback); fine for local tuning.

namespace Quantum
{
    using UnityEngine;
    using Photon.Deterministic;

    public class SandboxTuner : MonoBehaviour
    {
        [Header("Jump / Sink")]
        public float JumpBase         = 2f;      // weak base jump impulse (no sink) — chain jumps to build speed
        public float JumpSinkScale    = 1f;      // extra impulse per unit of sink
        public float SinkGain         = 0.5f;    // sink gained per unit of impact speed on landing
        public float SinkDecaySeconds = 2f;      // N: how long sink lasts after a hard/fast landing

        [Header("Air Control")]
        public float MaxSpeed        = 12f;      // horizontal speed cap
        public float GroundAccel     = 120f;     // grounded accel toward the wish direction
        public float AirAccel        = 40f;      // airborne accel (air control)
        public float GroundFriction  = 100f;     // grounded decel when no input
        public float Gravity         = 20f;

        [Header("Grapple")]
        public float GrappleSpringK    = 800f;   // rope spring stiffness (stiff rubber-band)
        public float GrappleRestFactor = 0.5f;   // <1 => rope spawns stretched, reels player in
        public float GrapplePlayerMass = 5f;     // effective player mass for grapple coupling
        public float GrappleMaxRange   = 30f;    // max grapple distance

        void Update()
        {
            var f = QuantumRunner.Default?.Game?.Frames?.Predicted;
            if (f == null) return;

            var mc = f.GetSingleton<MovementConfig>();
            mc.JumpBase          = FP.FromFloat_UNSAFE(JumpBase);
            mc.JumpSinkScale     = FP.FromFloat_UNSAFE(JumpSinkScale);
            mc.SinkGain          = FP.FromFloat_UNSAFE(SinkGain);
            mc.SinkDecaySeconds  = FP.FromFloat_UNSAFE(SinkDecaySeconds);
            mc.MaxSpeed          = FP.FromFloat_UNSAFE(MaxSpeed);
            mc.GroundAccel       = FP.FromFloat_UNSAFE(GroundAccel);
            mc.AirAccel          = FP.FromFloat_UNSAFE(AirAccel);
            mc.GroundFriction    = FP.FromFloat_UNSAFE(GroundFriction);
            mc.Gravity           = FP.FromFloat_UNSAFE(Gravity);
            mc.GrappleRestFactor = FP.FromFloat_UNSAFE(GrappleRestFactor);
            mc.GrapplePlayerMass = FP.FromFloat_UNSAFE(GrapplePlayerMass);
            mc.GrappleMaxRange   = FP.FromFloat_UNSAFE(GrappleMaxRange);
            f.SetSingleton(mc, f.GetSingletonEntityRef<MovementConfig>());

            var rc = f.GetSingleton<RopeSolverConfig>();
            rc.SpringK = FP.FromFloat_UNSAFE(GrappleSpringK);
            f.SetSingleton(rc, f.GetSingletonEntityRef<RopeSolverConfig>());
        }
    }
}
