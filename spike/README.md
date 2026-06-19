# `spike/` — The First Test (the gate)

This directory holds the **determinism spike**, which `PROJECT_BRIEF.md` requires to run and pass
*before any other build work begins*. It proves-or-kills the signature mechanic (stiff
coupled-spring ropes that collide with each other) and selects the engine, in one experiment.

Full design and pass/fail criteria: [`determinism/FIRST_TEST.md`](./determinism/FIRST_TEST.md).

## What's here

| File | Role |
|---|---|
| `determinism/Fixed.cs`, `FixedVec3.cs` | Q32.32 deterministic fixed-point (stand-in for Quantum `FP`/`FPVector3`) |
| `determinism/RopeSolver.cs` | The subject: 4-rope stiff coupled-spring + **ID-sorted** rope-rope collision |
| `determinism/DeterminismHarness.cs` | The 5-aspect test (runs, rollback, pair-order, expressibility, frame budget) |
| `determinism/RopeSim.qtn` | How the solver maps onto Quantum ECS components (port target) |
| `determinism/Determinism.csproj` | .NET 8 console project for the local de-risk run |
| `determinism/FIRST_TEST.md` | Runbook + decision tree + results table to fill in |

## Why a local .NET harness exists at all

The brief says build the spike *inside Quantum* — and that remains the test that counts (it is the
only thing that validates Aspect #4, Quantum-FP expressibility). But neither Unity, Quantum, nor a
.NET SDK is installed on this machine yet. The local harness lets the **algorithm** be proven
deterministic, rollback-safe, and pair-order-independent (Aspects 1-3, provisional 5) the moment a
.NET SDK exists — *before* paying the cost of a Unity+Quantum setup. If determinism fails locally,
the algorithm is wrong and Quantum can't rescue it; fix it here first.

## Stage 1 — Local de-risk (.NET only)

1. Install the **.NET 8 SDK (LTS)** — https://dotnet.microsoft.com/download/dotnet/8.0
   (`Int128`, used by the fixed-point type, needs .NET 7+).
2. Verify: `dotnet --version` (expect `8.x`).
3. Run:
   ```sh
   cd spike/determinism
   dotnet run -c Release
   ```
4. Read the printed report. Record Aspects 1-3 and the provisional 5 in `FIRST_TEST.md`. **A
   determinism failure here blocks everything — stop and fix the algorithm.**

## Stage 2 — The real test (Unity + Photon Quantum)

This is the result that selects the engine.

1. Install **Unity Hub** + a **Unity LTS** editor — https://unity.com/download
2. Get **Photon Quantum 3.0.x** (free for development): create an app in the Photon dashboard
   (https://dashboard.photonengine.com), download the Quantum 3.0.x SDK, and import it into a new
   Unity project. (Quantum SDK access is gated behind a Photon account — this step needs your
   login; I can't do it headless.)
3. Define the components in `determinism/RopeSim.qtn` in the Quantum project.
4. Port the body of `RopeSolver.Step` into a Quantum `SystemMainThread`, swapping `Fixed`→`FP` and
   `FixedVec3`→`FPVector3`. Keep it count-agnostic: iterate `f.Filter<Rope>()`, never assume two
   ropes. Lock the session `DeltaTime` to **128 Hz**.
5. Re-implement the five aspects against Quantum's frame snapshots (Quantum provides frame
   serialization + rollback natively, so Aspects 1, 2, 5 lean on engine facilities).
6. Record the full result and the decision in `FIRST_TEST.md`. Follow the decision tree.

## The gate

```
First Test passes in Quantum  ──▶  build sequence unlocked (PROJECT_BRIEF.md §5)
First Test fails              ──▶  follow FIRST_TEST.md decision tree (Path 2 / redesign)
```

Do not start movement, spin, offense, macro, or netcode work until the results table in
`FIRST_TEST.md` is filled and a decision is recorded.
