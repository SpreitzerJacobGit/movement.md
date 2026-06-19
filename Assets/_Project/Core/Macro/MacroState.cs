using System;
using System.Collections.Generic;
using UnityEngine;

namespace MovementMD.Core.Macro
{
    public readonly struct PlacedPiece
    {
        public Guid Id { get; }
        public int SideIndex { get; }
        public int TypeId { get; }
        public Vector3 Position { get; }

        public PlacedPiece(Guid id, int sideIndex, int typeId, Vector3 position)
        {
            Id = id;
            SideIndex = sideIndex;
            TypeId = typeId;
            Position = position;
        }
    }

    /// <summary>
    /// Geometry placed during edit windows. Owned by the match (created on match start, cleared on
    /// match end) so pieces persist across the entire match — accumulating through every mid-game
    /// and between-game edit. Both sides' pieces live here and are visible (perfect information).
    /// </summary>
    public sealed class MacroState
    {
        private readonly List<PlacedPiece> _pieces = new();
        public IReadOnlyList<PlacedPiece> Pieces => _pieces;

        public event Action<PlacedPiece> PieceAdded;
        public event Action<Guid> PieceRemoved;
        public event Action Cleared;

        public PlacedPiece Add(int sideIndex, int typeId, Vector3 position)
        {
            var piece = new PlacedPiece(Guid.NewGuid(), sideIndex, typeId, position);
            _pieces.Add(piece);
            PieceAdded?.Invoke(piece);
            return piece;
        }

        public bool Remove(Guid id)
        {
            int count = _pieces.Count;
            for (int i = 0; i < count; i++)
            {
                if (_pieces[i].Id == id)
                {
                    _pieces.RemoveAt(i);
                    PieceRemoved?.Invoke(id);
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            _pieces.Clear();
            Cleared?.Invoke();
        }
    }
}
