# Presentation

Unity-only thin renderer for the simulation layer. Compiles **inside the Unity editor only** —
there is no `.csproj` here; Unity generates and compiles these scripts. These scripts read
`SimulationState` and write Unity transforms / materials / particles. They **never** write
simulation state. See [`../../REFACTOR_GUIDE.md`](../../REFACTOR_GUIDE.md) and
[`../../docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md) for the full design.

## Layout

| Folder | Contents |
|---|---|
| `Scripts/SimBridge/` | `SimulationBridge` — the boundary. Obtains an `ISimulation`, drives `Tick`, and exposes `SimulationState` to renderers. This is the only seam between Unity and the pure sim; treat its shape as a structural decision. |
| `Scripts/Input/` | `InputCapture` — samples Unity input into `PlayerInputs` structs and forwards them to the sim via the bridge. |
| `Scripts/Rendering/` | `MoverRenderer`, `RopeRenderer`, `PerfectInfoRenderer` — read `SimulationState` and write Unity transforms, line renderers, and the perfect-info overlay (position light, look cone, spin meter, playback tell). |
| `Scripts/UI/` | Reserved for HUD / menus that subscribe to presentation-layer state. |
| `Scenes/` | Unity scene forwarding location. |
| `Assets/` | Unity asset forwarding location. |

## The read-only rule

This layer reads `SimulationState`, `MoverState`, and `RopeState` (pure-C# value types defined in
[`../Simulation/Core/ISimulation.cs`](../Simulation/Core/ISimulation.cs)). Those structs are
`readonly` precisely so the renderer cannot mutate sim truth. The only thing this layer writes to
the sim is a `PlayerInputs` struct handed to the bridge — never a direct mutation of sim state.

## Rules for editors

- Read `SimulationState`; write Unity `Transform`, `LineRenderer`, materials, particles.
- Never reference sim internals beyond the `ISimulation` contract + the `*State` value types.
- No `.csproj` here — Unity compiles these. Use the Unity editor for build/inspect.
- Visual verification happens in Unity Play Mode. Simulation correctness is verified headlessly in
  `../Simulation/Tests/`, not here.
- See [`../../docs/AI_GUIDELINES.md`](../../docs/AI_GUIDELINES.md) for the full do/don't list.
