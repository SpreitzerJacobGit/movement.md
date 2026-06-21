// The First Test subject: N stiff coupled-spring ropes that collide with each other.
//
// Determinism design (why this is bit-stable):
//   1. All forces for a tick are computed from a FROZEN position snapshot, then accumulated
//      into a per-node force buffer with fixed-point ADDITION (exact, associative). Because no
//      force reads a velocity/position mutated earlier in the same tick, the result is invariant
//      to the order forces are summed.
//   2. Rope-rope collision pairs are resolved in a STABLE ORDER SORTED BY ROPE ID (brief §4.3),
//      never by hash/iteration order. This is the guard rail the 4-rope test checks. Even though
//      (1) already makes summation order-independent, the sort is enforced so any future change
//      that reads mid-tick state cannot silently introduce run-dependent ordering.
//   3. Integration is semi-implicit Euler at a fixed timestep. No variable substepping.
//
// Port note: replace Fixed/FixedVec3 with Quantum FP/FPVector3 and store Rope/Node as Quantum
// components/QLists. The algorithm is unchanged.

using System;
using System.Collections.Generic;
using Simulation.Math;

namespace Simulation.Systems;

public struct Node
{
    public FixedVec3 Pos;
    public FixedVec3 Vel;
    public Fixed InvMass; // 0 == pinned anchor (immovable)
}

public sealed class Rope
{
    public int Id;
    public Node[] Nodes = Array.Empty<Node>();
}

public sealed class SolverParams
{
    public Fixed Dt;
    public Fixed SpringK;       // structural stiffness (stiff)
    public Fixed Damping;       // velocity damping along segment
    public Fixed SegmentRest;   // rest length per segment
    public Fixed Gravity;       // downward (-Y) accel
    public Fixed CollisionDist; // rope-rope contact distance
    public Fixed CollisionK;    // penalty stiffness on contact
}

public sealed class RopeSolver
{
    private readonly Rope[] _ropes;
    private readonly SolverParams _p;
    private readonly FixedVec3[][] _force; // force buffer per rope per node

    public RopeSolver(Rope[] ropes, SolverParams p)
    {
        _ropes = ropes;
        _p = p;
        _force = new FixedVec3[ropes.Length][];
        for (int r = 0; r < ropes.Length; r++)
            _force[r] = new FixedVec3[ropes[r].Nodes.Length];
    }

    public IReadOnlyList<Rope> Ropes => _ropes;

    // Advance one fixed tick. `iterationSeed != 0` shuffles the ORDER pairs are generated in
    // (Aspect #3): output must be invariant because pairs are re-sorted by ID before resolution.
    public void Step(uint iterationSeed = 0)
    {
        ClearForces();
        AccumulateGravity();
        AccumulateSprings();          // from frozen positions
        AccumulateRopeRopeCollisions(iterationSeed); // ID-sorted pair order
        Integrate();
    }

    private void ClearForces()
    {
        for (int r = 0; r < _ropes.Length; r++)
            for (int i = 0; i < _force[r].Length; i++)
                _force[r][i] = FixedVec3.Zero;
    }

    private void AccumulateGravity()
    {
        var g = new FixedVec3(Fixed.Zero, -_p.Gravity, Fixed.Zero);
        for (int r = 0; r < _ropes.Length; r++)
        {
            var nodes = _ropes[r].Nodes;
            for (int i = 0; i < nodes.Length; i++)
                if (nodes[i].InvMass > Fixed.Zero)
                    _force[r][i] += g; // mass folded into integrate via InvMass
        }
    }

    private void AccumulateSprings()
    {
        for (int r = 0; r < _ropes.Length; r++)
        {
            var nodes = _ropes[r].Nodes;
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                FixedVec3 delta = nodes[i + 1].Pos - nodes[i].Pos;
                Fixed len = delta.Length;
                if (len.Raw == 0) continue;
                FixedVec3 dir = delta * (Fixed.One / len);
                Fixed stretch = len - _p.SegmentRest;
                FixedVec3 springF = dir * (_p.SpringK * stretch);

                FixedVec3 relVel = nodes[i + 1].Vel - nodes[i].Vel;
                FixedVec3 dampF = dir * (_p.Damping * FixedVec3.Dot(relVel, dir));

                FixedVec3 f = springF + dampF;
                _force[r][i] += f;        // pulls i toward i+1
                _force[r][i + 1] += -f;   // equal & opposite
            }
        }
    }

    private void AccumulateRopeRopeCollisions(uint iterationSeed)
    {
        // Build unordered rope-rope pairs, then SORT BY (idA, idB). Brief §4.3.
        var pairs = new List<(int a, int b)>();
        for (int a = 0; a < _ropes.Length; a++)
            for (int b = a + 1; b < _ropes.Length; b++)
                pairs.Add((a, b));

        if (iterationSeed != 0) Shuffle(pairs, iterationSeed); // Aspect #3: perturb input order
        pairs.Sort((x, y) =>
        {
            int ax = _ropes[x.a].Id, bx = _ropes[x.b].Id;
            int ay = _ropes[y.a].Id, by = _ropes[y.b].Id;
            return ax != ay ? ax.CompareTo(ay) : bx.CompareTo(by);
        });

        foreach (var (a, b) in pairs)
            ResolveRopePair(_ropes[a], _force[a], _ropes[b], _force[b]);
    }

    private void ResolveRopePair(Rope ra, FixedVec3[] fa, Rope rb, FixedVec3[] fb)
    {
        for (int i = 0; i < ra.Nodes.Length - 1; i++)      // segment index order is stable
            for (int j = 0; j < rb.Nodes.Length - 1; j++)
            {
                ClosestPtSegSeg(ra.Nodes[i].Pos, ra.Nodes[i + 1].Pos,
                                rb.Nodes[j].Pos, rb.Nodes[j + 1].Pos,
                                out Fixed s, out Fixed t, out FixedVec3 pa, out FixedVec3 pb);

                FixedVec3 d = pa - pb;
                Fixed dist = d.Length;
                if (dist >= _p.CollisionDist || dist.Raw == 0) continue;

                FixedVec3 n = d * (Fixed.One / dist);
                Fixed pen = _p.CollisionDist - dist;
                FixedVec3 push = n * (_p.CollisionK * pen);

                // Distribute penalty across the two endpoints of each segment by barycentric s,t.
                fa[i]     += push * (Fixed.One - s);
                fa[i + 1] += push * s;
                fb[j]     += -push * (Fixed.One - t);
                fb[j + 1] += -push * t;
            }
    }

    private void Integrate()
    {
        for (int r = 0; r < _ropes.Length; r++)
        {
            var nodes = _ropes[r].Nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].InvMass.Raw == 0) continue; // pinned
                nodes[i].Vel += _force[r][i] * (nodes[i].InvMass * _p.Dt); // semi-implicit
                nodes[i].Pos += nodes[i].Vel * _p.Dt;
            }
        }
    }

    private static void ClosestPtSegSeg(FixedVec3 p1, FixedVec3 q1, FixedVec3 p2, FixedVec3 q2,
        out Fixed s, out Fixed t, out FixedVec3 c1, out FixedVec3 c2)
    {
        FixedVec3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
        Fixed a = FixedVec3.Dot(d1, d1), e = FixedVec3.Dot(d2, d2), f = FixedVec3.Dot(d2, r);

        if (a.Raw == 0 && e.Raw == 0) { s = Fixed.Zero; t = Fixed.Zero; c1 = p1; c2 = p2; return; }

        if (a.Raw == 0) { s = Fixed.Zero; t = Fixed.Clamp01(f / e); }
        else
        {
            Fixed c = FixedVec3.Dot(d1, r);
            if (e.Raw == 0) { t = Fixed.Zero; s = Fixed.Clamp01(-c / a); }
            else
            {
                Fixed bb = FixedVec3.Dot(d1, d2);
                Fixed denom = a * e - bb * bb;
                s = denom.Raw != 0 ? Fixed.Clamp01((bb * f - c * e) / denom) : Fixed.Zero;
                t = (bb * s + f) / e;
                if (t.Raw < 0) { t = Fixed.Zero; s = Fixed.Clamp01(-c / a); }
                else if (t > Fixed.One) { t = Fixed.One; s = Fixed.Clamp01((bb - c) / a); }
            }
        }
        c1 = p1 + d1 * s;
        c2 = p2 + d2 * t;
    }

    // Deterministic Fisher-Yates with a seeded integer LCG — used ONLY to perturb input order
    // for Aspect #3. Not part of the sim; never affects state when iterationSeed == 0.
    private static void Shuffle<T>(List<T> list, uint seed)
    {
        uint state = seed;
        for (int i = list.Count - 1; i > 0; i--)
        {
            state = state * 1664525u + 1013904223u;
            int j = (int)(state % (uint)(i + 1));
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // Flat bit-exact state vector: every node's pos+vel raw longs in fixed order.
    // Used by the harness for run-to-run diff, rollback re-sim diff, and order-independence diff.
    public long[] Serialize()
    {
        int count = 0;
        foreach (var rope in _ropes) count += rope.Nodes.Length * 6;
        var buf = new long[count];
        int k = 0;
        foreach (var rope in _ropes)
            foreach (var nd in rope.Nodes)
            {
                buf[k++] = nd.Pos.X.Raw; buf[k++] = nd.Pos.Y.Raw; buf[k++] = nd.Pos.Z.Raw;
                buf[k++] = nd.Vel.X.Raw; buf[k++] = nd.Vel.Y.Raw; buf[k++] = nd.Vel.Z.Raw;
            }
        return buf;
    }

    public void Deserialize(long[] buf)
    {
        int k = 0;
        for (int r = 0; r < _ropes.Length; r++)
        {
            var nodes = _ropes[r].Nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Pos = new FixedVec3(Fixed.FromRaw(buf[k++]), Fixed.FromRaw(buf[k++]), Fixed.FromRaw(buf[k++]));
                nodes[i].Vel = new FixedVec3(Fixed.FromRaw(buf[k++]), Fixed.FromRaw(buf[k++]), Fixed.FromRaw(buf[k++]));
            }
        }
    }
}
