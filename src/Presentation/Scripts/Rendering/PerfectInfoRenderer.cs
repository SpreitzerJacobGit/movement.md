using UnityEngine;
using Simulation.Core;
using Simulation.Math;

namespace Presentation.Rendering
{
    /// <summary>
    /// Perfect-information overlays (per REFACTOR_GUIDE Phase 2.3): through-wall
    /// position lights, look cones, spin meters. READS sim state only.
    /// </summary>
    public sealed class PerfectInfoRenderer : MonoBehaviour
    {
        [SerializeField] private SimulationBridge _simulationBridge;
        [SerializeField] private Transform[] _positionLights;

        private void Update()
        {
            if (_simulationBridge == null)
            {
                return;
            }

            SimulationState state = _simulationBridge.GetState();
            MoverState[] movers = state.Movers;
            if (movers == null || _positionLights == null)
            {
                return;
            }

            int count = Mathf.Min(movers.Length, _positionLights.Length);
            for (int i = 0; i < count; i++)
            {
                _positionLights[i].position = ToVector3(movers[i].Position);
            }

            // TODO: look-cone overlay (from movers[i].Yaw/Pitch) and spin-meter
            // overlay (from movers[i].Spin). Spin is design-only for now; these
            // are deferred until the visual design is locked.
        }

        private static float ToFloat(Fixed f) => (float)((double)f.Raw / (1L << Fixed.FracBits));

        private static Vector3 ToVector3(FixedVec3 v)
            => new Vector3(ToFloat(v.X), ToFloat(v.Y), ToFloat(v.Z));
    }
}
