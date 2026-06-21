// movement.md §5 — Unity-side input poller (the float -> FP boundary).
//
// Feeds keyboard/mouse (new Input System) into the deterministic sim each tick via CallbackPollInput.
// Self-contained: reads devices directly (Keyboard.current / Mouse.current), so it does not depend on
// the InputSystem_Actions asset bindings and works in any scene. Controls: WASD = move, mouse = look,
// Space = jump.
//
// DETERMINISM NOTE: this is the ONE place float enters the pipeline (Unity input -> sim). FP.FromFloat_UNSAFE
// is used here deliberately and ONLY here — it is forbidden inside the sim step. The sim receives pure FP.

namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class MoverInputPoller : MonoBehaviour
    {
        void OnEnable()
        {
            QuantumCallback.Subscribe(this, (CallbackPollInput c) => Poll(c));
        }

        void Poll(CallbackPollInput c)
        {
            var kbd = Keyboard.current;
            var mouse = Mouse.current;
            if (kbd == null || mouse == null)
            {
                c.SetInput(default, DeterministicInputFlags.Repeatable);
                return;
            }

            FP strafe = (kbd.dKey.isPressed ? FP._1 : FP._0) - (kbd.aKey.isPressed ? FP._1 : FP._0);
            FP forward = (kbd.wKey.isPressed ? FP._1 : FP._0) - (kbd.sKey.isPressed ? FP._1 : FP._0);

            Vector2 md = mouse.delta.ReadValue();          // per-frame mouse delta, pixels
            // Raw per-poll mouse delta (pixels); MovementConfig.LookYawRate/PitchRate convert to radians.
            FP lookX = FP.FromFloat_UNSAFE(md.x);
            FP lookY = FP.FromFloat_UNSAFE(md.y);

            FP jump = kbd.spaceKey.isPressed ? FP._1 : FP._0;
            FP grapple = mouse.rightButton.isPressed ? FP._1 : FP._0;   // HOOK: replace with the in-progress button

            // Aim point = where the screen-center reticle (camera forward) hits the world (room/pieces),
            // so the grapple lands on the cursor. Float->FP here at the input boundary only.
            Vector3 aimPt = Vector3.zero;
            var cam = Camera.main;
            if (cam != null)
            {
                Ray r = cam.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                aimPt = Physics.Raycast(r, out var hit, 60f) ? hit.point : (r.origin + r.direction * 30f);
            }

            var i = new Quantum.Input
            {
                Move = new FPVector2(strafe, forward),
                Look = new FPVector2(lookX, lookY),
                Jump = jump,
                Grapple = grapple,
                AimPoint = new FPVector3(FP.FromFloat_UNSAFE(aimPt.x), FP.FromFloat_UNSAFE(aimPt.y), FP.FromFloat_UNSAFE(aimPt.z)),
            };
            c.SetInput(i, DeterministicInputFlags.Repeatable);
        }
    }
}
