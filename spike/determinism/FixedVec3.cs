// Deterministic 3-vector over Q32.32 fixed-point. Stand-in for Quantum's FPVector3.

namespace Determinism;

public readonly struct FixedVec3
{
    public readonly Fixed X, Y, Z;
    public FixedVec3(Fixed x, Fixed y, Fixed z) { X = x; Y = y; Z = z; }

    public static readonly FixedVec3 Zero = new(Fixed.Zero, Fixed.Zero, Fixed.Zero);

    public static FixedVec3 operator +(FixedVec3 a, FixedVec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static FixedVec3 operator -(FixedVec3 a, FixedVec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static FixedVec3 operator -(FixedVec3 a) => new(-a.X, -a.Y, -a.Z);
    public static FixedVec3 operator *(FixedVec3 a, Fixed s) => new(a.X * s, a.Y * s, a.Z * s);

    public static Fixed Dot(FixedVec3 a, FixedVec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public Fixed LengthSq => Dot(this, this);
    public Fixed Length => Fixed.Sqrt(LengthSq);
}
