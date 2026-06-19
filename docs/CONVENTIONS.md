# Conventions

> Engine-specific tooling is **pending the First Test**. Conventions that depend on the engine
> are marked TBD with the trigger that resolves them. The determinism-related conventions below
> are **not** negotiable regardless of engine — they exist to protect bit-exactness.

## Language & runtime

- **TBD until the First Test selects the engine.**
  - Path 1 (preferred): **C# inside Photon Quantum 3.0.x** (deterministic ECS, fixed-point
    math), hosted by **Unity LTS**. Simulation code stays Unity-independent so it can run
    headless as the referee.
  - Path 2 (fallback): **Rust** (Bevy + Rapier + GGRS/lightyear).
- **None of these toolchains are installed yet.** Setup is documented in
  [`spike/README.md`](../spike/README.md).
- **Simulation tick rate: 128 Hz (provisional).**

## Determinism rules (apply on every engine — load-bearing)

- **Never use floating point in the simulation step.** Use the engine's fixed-point type
  (Quantum `FP`/`FPVector3`, or a fixed-point crate in Rust). Floats are allowed only in the
  presentation layer, which never feeds back into sim state.
- **Resolve all collision/interaction pairs in a stable order sorted by entity ID.** Never
  iterate hash maps or spatial-hash buckets to drive resolution order.
- **No wall-clock time, no `Random` without a seeded deterministic source, no frame-timing- or
  DPI-dependent inputs** in the sim. Spin integrates *swept angle*, never angular velocity.
- **Fixed timestep only.** No variable substepping inside the sim step.
- **The sim is a pure function of inputs.** No reads from rendering, OS, or network state inside
  the step.

## Naming

| Scope | Convention | Example |
|---|---|---|
| Entities/movers | Count-agnostic; iterate `N`. **Never** `player1`/`player2` | `for each mover in movers` |
| Systems | `<Domain>System`, single responsibility | `GrappleSystem`, `SpinSystem` |
| Fixed-point quantities | Suffix or type makes fixed-point explicit | `FP storedSpinAngle` |
| Sim vs. view | Sim types carry no rendering data; view reads sim | `SpinState` (sim) vs `SpinMeterView` |
| Files | Match engine idiom (PascalCase `.cs` for Quantum; snake_case `.rs` for Rust) | `GrappleSystem.cs` |

## Error handling

- **Fail fast.** Throw on violated preconditions rather than limping forward with bad state — a
  silent determinism violation is far worse than a crash.
- **Redundant checks with descriptive errors at boundaries** (system entry points, input
  decode, rollback re-entry).
- **No fallbacks.** One correct path; do not add a "compatible" alternate route around an error.
- Determinism asserts (e.g. "pair order is ID-sorted", "no float entered sim state") should be
  **hard assertions in debug builds**, surfaced loudly.

## File & folder structure

```
movement.md/
├── PROJECT_BRIEF.md        # authoritative starting brief — read first
├── CLAUDE.md               # AI entry point / index
├── docs/                   # REQUIREMENTS / ARCHITECTURE / CONVENTIONS / WORKFLOWS / REVIEWERS
├── spike/                  # the First Test — determinism spike + setup guide (gates all build work)
│   └── determinism/        # 4-rope solver + 5-aspect harness
└── (engine project)        # created only AFTER the First Test selects the engine
```

## Testing

- **The First Test is the project's foundational test.** Its 5 aspects (determinism across runs,
  determinism across rollback, collision-pair order independence, Quantum expressibility, frame
  budget under load) are the bar; see [`spike/determinism/FIRST_TEST.md`](../spike/determinism/FIRST_TEST.md).
- **Test at the worst case you ship: four ropes (up to six pairs)** — never two. Two ropes pass
  clean while hiding pair-order nondeterminism.
- Determinism tests are **bit-for-bit diffs**, not tolerance comparisons.
- New sim systems must ship with a determinism test (run-to-run + rollback re-sim) before they
  are considered done.

## Dependencies

- **Minimize.** "Don't overengineer; simple beats complex." Prefer the engine's built-ins over
  new libraries.
- Any dependency added to the **sim** must be vetted for determinism (no internal float/wall-time
  nondeterminism). When in doubt, write it inside fixed-point yourself.
- The renderer/presentation layer may use ordinary libraries freely — it never feeds sim state.

## Implementation principles (from the brief — apply throughout)

Don't overengineer · No fallbacks · One way · Clarity over compatibility · Throw / fail fast ·
Redundant boundary checks · Separation of concerns · Surgical changes only · Evidence-based
debugging · **Detective method**: form a theory, gather evidence, fix only once evidence proves
it. The First Test is this principle applied to the whole project.
