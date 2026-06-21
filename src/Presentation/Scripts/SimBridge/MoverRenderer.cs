using UnityEngine;
using Simulation.Core;
using Simulation.Math;

namespace Presentation.SimBridge
{
    /// <summary>
    /// Renders a single mover's visual state. READS from <see cref="MoverState"/>,
    /// WRITES only to Unity presentation objects. Never feeds back into the sim.
    /// </summary>
    public sealed class MoverRenderer : MonoBehaviour
    {
        [SerializeField] private Transform _transform;

        public void UpdateFromState(MoverState state)
        {
            Transform t = _transform != null ? _transform : transform;

            t.position = ToVector3(state.Position);

            // Simulation yaw is around Y, pitch around X; roll is unused.
            t.rotation = Quaternion.Euler(ToFloat(state.Pitch), ToFloat(state.Yaw), 0f);
        }

        private static float ToFloat(Fixed f) => (float)((double)f.Raw / (1L << Fixed.FracBits));

        private static Vector3 ToVector3(FixedVec3 v)
            => new Vector3(ToFloat(v.X), ToFloat(v.Y), ToFloat(v.Z));
    }

    /// <summary>
    /// Renders a single rope's node chain onto a <see cref="LineRenderer"/>.
    /// READS from <see cref="RopeState"/>, WRITES only to the line.
    /// </summary>
    public sealed class RopeRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer _line;

        public void UpdateFromState(RopeState state)
        {
            LineRenderer line = _line != null ? _line : GetComponent<LineRenderer>();
            if (line == null)
            {
                return;
            }

            FixedVec3[] nodes = state.Nodes;
            if (nodes == null)
            {
                line.positionCount = 0;
                return;
            }

            line.positionCount = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                line.SetPosition(i, ToVector3(nodes[i]));
            }
        }

        private static float ToFloat(Fixed f) => (float)((double)f.Raw / (1L << Fixed.FracBits));

        private static Vector3 ToVector3(FixedVec3 v)
            => new Vector3(ToFloat(v.X), ToFloat(v.Y), ToFloat(v.Z));
    }
}
