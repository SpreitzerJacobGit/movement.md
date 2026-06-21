# Simulation

Pure C#/.NET 8 headless simulation layer. **Zero Unity, zero Photon Quantum dependencies in this
tree** — it builds and tests with plain `dotnet`. This is the refactor's headline capability: the
simulation core runs outside any game engine. See
[`../../REFACTOR_GUIDE.md`](../../REFACTOR_GUIDE.md) and
[`../../docs/ARCHITECTURE.md`](../../docs/ARCHITECTURE.md) for the full design.

## Layout

| Folder | Contents |
|---|---|
| `Core/` | The `ISimulation` contract (`Tick` / `GetState` / `ApplyInput` / `Snapshot` / `Restore`), the render-readable `SimulationState` / `MoverState` / `RopeState` value types, the `PlayerInputs` input struct, and `HeadlessSimulation` — the pure-C# reference impl composing the rope solver. |
| `Math/` | Fixed-point math: `Fixed` (Q32.32, `Int128` intermediates) and `FixedVec3`. Ported from `spike/determinism/`. |
| `Systems/` | Simulation systems. `RopeSolver.cs` is the validated stiff coupled-spring rope-rope collision solver (ID-sorted pairs, frozen-position accumulator, semi-implicit Euler), ported bit-exact from the spike. Movement / Spin / Offense pending. |
| `Networking/` | Reserved for input-only transport, remote-input prediction, and rollback reconciliation. |
| `Tests/` | xUnit test project (`Tests.csproj`). `Determinism/DeterminismTests.cs` is the 5-aspect First Test ported headless; `Systems/SystemTests.cs` covers rope-solver invariants (Spin/Movement skipped, pending). |

## Build & test

```pwsh
dotnet build               # from this folder
dotnet test                # from Tests/
```

`dotnet test` runs the headless First Test + system tests in seconds (vs. minutes in Unity Play
Mode). This fast loop is the reason the layer exists.

## Current limitations

- **Photon Quantum SDK is not committed** to this repo (proprietary binaries). `Simulation.csproj`
  carries commented-out `<Reference>` entries for `Quantum.Deterministic.dll` /
  `Quantum.Engine.dll`; uncomment them only when the SDK is restored to the tree. The Quantum-backed
  `Simulation` described in REFACTOR_GUIDE.md §1.2 then replaces `HeadlessSimulation` as the live
  impl — both implement `ISimulation`.
- **`Fixed` (Q32.32) is not bit-identical to Quantum's `FP` (Q16.16).** The local `Fixed` proves the
  *algorithm* is deterministic; Quantum's `FP` proves expressibility at ship precision. Both runs
  are documented in `../../spike/determinism/FIRST_TEST.md`.
- **Spin / Offense systems do not exist yet.** Their tests in `Tests/Systems/` are skipped
  placeholders. Do not implement them speculatively.

## Rules for editors

- Pure C# only — no `using UnityEngine;`, no `using UnityEditor;`, no `using Photon.*;`.
- All math is fixed-point. No `float` / `double` in simulation logic.
- Systems must be deterministic: same inputs → bit-identical outputs. Iterate pairs in ID-sorted
  order, never hash/iteration order.
- Write the failing test in `Tests/` before touching `Systems/`. See
  [`../../docs/AI_GUIDELINES.md`](../../docs/AI_GUIDELINES.md).
