// movement.md §5 sandbox — live tuning tool for the sink/jump mechanic + grapple stiffness.
//
// Select this component during Play (or add it in the scene at edit time) and adjust the fields in the
// inspector — values are pushed into the sim's config singletons every frame, so changes apply LIVE.
// SANDBOX ONLY: writing sim config from Unity is not netcode-safe (no rollback); fine for local tuning.

namespace Quantum
{
    using UnityEngine;
    using Photon.Deterministic;

    public class SandboxTuner : MonoBehaviour
    {
        [Header("Sink / Jump")]
        public float SinkDecaySeconds = 2f;     // N: how long sink lasts after a hard/fast landing
        public float SinkGain        = 0.5f;    // sink gained per unit of impact speed on landing
        public float JumpBase        = 2f;      // weak base jump impulse (no sink) — chain jumps to build speed
        public float JumpSinkScale   = 1f;      // extra impulse per unit of sink

        [Header("Grapple")]
        public float GrappleSpringK  = 800f;    // rope spring stiffness (stiff rubber-band)

        void Update()
        {
            var f = QuantumRunner.Default?.Game?.Frames?.Predicted;
            if (f == null) return;

            var mc = f.GetSingleton<MovementConfig>();
            mc.SinkDecaySeconds = FP.FromFloat_UNSAFE(SinkDecaySeconds);
            mc.SinkGain         = FP.FromFloat_UNSAFE(SinkGain);
            mc.JumpBase         = FP.FromFloat_UNSAFE(JumpBase);
            mc.JumpSinkScale    = FP.FromFloat_UNSAFE(JumpSinkScale);
            f.SetSingleton(mc, f.GetSingletonEntityRef<MovementConfig>());

            var rc = f.GetSingleton<RopeSolverConfig>();
            rc.SpringK = FP.FromFloat_UNSAFE(GrappleSpringK);
            f.SetSingleton(rc, f.GetSingletonEntityRef<RopeSolverConfig>());
        }
    }
}
