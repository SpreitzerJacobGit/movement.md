// movement.md §5 — RopeCouplingSystem (Option B player<->rope coupling).
//
// Runs BEFORE the solver. For each rope attached to a player, pin its player-end node (the last node)
// to that player's current Pos/Vel — the player drives that end. The solver then computes the chain's
// pull on it (stashed as Rope.PlayerForce), which MovementSystem applies next tick.
//
// Gathers movers into a player->entity map first, then iterates ropes — no nested filters.

using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
    public unsafe class RopeCouplingSystem : SystemMainThread
    {
        const int MaxPlayers = 8;

        public override void Update(Frame f)
        {
            // Gather player index -> mover entity + velocity. (stackalloc zero-init => EntityRef.None)
            EntityRef* moverEntity = stackalloc EntityRef[MaxPlayers];
            FPVector3* moverVel = stackalloc FPVector3[MaxPlayers];
            var mit = f.Filter<Mover>();
            while (mit.NextUnsafe(out EntityRef me, out Mover* m))
            {
                if (m->PlayerIndex >= 0 && m->PlayerIndex < MaxPlayers)
                {
                    moverEntity[m->PlayerIndex] = me;
                    moverVel[m->PlayerIndex] = m->Velocity;
                }
            }

            // Pin each grapple rope's player-end to its mover.
            var rit = f.Filter<Rope>();
            while (rit.NextUnsafe(out EntityRef re, out Rope* rope))
            {
                if (rope->AttachedPlayer < 0) continue;
                int p = rope->AttachedPlayer;
                if (p >= MaxPlayers || moverEntity[p] == EntityRef.None) continue;

                var mt = f.Unsafe.GetPointer<Transform3D>(moverEntity[p]);
                var nodes = f.ResolveList(rope->Nodes);
                RopeNode* ln = nodes.GetPointer(nodes.Count - 1);
                ln->Pos = mt->Position;
                ln->Vel = moverVel[p];
                // InvMass is 0 (pinned at creation) — the solver won't move it; the player owns this end.
            }
        }
    }
}
