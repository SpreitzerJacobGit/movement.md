using MovementMD.Core.Macro;
using UnityEngine;

namespace MovementMD.Presentation
{
    /// <summary>Builds primitive GameObjects for placeable geometry types (renderer + ghost).</summary>
    public static class GeometryBuilder
    {
        public static GameObject Create(GeometryType type, Color tint, bool createCollider)
        {
            PrimitiveType pt = type.Shape == GeometryShape.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(pt);
            go.name = string.IsNullOrEmpty(type.Name) ? type.Shape.ToString() : type.Name;

            if (!createCollider)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);
            }

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = tint;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            switch (type.Shape)
            {
                case GeometryShape.Box:
                case GeometryShape.Wall:
                    go.transform.localScale = type.Size;
                    break;
                case GeometryShape.Ramp:
                    go.transform.localScale = type.Size;
                    go.transform.localRotation = Quaternion.Euler(40f, 0f, 0f); // tilted proxy (no wedge primitive)
                    break;
                case GeometryShape.Cylinder:
                    // default cylinder: diameter 1, height 2 → scale x/z to diameter, y to height/2
                    go.transform.localScale = new Vector3(type.Size.x, type.Size.y * 0.5f, type.Size.z);
                    break;
            }
            return go;
        }

        // Side ownership tints — symmetric/neutral so neither reads as "good". Read at a glance who
        // placed a piece (perfect information).
        public static Color SideTint(int sideIndex)
            => sideIndex == 0 ? new Color(0.30f, 0.78f, 1.00f, 1f) : new Color(1.00f, 0.62f, 0.30f, 1f);
    }
}
