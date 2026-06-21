# AI Agent Guidelines

Operating rules for AI assistants (Claude Code, etc.) working in this repo after the code-first
refactor. Read this together with [`ARCHITECTURE.md`](./ARCHITECTURE.md) and
[`REFACTOR_GUIDE.md`](../REFACTOR_GUIDE.md).

## Layout at a glance

- **`src/Simulation/`** — pure C#/.NET 8 simulation. Zero Unity, zero Photon Quantum dependencies
  in the tree. Builds and tests headless via `dotnet`.
- **`src/Presentation/`** — Unity-only thin renderer. Reads `SimulationState`, writes Unity
  transforms. Compiles inside the Unity editor only (no `.csproj` here).
- **`Assets/QuantumUser/Simulation/`** — existing Photon Quantum ECS systems (`MovementSystem`,
  `GrappleSystem`, `RopeSolverSystem`, `RopeCouplingSystem`). Unity-side until the Quantum SDK is
  restored; see "Current limitations."

## What to edit

**DO EDIT:**
- `src/Simulation/Core/` — the `ISimulation` contract, `HeadlessSimulation`, `PlayerInputs`.
- `src/Simulation/Systems/` — simulation systems (e.g. `RopeSolver.cs`). New systems go here.
- `src/Simulation/Math/` — fixed-point math (`Fixed`, `FixedVec3`).
- `src/Simulation/Networking/` — input-only transport, rollback.
- `src/Simulation/Tests/` — xUnit coverage. Add tests here *before* touching sim code.
- `src/Presentation/Scripts/Rendering/` — visual effects, renderers (`MoverRenderer`,
  `RopeRenderer`, `PerfectInfoRenderer`).
- `src/Presentation/Scripts/Input/` — `InputCapture` and input shaping.
- `src/Presentation/Scripts/UI/` — HUD / menus that read presentation-layer state.

**DO NOT EDIT** (without an architectural reason, surfaced first):
- `src/Presentation/Scripts/SimBridge/` — the `SimulationBridge` boundary layer. This is the seam
  between Unity and the pure sim; changing its shape is a structural decision.
- Unity scene files (`.unity`) — binary and fragile.
- Unity prefab files (`.prefab`) — edit the component scripts, not the prefab assets.
- `Assets/QuantumUser/Simulation/` Quantum ECS systems — still the live game systems in Unity.
  Porting them into `src/Simulation/` is guide Phase 1.3 / 3.1, gated on the SDK.
- Any file that writes **to** simulation state from Unity — this violates the core pattern.

## Code patterns

**Simulation code** (`src/Simulation/`):
- Pure C#. No `using UnityEngine;` / `using UnityEditor;`. No `using Photon.*;`.
- All math is fixed-point via `Simulation.Math.Fixed` (Q32.32) and `FixedVec3`. No `float` /
  `double` in simulation logic.
- Systems are deterministic: same inputs → bit-identical outputs. Iterate pairs in ID-sorted order,
  never hash/iteration order.
- Truth lives inside the `ISimulation` implementation; the presentation layer only sees the
  `SimulationState` value-type snapshot returned from `GetState()`.

**Presentation code** (`src/Presentation/Scripts/`):
- Reads `SimulationState` / `MoverState` / `RopeState` (pure C# value types).
- Writes to Unity `Transform`, `LineRenderer`, materials, particles — never to sim state.
- Input capture packs a `PlayerInputs` struct and forwards it through `SimulationBridge`; the sim
  is the only writer of sim truth.

**Testing** (`src/Simulation/Tests/`):
- All sim tests run headless: `dotnet test` from `src/Simulation/Tests/` (seconds, not Unity Play
  Mode minutes).
- Test invariants, edge cases, and determinism (same inputs → identical state across runs and
  across rollback re-sim).
- Use `Assert.Equal` for bit-identical comparisons.
- No Unity Play Mode tests for simulation logic — Play Mode is for visual verification only.

## Common patterns

**Adding a new simulation system:**
1. Write a failing test in `src/Simulation/Tests/Systems/` that pins the invariant you want.
2. Implement `src/Simulation/Systems/NewSystem.cs` in pure C# with fixed-point math.
3. Wire it into the `Tick` order inside `HeadlessSimulation` (and, later, the Quantum-backed
   `Simulation`).
4. `dotnet test` from `src/Simulation/Tests/` until green.
5. Expose any renderer-needed fields via `SimulationState` / `MoverState` / `RopeState` (pure value
   types).
6. Add the renderer in `src/Presentation/Scripts/Rendering/` (Unity-side, read-only).

**Fixing a bug:**
1. Reproduce headlessly: write a failing test in `src/Simulation/Tests/`.
2. Fix the bug in `src/Simulation/`.
3. Verify `dotnet test` passes.
4. Visually verify in Unity Play Mode only if the bug had a visible symptom.

**Adding a new visual effect:**
1. If the sim needs to track it, add the field to `SimulationState` / `MoverState` / `RopeState`
   and populate it inside the relevant system (pure C#, fixed-point).
2. Add the renderer in `src/Presentation/Scripts/Rendering/`, reading the new field read-only.
3. Visual verification happens in Unity Play Mode — no sim test needed unless the underlying state
   has an invariant worth pinning.

## Current limitations

Be aware of these before editing — they explain why some files you'd expect aren't here:

- **Photon Quantum SDK is NOT committed** to this repo (proprietary binaries). It lives at the
  canonical working copy `C:\Users\jacob\movement.md\Assets\Photon\Quantum\Assemblies\`
  (`Quantum.Deterministic.dll`, `Quantum.Engine.dll`, `Quantum.Log.dll`). `Simulation.csproj`
  carries commented-out `<Reference>` entries that are filled in when the SDK is restored.
- **Existing Quantum ECS systems** (`MovementSystem`, `GrappleSystem`, `RopeSolverSystem`,
  `RopeCouplingSystem`) live at `Assets/QuantumUser/Simulation/` and compile only inside
  Unity+Quantum. They are Unity-independent in principle (need only Quantum, not Unity) but their
  move into `src/Simulation/` is deferred until the SDK is available.
- **Spin and Offense systems do not exist yet.** Design-only float proxies live in
  `Assets/_Project/Dev/Tunables.cs`. Tests for them in `src/Simulation/Tests/Systems/` are skipped
  placeholders. Do not implement them speculatively.
- **`Fixed` (Q32.32, `Int128` intermediates) is not bit-identical to Quantum's `FP` (Q16.16).**
  The local `Fixed` proves the *algorithm* is deterministic; Quantum's `FP` proves expressibility
  at ship precision. Both runs are documented in
  [`spike/determinism/FIRST_TEST.md`](../spike/determinism/FIRST_TEST.md). When the SDK lands, the
  Quantum-backed `Simulation` replaces `HeadlessSimulation` as the live impl; both implement
  `ISimulation`.
