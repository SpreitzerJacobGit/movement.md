using UnityEngine;
using Simulation.Core;

namespace Presentation.SimBridge
{
    /// <summary>
    /// Bridge between the headless pure-C# simulation and the Unity renderer.
    /// Drives the fixed-timestep tick, then maps read-only simulation state onto
    /// Unity transforms each frame.
    /// </summary>
    /// <remarks>
    /// READ-ONLY INVARIANT: this component reads simulation truth and writes only to
    /// Unity presentation objects. It never mutates <see cref="SimulationState"/>;
    /// the sole path back into the sim is <see cref="SendPlayerInputs"/> -> ApplyInput.
    /// </remarks>
    public sealed class SimulationBridge : MonoBehaviour
    {
        private ISimulation _simulation;

        [Header("Render Mapping")]
        [SerializeField] private MoverRenderer[] _moverRenderers;
        [SerializeField] private RopeRenderer[] _ropeRenderers;

        [SerializeField] private int _tickRate = 128;

        private double _accumulator;

        private double TickInterval => 1.0 / _tickRate;

        /// <summary>
        /// Install the simulation to drive. Null is rejected loudly rather than
        /// silently leaving the bridge idle, so a wiring mistake fails fast.
        /// </summary>
        public void SetSimulation(ISimulation simulation)
        {
            if (simulation == null)
            {
                Debug.LogError($"{nameof(SimulationBridge)}: refused null simulation.");
                return;
            }

            _simulation = simulation;
        }

        private void Update()
        {
            if (_simulation == null)
            {
                return;
            }

            // Fixed-timestep accumulator: the sim must tick at exactly _tickRate Hz
            // regardless of Unity's variable frame rate, or determinism breaks.
            _accumulator += Time.deltaTime;
            while (_accumulator >= TickInterval)
            {
                _simulation.Tick();
                _accumulator -= TickInterval;
            }

            RenderSimulationState();
        }

        private void RenderSimulationState()
        {
            var state = _simulation.GetState();

            MoverState[] movers = state.Movers;
            if (movers != null)
            {
                int count = _moverRenderers != null ? System.Math.Min(movers.Length, _moverRenderers.Length) : 0;
                for (int i = 0; i < count; i++)
                {
                    _moverRenderers[i].UpdateFromState(movers[i]);
                }
            }

            RopeState[] ropes = state.Ropes;
            if (ropes != null)
            {
                int count = _ropeRenderers != null ? System.Math.Min(ropes.Length, _ropeRenderers.Length) : 0;
                for (int i = 0; i < count; i++)
                {
                    _ropeRenderers[i].UpdateFromState(ropes[i]);
                }
            }
        }

        public void SendPlayerInputs(PlayerInputs inputs)
        {
            _simulation?.ApplyInput(inputs);
        }

        public SimulationState GetState() => _simulation.GetState();
    }
}
