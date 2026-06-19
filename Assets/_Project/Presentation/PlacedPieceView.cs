using System;
using UnityEngine;

namespace MovementMD.Presentation
{
    /// <summary>
    /// Marks a rendered placed piece so the place-geometry UI can raycast and remove it. Carries
    /// the <see cref="MacroState"/> id; keeps view objects decoupled from the model.
    /// </summary>
    public sealed class PlacedPieceView : MonoBehaviour
    {
        public Guid Id;
    }
}
