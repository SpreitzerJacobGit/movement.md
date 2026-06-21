// Q32.32 deterministic fixed-point. Pure integer math => bit-identical on every machine.
//
// This is a STAND-IN for Photon Quantum's `FP`. When the First Test moves inside Quantum,
// delete Fixed/FixedVec3 and substitute Quantum's `FP`/`FPVector3` — the call sites in
// RopeSolver.cs are written against the same surface (operators, Sqrt, Dot, Length).
// Keeping a local fixed-point here lets Aspects 1-3 be proven with only the .NET SDK,
// before Unity/Quantum is installed (see ../README.md).
//
// CONFIRM-IN-QUANTUM: Quantum's FP is Q__.__ with its own rounding; do not assume bit-equality
// between this type and FP. This type proves the *algorithm* is order/run/rollback stable;
// Quantum FP proves Aspect #4 (expressibility) and re-validates 1-3 in the real engine.

using System;

namespace Simulation.Math;

public readonly struct Fixed : IEquatable<Fixed>
{
    public const int FracBits = 32;
    public readonly long Raw;

    private Fixed(long raw) => Raw = raw;
    public static Fixed FromRaw(long raw) => new(raw);
    public static Fixed FromInt(int v) => new((long)v << FracBits);

    // Setup-only. Never call inside the sim step — float input would break determinism intent.
    public static Fixed FromDouble(double v) => new((long)System.Math.Round(v * (1L << FracBits)));

    public static readonly Fixed Zero = new(0);
    public static readonly Fixed One = FromInt(1);

    public static Fixed operator +(Fixed a, Fixed b) => new(a.Raw + b.Raw);   // exact, associative
    public static Fixed operator -(Fixed a, Fixed b) => new(a.Raw - b.Raw);
    public static Fixed operator -(Fixed a) => new(-a.Raw);
    public static Fixed operator *(Fixed a, Fixed b) => new((long)(((Int128)a.Raw * b.Raw) >> FracBits));
    public static Fixed operator /(Fixed a, Fixed b) => new((long)(((Int128)a.Raw << FracBits) / b.Raw));

    public static bool operator <(Fixed a, Fixed b) => a.Raw < b.Raw;
    public static bool operator >(Fixed a, Fixed b) => a.Raw > b.Raw;
    public static bool operator <=(Fixed a, Fixed b) => a.Raw <= b.Raw;
    public static bool operator >=(Fixed a, Fixed b) => a.Raw >= b.Raw;

    public static Fixed Sqrt(Fixed v)
    {
        if (v.Raw <= 0) return Zero;
        // sqrt(raw/2^32)*2^32 == isqrt(raw << 32). Newton's method on Int128 — deterministic.
        Int128 n = (Int128)v.Raw << FracBits;
        Int128 x = n, y = (x + 1) / 2;
        while (y < x) { x = y; y = (x + n / x) / 2; }
        return new((long)x);
    }

    public static Fixed Clamp01(Fixed v) => v.Raw < 0 ? Zero : (v > One ? One : v);

    // Fixed-point trig via a 2048-entry LUT over [0, 2pi). Built once at type init from System.Math.Sin
    // (setup-only — float never enters the sim step; the LUT is frozen deterministic data afterward).
    private static readonly Fixed TwoPi = FromDouble(System.Math.PI * 2.0);
    private static readonly Fixed[] SinLut = BuildSinLut();
    private static Fixed[] BuildSinLut()
    {
        const int n = 2048;
        var lut = new Fixed[n];
        for (int i = 0; i < n; i++)
            lut[i] = FromDouble(System.Math.Sin(System.Math.PI * 2.0 * i / n));
        return lut;
    }
    public static Fixed Sin(Fixed radians) => SinLut[LutIndex(radians)];
    public static Fixed Cos(Fixed radians) => SinLut[(LutIndex(radians) + 2048 / 4) % 2048];
    private static int LutIndex(Fixed radians)
    {
        long r = radians.Raw % TwoPi.Raw;       // reduce to [-2pi, 2pi)
        if (r < 0) r += TwoPi.Raw;               // [0, 2pi)
        long idx = (long)(((Int128)r * 2048) / TwoPi.Raw);
        return (int)(idx % 2048);
    }

    public bool Equals(Fixed other) => Raw == other.Raw;
    public override bool Equals(object? o) => o is Fixed f && Equals(f);
    public override int GetHashCode() => Raw.GetHashCode();
    public override string ToString() => ((double)Raw / (1L << FracBits)).ToString("0.000000");
}
