using UnityEngine;

namespace MovementMD.Dev
{
    /// <summary>
    /// Runtime-tweakable sim parameters. These are <b>float proxies</b> for dev feel-tuning and
    /// presentation only — the deterministic sim consumes fixed-point (Quantum <c>FP</c>) values,
    /// never these floats directly. Converted at the sim boundary when the host is wired.
    /// </summary>
    [CreateAssetMenu(fileName = "Tunables", menuName = "MovementMD/Tunables", order = 0)]
    public sealed class Tunables : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Max ground speed (m/s).")] public float MoveSpeed = 8f;
        [Tooltip("Slide launch speed (m/s).")] public float SlideSpeed = 12f;
        [Tooltip("Jump impulse (m/s).")] public float JumpImpulse = 7f;
        [Tooltip("Air-control fraction 0..1.")] public float AirControl = 0.35f;

        [Header("Grapple (stiff coupled spring — steering)")]
        [Tooltip("Spring stiffness (N/m).")] public float GrappleStiffness = 4000f;
        [Tooltip("Spring damping (N·s/m).")] public float GrappleDamper = 60f;
        [Tooltip("Max rope length (m).")] public float GrappleMaxLength = 40f;

        [Header("Spin (acceleration — swept angle, not angular velocity)")]
        [Tooltip("Spin gain per swept radian (inverse-speed weighted at the sim boundary).")]
        public float SpinGainRate = 1f;
        [Tooltip("Max bankable spin (radians).")] public float SpinMaxAngle = 12f;
        [Tooltip("Discharge rate (radians/sec).")] public float SpinDischargeRate = 6f;

        [Header("Offense")]
        [Tooltip("Damage baseline per shot.")] public float ShotDamage = 10f;
        [Tooltip("Damage multiplier at full speed.")] public float SpeedDamageScale = 2f;
    }
}
