// movement.md §5 sandbox — spawns grapple anchor points on an overhead grid.
//
// Anchors are data entities (Anchor marker + Transform3D). GrappleSystem attaches ropes to the nearest
// overhead one when a player fires the grapple. Count-agnostic grid placement.

using Photon.Deterministic;

namespace Quantum
{
    public unsafe class AnchorSpawnSystem : SystemSignalsOnly
    {
        public override void OnInit(Frame f)
        {
            FP y = (FP)8;                                     // overhead
            for (int gx = -2; gx <= 2; gx++)
                for (int gz = -2; gz <= 2; gz++)
                {
                    EntityRef e = f.Create();
                    f.Add<Transform3D>(e);
                    f.Add<Anchor>(e);
                    var t = f.Unsafe.GetPointer<Transform3D>(e);
                    t->Position = new FPVector3((FP)(gx * 4), y, (FP)(gz * 4));
                }
        }
    }
}
