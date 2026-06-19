// movement.md §5 — MovementSystem v4 (real ground raycast).
//
// Count-agnostic. Ground handling: the previous tick's Grounded state drives grounded accel/friction
// + grounded jump; a ground pass at the end probes downward and snaps to the floor, recomputing
// Grounded for next tick. Pure function of input + fixed dt => deterministic.
//
// v4 GROUND = real Physics3D raycast (Option C) against scene static colliders, with an analytical
// Y=0 fallback when no collider is hit (so the spike stays playable before level geometry is added).
// When real geometry is baked into the map, the raycast takes over automatically.

namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class MovementSystem : SystemMainThread
    {
        // Ground-probe tuning (placeholder constants; move to MovementConfig when tuning for real).
        static readonly FP GroundProbeSkin = (FP)1 / (FP)10;   // 0.1 — offset the origin off the floor so the ray hits
        static readonly FP GroundProbeDist = (FP)1 / (FP)2;    // 0.5 — how far below the feet to look for ground
        static readonly FP GrapplePlayerMass = (FP)5;          // effective player mass for grapple coupling (tune)

        // Look direction from yaw/pitch (unit vector; matches the grapple aim direction).
        static FPVector3 AimDir(FP yaw, FP pitch)
        {
            FP cp = FPMath.Cos(pitch);
            return new FPVector3(cp * FPMath.Sin(yaw), FPMath.Sin(pitch), cp * FPMath.Cos(yaw));
        }

        public override void Update(Frame f)
        {
            var cfg = f.GetSingleton<MovementConfig>();
            FP dt = f.DeltaTime;

            // Pre-pass: collect each player's grapple pull (chain force stashed by last tick's solver).
            // One rope per player max; stackalloc is zero-initialized.
            FPVector3* grappleForce = stackalloc FPVector3[8];
            var rit = f.Filter<Rope>();
            while (rit.NextUnsafe(out EntityRef re, out Rope* gr))
                if (gr->AttachedPlayer >= 0 && gr->AttachedPlayer < 8)
                    grappleForce[gr->AttachedPlayer] = gr->PlayerForce;

            var it = f.Filter<Mover>();
            while (it.NextUnsafe(out EntityRef e, out Mover* m))
            {
                var input = f.GetPlayerInput((PlayerRef)m->PlayerIndex);
                var t = f.Unsafe.GetPointer<Transform3D>(e);
                bool grounded = m->Grounded > FP._0;          // last tick's resolved state

                // --- Look (yaw + pitch). ---
                m->Yaw   += input->Look.X * cfg.LookYawRate;
                m->Pitch  = FPMath.Clamp(m->Pitch + input->Look.Y * cfg.LookPitchRate, cfg.PitchMin, cfg.PitchMax);

                // --- Horizontal movement: accel toward the yaw-relative wish dir (grounded vs air). ---
                FP sinY = FPMath.Sin(m->Yaw);
                FP cosY = FPMath.Cos(m->Yaw);
                FPVector3 fwd   = new FPVector3( sinY, FP._0,  cosY);
                FPVector3 right = new FPVector3( cosY, FP._0, -sinY);
                FPVector3 wishDir = (right * input->Move.X) + (fwd * input->Move.Y);
                FP wishMag = wishDir.Magnitude;
                if (wishMag > FP._1) wishDir = wishDir * (FP._1 / wishMag);

                FP accel;
                FPVector3 wishVel;
                if (grounded)
                {
                    bool hasInput = wishMag > FP._0;
                    accel   = hasInput ? cfg.GroundAccel : cfg.GroundFriction;
                    wishVel = hasInput ? wishDir * cfg.MaxSpeed : FPVector3.Zero;
                }
                else
                {
                    accel   = cfg.AirAccel;
                    wishVel = wishDir * cfg.MaxSpeed;
                }

                FPVector3 horiz = new FPVector3(m->Velocity.X, FP._0, m->Velocity.Z);
                FPVector3 diff  = wishVel - horiz;
                FP maxStep = accel * dt;
                FP diffMag = diff.Magnitude;
                if (diffMag > maxStep && diffMag > FP._0) diff = diff * (maxStep / diffMag);
                horiz += diff;
                m->Velocity.X = horiz.X;
                m->Velocity.Z = horiz.Z;

                // --- Sink decays each tick; jump fires toward AIM dir, scaled by sink (weak without it). ---
                m->Sink -= m->Sink * (dt / cfg.SinkDecaySeconds);
                if (m->Sink < FP._0) m->Sink = FP._0;

                if (input->Jump > FP._0 && m->PrevJump == FP._0 && grounded)
                {
                    FPVector3 aim = AimDir(m->Yaw, m->Pitch);
                    FP force = cfg.JumpBase + m->Sink * cfg.JumpSinkScale;
                    m->Velocity += aim * force;
                }
                m->PrevJump = input->Jump;

                // --- Grapple pull (last tick's solver) + gravity + integrate. ---
                if (m->PlayerIndex >= 0 && m->PlayerIndex < 8)
                    m->Velocity += grappleForce[m->PlayerIndex] * (dt / GrapplePlayerMass);
                m->Velocity.Y -= cfg.Gravity * dt;
                t->Position += m->Velocity * dt;

                // --- Ground pass (v4: real Physics3D raycast; analytical Y=0 fallback). ---
                // Option C: probe downward for a real static/dynamic collider. Falls back to an analytical
                // Y=0 plane when nothing is hit, so the spike is playable before level geometry is added.
                // ComputeDetailedInfo is required to populate hit.Point. CONFIRM: Raycast returns nullable Hit3D.
                FPVector3 skin = new FPVector3(FP._0, GroundProbeSkin, FP._0);
                FPVector3 down = new FPVector3(FP._0, -(FP)1, FP._0);
                var hit = f.Physics3D.Raycast(t->Position + skin, down, GroundProbeDist + GroundProbeSkin, -1,
                                              QueryOptions.HitAll | QueryOptions.ComputeDetailedInfo);
                FP groundY = (hit != null) ? hit.Value.Point.Y : FP._0;   // real collider, else analytical Y=0

                if (t->Position.Y <= groundY + GroundProbeSkin)
                {
                    // Landing impact builds sink (proportional to impact speed) — fuels the next jump.
                    FP gained = m->Velocity.Magnitude * cfg.SinkGain;
                    if (gained > m->Sink) m->Sink = gained;
                    t->Position.Y = groundY;
                    if (m->Velocity.Y < FP._0) m->Velocity.Y = FP._0;
                    m->Grounded = FP._1;
                }
                else
                {
                    m->Grounded = FP._0;
                }
            }
        }
    }
}
