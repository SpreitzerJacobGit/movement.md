using System;
using System.Collections.Generic;
using MovementMD.Core.Macro;
using UnityEngine;

namespace MovementMD.Presentation
{
    /// <summary>
    /// Renders <see cref="MacroState"/> pieces as primitives, keeping the view in sync with the
    /// model. Spawns into the active scene; GameObjects render under the mode-scene camera
    /// regardless of which scene owns them.
    /// </summary>
    public sealed class PlacedGeometryRenderer : MonoBehaviour
    {
        [SerializeField] private GeometryPalette palette;
        private readonly Dictionary<Guid, GameObject> _views = new();
        private MacroState _macro;

        public void Bind(MacroState macro)
        {
            Unbind();
            _macro = macro;
            if (_macro == null) return;
            _macro.PieceAdded += OnAdded;
            _macro.PieceRemoved += OnRemoved;
            _macro.Cleared += OnCleared;
            for (int i = 0; i < _macro.Pieces.Count; i++) OnAdded(_macro.Pieces[i]);
        }

        public void Unbind()
        {
            if (_macro != null)
            {
                _macro.PieceAdded -= OnAdded;
                _macro.PieceRemoved -= OnRemoved;
                _macro.Cleared -= OnCleared;
                _macro = null;
            }
            ClearViews();
        }

        private void OnAdded(PlacedPiece piece)
        {
            if (_views.ContainsKey(piece.Id)) return;
            var type = ResolveType(piece.TypeId);
            var go = GeometryBuilder.Create(type, GeometryBuilder.SideTint(piece.SideIndex), createCollider: true);
            var pos = piece.Position;
            pos.y = type.Size.y * 0.5f; // base on the ground plane
            go.transform.position = pos;
            var view = go.AddComponent<PlacedPieceView>();
            view.Id = piece.Id;
            _views[piece.Id] = go;
        }

        private void OnRemoved(Guid id)
        {
            if (_views.TryGetValue(id, out var go))
            {
                if (go != null) Destroy(go);
                _views.Remove(id);
            }
        }

        private void OnCleared() => ClearViews();

        private void ClearViews()
        {
            foreach (var kv in _views)
                if (kv.Value != null) Destroy(kv.Value);
            _views.Clear();
        }

        private GeometryType ResolveType(int typeId)
        {
            if (palette != null && (uint)typeId < (uint)palette.Types.Length)
                return palette.Types[typeId];
            return new GeometryType { Name = "Block", Shape = GeometryShape.Box, Size = new Vector3(2f, 2f, 2f), Tint = Color.gray };
        }
    }
}
