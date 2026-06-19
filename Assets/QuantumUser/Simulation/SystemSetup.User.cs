namespace Quantum
{
    using System;
    using System.Collections.Generic;

    public static partial class DeterministicSystemSetup
    {
        static partial void AddSystemsUser(ICollection<SystemBase> systems, RuntimeConfig gameConfig, SimulationConfig simulationConfig, SystemsConfig systemsConfig)
        {
            // First Test (Stage 2): spawn must run before the solver so entities exist at frame 0.
            systems.Add(new RopeSpawnSystem());
            systems.Add(new RopeSolverSystem());
        }
    }
}