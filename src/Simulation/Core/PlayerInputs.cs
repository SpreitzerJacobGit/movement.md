using Simulation.Math;

namespace Simulation.Core;

/// <summary>
/// Spin accumulator state for the grapple swing meter. <see cref="StoredAngle"/> is the
/// accumulated angular delta awaiting discharge; <see cref="DischargeRate"/> governs how
/// quickly it bleeds off when released. Both fixed-point so rollback math is reproducible.
/// </summary>
public readonly struct FPSpin
{
    public readonly Fixed StoredAngle;
    public readonly Fixed DischargeRate;

    public FPSpin(Fixed storedAngle, Fixed dischargeRate)
    {
        StoredAngle = storedAngle;
        DischargeRate = dischargeRate;
    }
}

/// <summary>
/// One player's worth of input for a single tick. Every field is a value type (Fixed / bool)
/// so the struct serializes deterministically and can be queued, replayed, and hashed for
/// network sync. <see cref="AimPoint"/> is the grapple anchor in world space; <see cref="MoveX"/>
/// / <see cref="MoveZ"/> are local-space move axes; aim deltas (<see cref="AimYaw"/>,
/// <see cref="AimPitch"/>) are wrapped angles.
/// </summary>
public readonly struct PlayerInputs
{
    public readonly Fixed MoveX;
    public readonly Fixed MoveZ;
    public readonly Fixed AimYaw;
    public readonly Fixed AimPitch;
    public readonly bool Jump;
    public readonly bool Slide;
    public readonly bool Grapple;
    public readonly FixedVec3 AimPoint;
    public readonly bool SpinRecord;
    public readonly bool SpinDischarge;
    public readonly bool Fire;

    public PlayerInputs(
        Fixed moveX,
        Fixed moveZ,
        Fixed aimYaw,
        Fixed aimPitch,
        bool jump,
        bool slide,
        bool grapple,
        FixedVec3 aimPoint,
        bool spinRecord,
        bool spinDischarge,
        bool fire)
    {
        MoveX = moveX;
        MoveZ = moveZ;
        AimYaw = aimYaw;
        AimPitch = aimPitch;
        Jump = jump;
        Slide = slide;
        Grapple = grapple;
        AimPoint = aimPoint;
        SpinRecord = spinRecord;
        SpinDischarge = spinDischarge;
        Fire = fire;
    }
}
