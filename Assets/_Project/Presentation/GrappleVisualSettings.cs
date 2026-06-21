using System;
using UnityEngine;

namespace MovementMD.Presentation
{
    /// <summary>
    /// Visual configuration for the grapple rope. Presentation only (brief §1.3: grapples are
    /// physical ropes and always visible — a perfect-information tell). Read by
    /// <see cref="GrappleLineRenderer"/>; never feeds simulation state. Tweaked live from the
    /// Developer Menu's "Grapple Visual" tool and persisted to the asset (editor).
    /// </summary>
    [CreateAssetMenu(fileName = "GrappleVisualSettings", menuName = "MovementMD/Grapple Visual Settings", order = 1)]
    public sealed class GrappleVisualSettings : ScriptableObject
    {
        [Header("Color & Glow")]
        [Tooltip("Bright core line color.")] public Color CoreColor = new Color(0.55f, 0.85f, 1.0f, 1f);
        [Tooltip("Additive halo color (the glow).")] public Color GlowColor = new Color(0.30f, 0.65f, 1.0f, 1f);
        [Tooltip("Glow strength 0..1 — scales halo width and additive intensity.")]
        [Range(0f, 1f)] public float GlowIntensity = 0.6f;

        [Header("Width (function along the rope)")]
        [Tooltip("Core line peak width (meters) at the peak of the width curve.")]
        public float CoreWidth = 0.08f;
        [Tooltip("Halo line peak width (meters). Wider than core → more glow.")]
        public float HaloWidth = 0.35f;
        [Tooltip("Width function along the rope. t=0 = player end, t=1 = anchor end. " +
                 "Value is a 0..1 multiplier on Core/Halo width. Default: wider near the player.")]
        public AnimationCurve WidthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

        [Header("Geometry")]
        [Tooltip("Rounds bend joints. 0 = sharp corners.")] [Range(0, 16)] public int CornerVertices = 4;
        [Tooltip("Rounds end caps. 0 = flat caps.")] [Range(0, 16)] public int CapVertices = 4;

        /// <summary>Raised whenever a tweak changes the look. Renderers subscribe to rebuild.</summary>
        public event Action Changed;

        /// <summary>Call after mutating any field (the dev tool does this after each edit).</summary>
        public void NotifyChanged() => Changed?.Invoke();
    }
}
