# Architecture

> The architecture is **deliberately not finalized**. The brief mandates that the engine and
> network topology are *decided by the First Test* (`PROJECT_BRIEF.md` §4), not chosen in
> advance. This document records what is fixed, what is pending the test, and the decision tree.

## Overview

A deterministic, fixed-timestep, rollback-networked movement shooter. The whole simulation is a
pure function of player inputs: same inputs → bit-identical state, every run, on every machine.
Networking ships **inputs only**; each client predicts remote inputs and re-simulates on
misprediction. Every system iterates over `N` entities and never references a specific player,
so 1v1 and 4-player are the same code at different `N`. The signature mechanic — stiff
coupled-spring grapple ropes that physically collide with each other — is the hardest thing to
keep deterministic under rollback and is validated **before** anything else is built.

## Project status

- **Architecture: code-first split in progress (per REFACTOR_GUIDE.md).** `src/Simulation/` is a pure C#/.NET 8 headless layer (validated First-Test rope solver + 5-aspect xUnit tests, run via `dotnet test`); `src/Presentation/Scripts/` is a thin Unity renderer reading `SimulationState`. Quantum ECS systems stay at `Assets/QuantumUser/Simulation/` until the Photon Quantum SDK is restored into the tree.
- **Engine: Path 1 — Unity 6000.3.17f1 + Photon Quantum 3.0.11. VALIDATED by the First Test
  (2026-06-17).** Determinism across runs, rollback re-simulation, and FP expressibility all
  passed in Quantum at 4 ropes / 128 Hz. See [`spike/determinism/FIRST_TEST.md`](../spike/determinism/FIRST_TEST.md)
  Run 2. The build sequence (`PROJECT_BRIEF.md` §5) is unlocked.
- **Tick rate: 128 Hz** (`SessionConfig.UpdateFPS = 128`). Counter-Strike competitive cadence.
- **Aspect #5 frame budget: CLOSED (2026-06-19).** Quantum Profiler measured `RopeSolverSystem`
  avg 0.1490 ms / max 0.1997 ms → depth-8 rollback = 1.598 ms → **1.797 ms of 7.8125 ms (~77%
  headroom)** at the 4-rope / 6-pair worst case. Verdict: **4-player ships day one** (not
  1v1-first). All five aspects now pass in Quantum; Path 1 fully validated.
- **Working title: movement.md.** Repo: https://github.com/SpreitzerJacobGit/movement.md — now a
  single unified repo containing both the Unity project and the design docs/spike. Canonical local
  working copy: `C:\Users\jacob\movement.md` (the Unity+Quantum project). (`C:\Claude\movement.md`
  is a stale secondary copy, retained for now.)

## Component map

All components are count-agnostic systems operating over an entity set of `N` movers.

| Component | Responsibility |
|---|---|
| **Determinism Spike** (`spike/`) | The First Test: 4-rope stiff coupled-spring rope-rope collision solver + 5-aspect validation harness. Gates everything else. |
| **Simulation core** | Fixed-timestep deterministic step; owns the entity set and tick loop. No engine-visual dependency (runs headless as referee). |
| **Movement system** | Sprint / slide / jump / wall-jump / wall-run over entities. |
| **Grapple system** | Stiff coupled-spring ropes + **ID-sorted** rope-rope collision resolution. The steering tool. Highest-risk system. |
| **Spin system** | Records swept *angle* (not angular velocity), inverse-speed weighted; deterministic order-independent stacking; additive discharge. The acceleration tool. |
| **Offense system** | Output scaling by speed + rate-of-change of direction; stillness-gated ability refresh. |
| **Presentation layer** | Perfect-information rendering: through-wall position light, look cone, spin meter, playback tell. Read-only over sim state. |
| **Macro system** | Between-round geometry edit + shared ability pool; identical per-round start state; geometry persists within a match, resets per match. |
| **Netcode** | Input-only transport, remote-input prediction, rollback re-simulation, server-authoritative reconciliation. |
| **App flow** (`_Project/Core`) | Host-layer mode state machine + additive scene orchestration (Boot → Singles/Doubles/Sandbox/Training). Count-agnostic: Singles & Doubles share one Match scene at different N. |
| **Presentation settings** (`_Project/Presentation`) | `OverlaySettings` — perfect-info overlay flags (position light, look cone, spin meter, playback tell). Read-only by the renderer; never feeds sim state. |
| **Dev tooling** (`_Project/Dev`, `_Project/UI/DevMenu`) | F1 Developer Menu + `IDevTool` registry; tools route through the `ISimContext` boundary so they compile before Quantum is installed. |
| **Match flow** (`_Project/Core/Match`) | `MatchController` + `MatchState`: count-agnostic (per-side) first-to-11 / best-of-3 lifecycle with mid-game (at 6) and between-game edit windows. Core-only; UI subscribes to its events. |
| **Macro / place-geometry** (`_Project/Core/Macro`, `_Project/UI/Macro`) | `MacroState` holds placed geometry (persists the whole match, resets per match); the place UI does free-3D mouse placement, one piece per side per edit window. Both sides' pieces are visible (perfect info). |
| **Settings** (`_Project/UI/Settings`) | Display + Audio settings, persisted via PlayerPrefs. |

## Code-first layout

The refactor (per [`REFACTOR_GUIDE.md`](../REFACTOR_GUIDE.md)) splits the codebase into a pure-C# simulation layer and a Unity-only presentation layer. The solution file is [`movement.md.sln`](../movement.md.sln) at the repo root.

### `src/Simulation/` — pure C#/.NET 8, zero Unity/Quantum deps in the tree

Builds and tests without Unity or Photon Quantum installed. `dotnet build` / `dotnet test` from this folder.

- **`Simulation.csproj`** — `net8.0`, `RootNamespace=Simulation`. Contains commented-out `<Reference>` entries for `Quantum.Deterministic.dll` / `Quantum.Engine.dll` that are uncommented only when the Photon Quantum SDK is restored into the tree (see "Current limitations" below).
- **`Core/`** — the simulation contract and reference impl.
  - `ISimulation.cs` — the pure-C# contract (`Tick` / `GetState` / `ApplyInput` / `Snapshot` / `Restore`) plus the render-readable value-type structs (`SimulationState`, `MoverState`, `RopeState`) handed to the presentation layer.
  - `PlayerInputs.cs` — the per-tick input struct (`PlayerInputs`) and the spin-accumulator struct (`FPSpin`). Every field is fixed-point or bool so the struct serializes deterministically.
  - `HeadlessSimulation.cs` — the pure-C# reference `ISimulation` implementation that composes the rope solver. This is the refactor's headline capability: the sim core runs outside any game engine. The Quantum-backed `Simulation` described in REFACTOR_GUIDE.md §1.2 will supersede it once the SDK is restored; both implement `ISimulation` so the presentation layer is agnostic to which one is live.
- **`Math/`** — fixed-point math. `Fixed.cs` (Q32.32 with `Int128` intermediates) and `FixedVec3.cs`, ported from `spike/determinism/`. This is the validated First-Test stand-in for Quantum's `FP` (Q16.16) — same algorithm, different ship precision; see "Current limitations."
- **`Systems/`** — simulation systems. `RopeSolver.cs` is the validated stiff coupled-spring rope-rope collision solver (ID-sorted pairs, frozen-position accumulator, semi-implicit Euler), ported bit-exact from the spike. Movement and Spin systems are pending (see below).
- **`Networking/`** — reserved for input-only transport, remote-input prediction, and rollback reconciliation.
- **`Tests/`** — `Tests.csproj` (xUnit). `Determinism/DeterminismTests.cs` is the 5-aspect First Test ported headless (run determinism, rollback re-sim, pair-order independence, fixed-point expressibility, frame budget). `Systems/SystemTests.cs` covers rope-solver invariants; Spin/Movement tests are skipped placeholders pending those systems. `Integration/` is reserved.

### `src/Presentation/` — Unity-only renderer

Compiles only inside the Unity editor (no `.csproj` here; Unity generates and compiles it). All scripts read `SimulationState` and write Unity transforms/materials/particles — **never** write simulation state.

- **`Scripts/SimBridge/`** — `SimulationBridge`: the boundary that obtains an `ISimulation`, drives `Tick`, and exposes `SimulationState` to renderers.
- **`Scripts/Input/`** — `InputCapture`: samples Unity input into `PlayerInputs` structs and forwards them to the sim via the bridge.
- **`Scripts/Rendering/`** — `MoverRenderer`, `RopeRenderer`, `PerfectInfoRenderer`: read `SimulationState` and write Unity transforms / line renderers / the perfect-info overlay (position light, look cone, spin meter, playback tell).
- **`Scripts/UI/`** — reserved for HUD / menus that subscribe to presentation-layer state.
- **`Scenes/`**, **`Assets/`** — Unity scene and asset forwarding locations.

### Current limitations (stated accurately)

- **Photon Quantum SDK is NOT committed to this repo** (proprietary binaries). It exists at the canonical working copy `C:\Users\jacob\movement.md\Assets\Photon\Quantum\Assemblies\` (`Quantum.Deterministic.dll`, `Quantum.Engine.dll`, `Quantum.Log.dll`). `Simulation.csproj` has commented-out `<Reference>` entries that are filled in when the SDK is restored.
- **Existing Quantum ECS systems** (`MovementSystem`, `GrappleSystem`, `RopeSolverSystem`, `RopeCouplingSystem`) remain at `Assets/QuantumUser/Simulation/` (Unity-side). They are Unity-independent in principle (only need Quantum, not Unity) but compile only inside Unity+Quantum for now. Moving them into `src/Simulation/` is deferred until the SDK is available (guide Phase 1.3 / 3.1).
- **Spin and Offense systems do not exist yet** (design-only float proxies live in `Assets/_Project/Dev/Tunables.cs`). Tests for them in `src/Simulation/Tests/Systems/` are skipped placeholders.
- **`Fixed` (Q32.32) is not bit-identical to Quantum's `FP` (Q16.16).** The spike's `Fixed` proves the *algorithm* is deterministic; Quantum's `FP` proves expressibility at ship precision. Both runs are documented in [`spike/determinism/FIRST_TEST.md`](../spike/determinism/FIRST_TEST.md).

## Key decisions

### Decision 1: The engine is selected by the First Test, not chosen now
**Decision:** Do not commit an engine. Build the determinism spike (stiff coupled-spring
rope-rope solver) **inside Photon Quantum's fixed-point math** at **four ropes**, run the
5-aspect evaluation, and let the result pick the path.

**Rationale:** The signature mechanic and the engine carry the same risk (determinism of stiff
coupled springs under rollback). One experiment answers both: if it passes in Quantum, the
feature and Path 1 are validated together; if Quantum can't express it, that failure is the
evidence justifying the cost of Path 2.

**Alternatives considered:**
- **Path 1 — Unity + Photon Quantum** (deterministic ECS, fixed-point physics, rollback +
  input networking built in; sim has no Unity dependency, runs headless as referee). *Preferred
  if the test passes inside Quantum.* Caveat: the coupled-spring rope solver is not built in and
  Quantum's 3D physics is less mature than 2D.
- **Path 2 — Rust (Bevy + Rapier + GGRS / lightyear)** (own every float; rope solver is yours
  to make deterministic). *Justified only if the test fails inside Quantum* — enormous surface
  area, you own all determinism bugs.
- **Path 3 — Fully custom fixed-point engine.** Ruled out unless Paths 1 and 2 both fail
  (overengineering at this stage).

**Decision tree (from the First Test result):**
- All 5 aspects pass → **Path 1**; proceed to build sequence.
- Determinism passes but budget only fits 2 ropes → ship **1v1 first**; 4-player rope collision
  becomes a later optimization or constrained mode.
- Quantum can't express the solver, or determinism fails in Quantum → re-run the same 5-aspect
  test in **Path 2 (Rust)** before committing.
- Both fail → redesign around rope-rope collision (impulse-based, fixed iteration count, or drop
  it) — on evidence, not before.

### Decision 2: Bit-exact determinism is a hard precondition, not a feature
Same inputs must produce bit-identical state across runs and across rollback re-simulation.
Everything downstream (rollback, server authority, replays) depends on it. **Resolve collision
pairs in a stable order sorted by entity ID — never iteration order** (hash/spatial-hash
ordering is run-dependent and stiff springs amplify it into a full desync).

### Decision 3: Count-agnostic simulation
No system assumes a player count. Systems iterate `N` movers. 1v1 and 4-player are the same code
at different `N`. Rope-rope pairs scale as n-choose-2 (1 pair at 2 players, **6 at 4**), and
rollback frequency rises with remote player count — the product is the true performance ceiling.

### Decision 4: Network topology falls out of the test, not chosen now
- 1v1 → peer-to-peer rollback, each client predicts one opponent, server optional as referee.
- 4-player → authoritative server runs the truth sim at worst-case 4-rope load; clients predict
  and roll back against it. Still input-only, so bandwidth stays trivial; the **server** carries
  the physics cost.

### Decision 5: Hosting deferred and swappable
Input-only netcode means a featherweight server (relay/validate inputs, optional referee sim).
Casual play may not need dedicated per-match servers (P2P + referee); reserve dedicated servers
for ranked/4-player. Container options (Edgegap, Gameye, GameFabric, AWS GameLift) all take a
Docker image + REST call, so they stay swappable. **Decide late.** (Note: Unity Multiplay ended
direct support 2026-03-31; Hathora shut down 2026-05-05 — do not lock a provider early.)

## Data flow

1. Each client samples local **input** for the tick (movement, aim, spin record/discharge, fire,
   macro edits between rounds).
2. Inputs are exchanged (P2P or via server). Missing remote inputs are **predicted**.
3. The deterministic sim advances one fixed tick over all `N` entities: movement → grapple
   (ID-sorted rope-rope collision) → spin → offense → macro/state.
4. On a misprediction, the client **rolls back** to the last confirmed tick and **re-simulates**
   forward with corrected inputs. Re-sim must be bit-identical.
5. The server (when present) runs the same sim as authority; clients reconcile to it.
6. The presentation layer renders sim state read-only — no gameplay state lives in the renderer.

## App flow & UI (host/renderer layer — groundwork)

The Unity host now carries a **shell** (no sim yet) so the UI and dev tooling are in place before
the sim systems are built. See [`Assets/_Project/README.md`](../Assets/_Project/README.md).

- **Entry flow:** traditional funnel for now — Boot → Main Menu → mode select (Singles / Doubles /
  Sandbox / Training). The brief's "sandbox-as-hub that morphs into a match" (§1.4) is a later
  polish goal, not this shell.
- **Scene topology:** one always-loaded Boot scene; each mode loads **additively** and swaps out.
  Boot holds `AppFlow`, the menus, the HUD, and the dev tooling.
- **UI tech (hybrid):** UI Toolkit for the Main Menu + Developer menu; uGUI Canvas for the thin
  in-match HUD. Perfect-info rendering will be world-space meshes reading `OverlaySettings`.
- **Count-agnosticism holds in the UI:** modes are defined by a player-count config, not
  `player1`/`player2`. Singles and Doubles run the same Match scene at N=2 / N=4.
- **Sim boundary:** `SimHost.Current` (`ISimContext`) defaults to `NullSimContext`, which fails fast
  with a clear pending status. Photon Quantum installs the real context via `SimHost.Set(...)` later;
  dev tools then come online with no UI changes.
- **Tunables** are float proxies for feel-tuning/presentation only — the sim consumes fixed-point;
  conversion happens at the sim boundary, never inside the step.

## External dependencies

| Dependency | Purpose | Status |
|---|---|---|
| Photon Quantum 3.0.x | Deterministic ECS, fixed-point physics, rollback + input networking | Candidate (Path 1); validated/​rejected by the First Test |
| Unity (LTS) | Host/renderer for Quantum; sim runs Unity-independent and headless | Candidate (Path 1) |
| Rust (Bevy + Rapier + GGRS/lightyear) | Fallback deterministic stack | Path 2, only if Quantum fails |
| Container host (Edgegap / Gameye / GameFabric / GameLift) | Dedicated server orchestration when needed | Deferred, swappable |
