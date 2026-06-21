// movement.md §5 — GrappleSystem (Option B: real ropes in RopeSolverSystem).
//
// Grapple input -> create/destroy grapple ropes. The anchor is the screen-center reticle point
// (passed in via input.AimPoint, computed Unity-side from a camera raycast), so the grapple lands
// exactly on the cursor — no parallax from the over-the-shoulder camera offset. The rope spawns
// PRE-STRETCHED (rest length = GrappleRestFactor of the span, <1) so it reels the player in.
//
// Quantum forbids structural changes during filter iteration, so this gathers first, then mutates.

using Photon.Deterministic;

namespace Quantum
{
    public unsafe class GrappleSystem : SystemMainThread
    {
        const int MaxPlayers = 8;

        public override void Update(Frame f)
        {
            var mcfg = f.GetSingleton<MovementConfig>();

            // Gather attached rope per player. (stackalloc is zero-init => EntityRef.None)
            EntityRef* attached = stackalloc EntityRef[MaxPlayers];
            var rit = f.Filter<Rope>();
            while (rit.NextUnsafe(out EntityRef re, out Rope* r))
                if (r->AttachedPlayer >= 0 && r->AttachedPlayer < MaxPlayers) attached[r->AttachedPlayer] = re;

            // Gather per-mover input + transform + aim point.
            EntityRef* mEntity = stackalloc EntityRef[MaxPlayers];
            int*       mPlayer = stackalloc int[MaxPlayers];
            FP*        mDown   = stackalloc FP[MaxPlayers];
            FP*        mWasDown= stackalloc FP[MaxPlayers];
            FPVector3* mPos    = stackalloc FPVector3[MaxPlayers];
            FPVector3* mAim    = stackalloc FPVector3[MaxPlayers];
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
                mAim[mCount]     = input->AimPoint;
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
                    CreateGrappleRope(f, m, mPos[i], mAim[i], mcfg.GrappleRestFactor);   // anchor = reticle point
                else if (!down && wasDown && att != EntityRef.None)
                {
                    f.Destroy(att);
                    m->GrappleActive = FP._0;
                }

                m->GrappleHeld = mDown[i];
            }
        }

        static void CreateGrappleRope(Frame f, Mover* m, FPVector3 playerPos, FPVector3 anchorPos, FP restFactor)
        {
            const int nodeCount = 8;
            EntityRef e = f.Create();
            f.Add<Rope>(e);
            var rope = f.Unsafe.GetPointer<Rope>(e);
            rope->Id = m->PlayerIndex;                        // unique stable id (one rope per player)
            rope->AttachedPlayer = m->PlayerIndex;

            FPVector3 span = playerPos - anchorPos;
            // Pre-stretch: rest length is a fraction of the span => the rope reels the player toward the anchor.
            rope->SegmentRest = span.Magnitude * restFactor / (FP)(nodeCount - 1);

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
