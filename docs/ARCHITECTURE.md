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

- **Engine: Path 1 — Unity 6000.3.17f1 + Photon Quantum 3.0.11. VALIDATED by the First Test
  (2026-06-17).** Determinism across runs, rollback re-simulation, and FP expressibility all
  passed in Quantum at 4 ropes / 128 Hz. See [`spike/determinism/FIRST_TEST.md`](../spike/determinism/FIRST_TEST.md)
  Run 2. The build sequence (`PROJECT_BRIEF.md` §5) is unlocked.
- **Tick rate: 128 Hz** (`SessionConfig.UpdateFPS = 128`). Counter-Strike competitive cadence.
- **Open: Aspect #5 frame budget.** Per-tick `RopeSolverSystem` cost vs the 7.8125 ms budget is
  still to be read from the Quantum Profiler; it decides 4-player day-one vs. 1v1-first, not
  engine viability.
- **Working title: movement.md.** Repo: https://github.com/SpreitzerJacobGit/movement.md (the
  Unity project at `C:\Users\jacob\movement.md`; this docs/spike folder at `C:\Claude\movement.md`
  is currently separate and not yet in the repo).

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

## External dependencies

| Dependency | Purpose | Status |
|---|---|---|
| Photon Quantum 3.0.x | Deterministic ECS, fixed-point physics, rollback + input networking | Candidate (Path 1); validated/​rejected by the First Test |
| Unity (LTS) | Host/renderer for Quantum; sim runs Unity-independent and headless | Candidate (Path 1) |
| Rust (Bevy + Rapier + GGRS/lightyear) | Fallback deterministic stack | Path 2, only if Quantum fails |
| Container host (Edgegap / Gameye / GameFabric / GameLift) | Dedicated server orchestration when needed | Deferred, swappable |
