// Photon Quantum 3.0.11 port of RopeSolver.Step (the First Test subject, Stage 2).
//
// Line-for-line port of spike/determinism/RopeSolver.cs onto Quantum's FP / FPVector3 / QList / ECS.
// The algorithm is identical to the local spike and proven deterministic there (FIRST_TEST.md Run 1);
// this file changes only the storage and math types. All API calls below were verified by reflection
// against this project's Quantum.Engine.dll / Quantum.Deterministic.dll (3.0.11).
//
// Determinism guarantees carried over from the spike:
//   * All forces are accumulated from a FROZEN position snapshot (positions only move in Integrate,
//     the final pass), so force summation is order-independent.
//   * Rope-rope pairs are resolved in a STABLE ORDER SORTED BY Rope.Id (brief §4.3) — the guard
//     rail the four-rope test exists to check. Pair endpoints are canonicalised so the lower Id is
//     always first, making the order independent of entity-creation order.
//   * Fixed timestep f.DeltaTime (SessionConfig.UpdateFPS locked to 128). No variable substepping.
//
// Registration: added in SystemSetup.User.cs (AFTER RopeSpawnSystem). Count-agnostic — it iterates
// f.Filter<Rope>(), never a fixed rope count.

using Photon.Deterministic;
using Quantum.Collections;

namespace Quantum
{
    public unsafe class RopeSolverSystem : SystemMainThread
    {
        // Cap for the spike's worst case (4 ropes). Raise if you test more; the assert below is the
        // fail-fast guard rail per CONVENTIONS.md ("throw on violated preconditions").
        const int MaxRopes = 8;
        const int MaxPairs = MaxRopes * (MaxRopes - 1) / 2;

        public override void Update(Frame f)
        {
            var cfg = f.GetSingleton<RopeSolverConfig>();
            FP dt = f.DeltaTime;                            // == 1/128 when SessionConfig.UpdateFPS = 128

            // --- 1. Gather rope entities + ids into stable, stack-local arrays. ---
            EntityRef* ropes = stackalloc EntityRef[MaxRopes];
            int* ids = stackalloc int[MaxRopes];
            int n = 0;

            var it = f.Filter<Rope>();
            while (it.NextUnsafe(out EntityRef e, out Rope* rope))
            {
                Assert.Check(n < MaxRopes, "RopeSolverSystem: rope count exceeds MaxRopes");
                ropes[n] = e;
                ids[n] = rope->Id;
                n++;
            }

            // --- 2. Clear forces + gravity + structural springs, per rope, from frozen positions. ---
            for (int r = 0; r < n; r++)
            {
                var rope = f.Unsafe.GetPointer<Rope>(ropes[r]);
                var nodes = f.ResolveList(rope->Nodes);
                ClearAndGravity(nodes, cfg);
                AccumulateSprings(nodes, rope->SegmentRest, cfg);
            }

            // --- 3. Rope-rope collisions, resolved in Id-sorted pair order (brief §4.3). ---
            ResolveCollisionsIdSorted(f, ropes, ids, n, cfg);

            // --- 3.5 Stash the chain force on the player-end node for coupled grapple ropes.
            //        Read BEFORE Integrate zeroes pinned-node forces; MovementSystem applies it next tick. ---
            for (int r = 0; r < n; r++)
            {
                var rope = f.Unsafe.GetPointer<Rope>(ropes[r]);
                if (rope->AttachedPlayer < 0) continue;
                var nodes = f.ResolveList(rope->Nodes);
                rope->PlayerForce = nodes.GetPointer(nodes.Count - 1)->Force;
            }

            // --- 4. Integrate (semi-implicit Euler) and zero the force accumulator. ---
            for (int r = 0; r < n; r++)
            {
                var rope = f.Unsafe.GetPointer<Rope>(ropes[r]);
                var nodes = f.ResolveList(rope->Nodes);
                Integrate(nodes, dt);
            }
        }

        static void ClearAndGravity(QList<RopeNode> nodes, RopeSolverConfig cfg)
        {
            FPVector3 g = new FPVector3(FP._0, -cfg.Gravity, FP._0);
            for (int i = 0; i < nodes.Count; i++)
            {
                RopeNode* nd = nodes.GetPointer(i);
                nd->Force = FPVector3.Zero;
                if (nd->InvMass > FP._0) nd->Force += g;     // mass==1 here; gravity folded via InvMass in Integrate
            }
        }

        static void AccumulateSprings(QList<RopeNode> nodes, FP segmentRest, RopeSolverConfig cfg)
        {
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                RopeNode* a = nodes.GetPointer(i);
                RopeNode* b = nodes.GetPointer(i + 1);

                FPVector3 delta = b->Pos - a->Pos;
                FP len = delta.Magnitude;
                if (len == FP._0) continue;

                FPVector3 dir = delta * (FP._1 / len);
                FP stretch = len - segmentRest;
                FPVector3 springF = dir * (cfg.SpringK * stretch);

                FPVector3 relVel = b->Vel - a->Vel;
                FPVector3 dampF = dir * (cfg.Damping * FPVector3.Dot(relVel, dir));

                FPVector3 force = springF + dampF;
                a->Force += force;       // pulls i toward i+1
                b->Force += -force;      // equal & opposite
            }
        }

        static void ResolveCollisionsIdSorted(Frame f, EntityRef* ropes, int* ids, int n, RopeSolverConfig cfg)
        {
            // Build unordered rope pairs, canonicalise so the lower Id is first, then sort by
            // (idLow, idHigh). Small n (<= MaxRopes) => a deterministic insertion sort is plenty.
            int* pa = stackalloc int[MaxPairs];   // index of lower-Id rope in `ropes`
            int* pb = stackalloc int[MaxPairs];   // index of higher-Id rope
            long* key = stackalloc long[MaxPairs];
            int pc = 0;

            for (int a = 0; a < n; a++)
                for (int b = a + 1; b < n; b++)
                {
                    int lo = a, hi = b;
                    if (ids[lo] > ids[hi]) { int tmp = lo; lo = hi; hi = tmp; }
                    pa[pc] = lo;
                    pb[pc] = hi;
                    key[pc] = ((long)ids[lo] << 32) | (uint)ids[hi];
                    pc++;
                }

            for (int i = 1; i < pc; i++)         // insertion sort by key (stable, deterministic)
            {
                long k = key[i]; int xa = pa[i], xb = pb[i]; int j = i - 1;
                while (j >= 0 && key[j] > k) { key[j + 1] = key[j]; pa[j + 1] = pa[j]; pb[j + 1] = pb[j]; j--; }
                key[j + 1] = k; pa[j + 1] = xa; pb[j + 1] = xb;
            }

            for (int p = 0; p < pc; p++)
                ResolveRopePair(f, ropes[pa[p]], ropes[pb[p]], cfg);
        }

        static void ResolveRopePair(Frame f, EntityRef ea, EntityRef eb, RopeSolverConfig cfg)
        {
            var ra = f.Unsafe.GetPointer<Rope>(ea);
            var rb = f.Unsafe.GetPointer<Rope>(eb);
            var na = f.ResolveList(ra->Nodes);
            var nb = f.ResolveList(rb->Nodes);

            for (int i = 0; i < na.Count - 1; i++)           // segment index order is stable
                for (int j = 0; j < nb.Count - 1; j++)
                {
                    RopeNode* ai = na.GetPointer(i);
                    RopeNode* ai1 = na.GetPointer(i + 1);
                    RopeNode* bj = nb.GetPointer(j);
                    RopeNode* bj1 = nb.GetPointer(j + 1);

                    ClosestPtSegSeg(ai->Pos, ai1->Pos, bj->Pos, bj1->Pos,
                                    out FP s, out FP t, out FPVector3 ca, out FPVector3 cb);

                    FPVector3 d = ca - cb;
                    FP dist = d.Magnitude;
                    if (dist >= cfg.CollisionDist || dist == FP._0) continue;

                    FPVector3 nrm = d * (FP._1 / dist);
                    FP pen = cfg.CollisionDist - dist;
                    FPVector3 push = nrm * (cfg.CollisionK * pen);

                    ai->Force += push * (FP._1 - s);
                    ai1->Force += push * s;
                    bj->Force += -push * (FP._1 - t);
                    bj1->Force += -push * t;
                }
        }

        static void Integrate(QList<RopeNode> nodes, FP dt)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                RopeNode* nd = nodes.GetPointer(i);
                if (nd->InvMass == FP._0) { nd->Force = FPVector3.Zero; continue; } // pinned anchor
                nd->Vel += nd->Force * (nd->InvMass * dt);   // semi-implicit Euler
                nd->Pos += nd->Vel * dt;
                nd->Force = FPVector3.Zero;                   // keep force out of frame-boundary state
            }
        }

        // Closest points between segment [p1,q1] and [p2,q2]. Direct FP port of the spike's routine
        // (Ericson, Real-Time Collision Detection). All branches stay in fixed-point.
        static void ClosestPtSegSeg(FPVector3 p1, FPVector3 q1, FPVector3 p2, FPVector3 q2,
            out FP s, out FP t, out FPVector3 c1, out FPVector3 c2)
        {
            FPVector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            FP a = FPVector3.Dot(d1, d1), e = FPVector3.Dot(d2, d2), fdot = FPVector3.Dot(d2, r);

            if (a == FP._0 && e == FP._0) { s = FP._0; t = FP._0; c1 = p1; c2 = p2; return; }

            if (a == FP._0) { s = FP._0; t = Clamp01(fdot / e); }
            else
            {
                FP c = FPVector3.Dot(d1, r);
                if (e == FP._0) { t = FP._0; s = Clamp01(-c / a); }
                else
                {
                    FP b = FPVector3.Dot(d1, d2);
                    FP denom = a * e - b * b;
                    s = denom != FP._0 ? Clamp01((b * fdot - c * e) / denom) : FP._0;
                    t = (b * s + fdot) / e;
                    if (t < FP._0) { t = FP._0; s = Clamp01(-c / a); }
                    else if (t > FP._1) { t = FP._1; s = Clamp01((b - c) / a); }
                }
            }
            c1 = p1 + d1 * s;
            c2 = p2 + d2 * t;
        }

        static FP Clamp01(FP v) => FPMath.Clamp(v, FP._0, FP._1);
    }
}
