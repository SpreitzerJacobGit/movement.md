using UnityEngine;

namespace MovementMD.Core.Macro
{
    public enum GeometryShape { Box, Wall, Ramp, Cylinder }

    [System.Serializable]
    public struct GeometryType
    {
        public string Name;
        public GeometryShape Shape;
        public Vector3 Size;
        public Color Tint;
    }

    /// <summary>Palette of placeable geometry types, consumed by the place-geometry UI + renderer.</summary>
    [CreateAssetMenu(fileName = "GeometryPalette", menuName = "MovementMD/Geometry Palette", order = 1)]
    public sealed class GeometryPalette : ScriptableObject
    {
        public GeometryType[] Types =
        {
            new GeometryType { Name = "Cover",  Shape = GeometryShape.Box,     Size = new Vector3(2f, 1.5f, 2f), Tint = new Color(0.70f, 0.70f, 0.70f) },
            new GeometryType { Name = "Wall",   Shape = GeometryShape.Wall,     Size = new Vector3(6f, 3f, 0.5f), Tint = new Color(0.60f, 0.60f, 0.60f) },
            new GeometryType { Name = "Ramp",   Shape = GeometryShape.Ramp,     Size = new Vector3(3f, 2f, 4f),  Tint = new Color(0.65f, 0.65f, 0.65f) },
            new GeometryType { Name = "Pillar", Shape = GeometryShape.Cylinder, Size = new Vector3(1f, 4f, 1f),  Tint = new Color(0.70f, 0.70f, 0.70f) },
        };
    }
}
