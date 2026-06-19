// movement.md §5 — GrappleSystem (Option B: real ropes in RopeSolverSystem).
//
// Grapple input -> create/destroy grapple ropes. The anchor is ANY surface: on fire, a ray is cast in
// the player's look direction against the analytical sandbox room; the first surface hit (floor,
// ceiling, or wall) becomes the anchor. (Swap IntersectRoom for f.Physics3D.Raycast later, once the
// sandbox ships real colliders — identical call shape.) The rope spawns PRE-STRETCHED (rest length is
// GrappleRestFactor of the span, <1) so it actively reels the player toward the anchor.
//
// Quantum forbids structural changes during filter iteration, so this gathers first, then mutates.
// The Grapple input field is the HOOK for the in-progress button.

using Photon.Deterministic;

namespace Quantum
{
    public unsafe class GrappleSystem : SystemMainThread
    {
        const int MaxPlayers = 8;
        static readonly FP MaxRange          = (FP)30;
        static readonly FP GrappleRestFactor = (FP)1 / (FP)2;   // <1 => rope spawns stretched, reels player in

        // Analytical sandbox room — interior surfaces the grapple ray can hit.
        static readonly FP RoomH  = (FP)12;     // ceiling height
        static readonly FP RoomHW = (FP)15;     // half-width => walls at +-15

        public override void Update(Frame f)
        {
            // Gather attached rope per player. (stackalloc is zero-init => EntityRef.None)
            EntityRef* attached = stackalloc EntityRef[MaxPlayers];
            var rit = f.Filter<Rope>();
            while (rit.NextUnsafe(out EntityRef re, out Rope* r))
                if (r->AttachedPlayer >= 0 && r->AttachedPlayer < MaxPlayers) attached[r->AttachedPlayer] = re;

            // Gather per-mover input + transform.
            EntityRef* mEntity = stackalloc EntityRef[MaxPlayers];
            int*       mPlayer = stackalloc int[MaxPlayers];
            FP*        mDown   = stackalloc FP[MaxPlayers];
            FP*        mWasDown= stackalloc FP[MaxPlayers];
            FPVector3* mPos    = stackalloc FPVector3[MaxPlayers];
            FP*        mYaw    = stackalloc FP[MaxPlayers];
            FP*        mPitch  = stackalloc FP[MaxPlayers];
            int mCount = 0;
            var mit = f.Filter<Mover>();
            while (mit.NextUnsafe(out EntityRef me, out Mover* m))
            {
                if (mCount >= MaxPlayers) break;
                var input = f.GetPlayerInput((PlayerRef)m->PlayerIndex);
                mEntity[mCount]  = me;
                mPlayer[mCount]  = m->PlayerIndex;
                mDown[mCount]    = input->Grapple;
                mWasDown[mCount] = m->GrappleHeld;
                mPos[mCount]     = f.Unsafe.GetPointer<Transform3D>(me)->Position;
                mYaw[mCount]     = m->Yaw;
                mPitch[mCount]   = m->Pitch;
                mCount++;
            }

            // Act (structural changes are safe here; no filter is iterating).
            for (int i = 0; i < mCount; i++)
            {
                int p = mPlayer[i];
                bool down = mDown[i] > FP._0;
                bool wasDown = mWasDown[i] > FP._0;
                EntityRef att = (p >= 0 && p < MaxPlayers) ? attached[p] : EntityRef.None;
                var m = f.Unsafe.GetPointer<Mover>(mEntity[i]);

                if (down && !wasDown && att == EntityRef.None)
                {
                    FPVector3 aim = AimDir(mYaw[i], mPitch[i]);
                    if (IntersectRoom(mPos[i], aim, MaxRange, out FPVector3 anchor))
                        CreateGrappleRope(f, m, mPos[i], anchor);
                }
                else if (!down && wasDown && att != EntityRef.None)
                {
                    f.Destroy(att);
                    m->GrappleActive = FP._0;
                }

                m->GrappleHeld = mDown[i];
            }
        }

        // Look direction from yaw/pitch (unit vector; consistent with movement's fwd when pitch=0).
        static FPVector3 AimDir(FP yaw, FP pitch)
        {
            FP cp = FPMath.Cos(pitch);
            return new FPVector3(cp * FPMath.Sin(yaw), FPMath.Sin(pitch), cp * FPMath.Cos(yaw));
        }

        // Ray-vs-room: nearest intersection with the 6 interior planes, within range.
        static bool IntersectRoom(FPVector3 o, FPVector3 d, FP range, out FPVector3 hit)
        {
            hit = default;
            FP bestT = range;
            bool found = false;
            TryPlane(o, d, new FPVector3(FP._0, FP._0, FP._0),   new FPVector3(FP._0,  (FP)1, FP._0),  ref bestT, ref hit, ref found); // floor
            TryPlane(o, d, new FPVector3(FP._0, RoomH, FP._0),   new FPVector3(FP._0, -(FP)1, FP._0),  ref bestT, ref hit, ref found); // ceiling
            TryPlane(o, d, new FPVector3(-RoomHW, FP._0, FP._0), new FPVector3( (FP)1, FP._0, FP._0),  ref bestT, ref hit, ref found); // wall -X
            TryPlane(o, d, new FPVector3( RoomHW, FP._0, FP._0), new FPVector3(-(FP)1, FP._0, FP._0),  ref bestT, ref hit, ref found); // wall +X
            TryPlane(o, d, new FPVector3(FP._0, FP._0, -RoomHW), new FPVector3(FP._0, FP._0,  (FP)1),  ref bestT, ref hit, ref found); // wall -Z
            TryPlane(o, d, new FPVector3(FP._0, FP._0,  RoomHW), new FPVector3(FP._0, FP._0, -(FP)1),  ref bestT, ref hit, ref found); // wall +Z
            return found;
        }

        static void TryPlane(FPVector3 o, FPVector3 d, FPVector3 p0, FPVector3 n,
                             ref FP bestT, ref FPVector3 bestHit, ref bool found)
        {
            FP denom = FPVector3.Dot(d, n);
            if (denom == FP._0) return;                        // ray parallel to plane
            FP t = FPVector3.Dot(p0 - o, n) / denom;
            if (t > FP._0 && t < bestT) { bestT = t; bestHit = o + d * t; found = true; }
        }

        static void CreateGrappleRope(Frame f, Mover* m, FPVector3 playerPos, FPVector3 anchorPos)
        {
            const int nodeCount = 8;
            EntityRef e = f.Create();
            f.Add<Rope>(e);
            var rope = f.Unsafe.GetPointer<Rope>(e);
            rope->Id = m->PlayerIndex;                        // unique stable id (one rope per player)
            rope->AttachedPlayer = m->PlayerIndex;

            FPVector3 span = playerPos - anchorPos;
            // Pre-stretch: rest length is a fraction of the span => the rope reels the player toward the anchor.
            rope->SegmentRest = span.Magnitude * GrappleRestFactor / (FP)(nodeCount - 1);

            rope->Nodes = f.AllocateList<RopeNode>(nodeCount);
            var nodes = f.ResolveList(rope->Nodes);
            for (int i = 0; i < nodeCount; i++)
            {
                FP fi = (FP)i / (FP)(nodeCount - 1);
                bool endNode = (i == 0 || i == nodeCount - 1);
                nodes.Add(new RopeNode
                {
                    Pos = anchorPos + span * fi,
                    Vel = FPVector3.Zero,
                    InvMass = endNode ? FP._0 : FP._1,        // both ends pinned (anchor + player)
                    Force = FPVector3.Zero,
                });
            }

            m->GrappleActive = FP._1;
            m->GrappleAnchorPos = anchorPos;
        }
    }
}
