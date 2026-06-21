namespace Quantum
{
    using System;
    using System.Collections.Generic;

    public static partial class DeterministicSystemSetup
    {
        static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
        {
            // Init-time spawn (order among OnInit systems is not significant).
            systems.Add(new MoverSpawnSystem());      // §5.1 count-agnostic player entity + MovementConfig
            systems.Add(new RopeSpawnSystem());       // RopeSolverConfig singleton (First-Test ropes retired)

            // Per-tick order: grapple input -> move player (with last tick's grapple pull) -> couple ropes
            // to players -> solve ropes (stash new pull). 1-tick latency on the pull, deterministic.
            systems.Add(new GrappleSystem());
            systems.Add(new MovementSystem());
            systems.Add(new RopeCouplingSystem());
            systems.Add(new RopeSolverSystem());
        }
    }
}