using UnityEngine;

namespace MovementMD.Presentation
{
    /// <summary>
    /// Provider of the grapple rope's physical point chain, ordered from the player (index 0) to
    /// the anchor (last). The presentation layer reads this read-only; the deterministic grapple
    /// system implements it once built. A mock (<see cref="GrappleVisualDemo"/>) drives visual
    /// tooling until the sim is wired.
    /// </summary>
    public interface IGrapplePointSource
    {
        /// <summary>Ordered world-space points from player end to anchor end. Empty when inactive.</summary>
        Vector3[] GetPoints();

        /// <summary>True while a grapple is active and should render.</summary>
        bool IsActive { get; }
    }
}
