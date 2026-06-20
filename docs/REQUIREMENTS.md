# Requirements

> Source of truth: [`PROJECT_BRIEF.md`](../PROJECT_BRIEF.md). This file restates the brief
> as enforceable requirements. Where the brief and this file disagree, the brief wins —
> fix this file.

## Product summary

A perfect-information, mirror-match movement shooter. Both players see everything; the only
hidden variable is *the future*. Skill is the ability to predict and shape the emergent
result of two fully-visible, deterministic physical systems colliding at speed. Low skill
floor, high skill ceiling, across three layers: micro (execution), meso (reading/adaptation —
**not bluffing**), macro (shaping a symmetric arena so your execution wins inside it).

## Functional requirements

### Movement (always free — never gated, never on cooldown)
- The system shall provide sprint, slide, jump, wall-jump, and wall-run.
- The system shall provide a **grapple** modeled as a stiff rubber-band (spring), not a fixed
  rope. Grapples are instantaneously-created physical ropes that **collide with each other**
  and deflect both trajectories on contact. The grapple is the **steering** tool.
- The system shall provide **spin** as the **acceleration** tool: holding a button while
  rotating the camera records swept angle as stored spin, which discharges into momentum over
  time as an **additive** vector.

### Spin rules (exact, load-bearing — verify each in tests)
- Stored spin shall be measured by **total angle swept, not angular velocity** (integrate
  angle). Identical intended motion must produce identical spin regardless of camera speed,
  DPI, or frame timing.
- Record and playback shall be **separable by player choice** (bank in safety, discharge in
  combat).
- Recording shall be **inverse-speed**: slower while recording → greater stored effect.
- Stacking shall be **order-independent and deterministic**: `A then B == B then A`,
  reproducible from momentum-at-record plus recorded camera motion.
- **Spin generation is a complex, ruleset-defined function.** Inputs: the mover's current
  velocity, acceleration, and recorded camera (mouse) motion. Output: a velocity +
  acceleration + impulse effect applied on discharge, triggered by the player on command.
  - The function is **ruleset-level config, identical for both players** (mirror-match
    invariant). It is **not per-player config**.
  - The function parameterizes spin **within** the load-bearing rules above — it does not
    override the determinism invariants (angle integration, inverse-speed, order-independence).
  - This extends the original "additive vector over time" discharge to include an **impulse**
    component (instantaneous momentum change alongside continuous velocity/acceleration).
  - **Verify in the determinism test:** "recorded camera motion" must mean angular
    displacement (consistent with the angle-integration rule), NOT raw mouse velocity, or it
    reintroduces DPI/frame-timing dependence. The velocity/acceleration inputs must be
    sim-state-derived only (no wall-clock, no render-rate leakage). The impulse output must
    remain order-independent and bit-reproducible across rollback re-sim.
- Spin state shall be shown as an **abstract geometric meter** driven by the current ruleset's
  spin-generation function.

### Offense
- Offensive output shall **scale with speed and rate-of-change of direction**.
- Special/offensive abilities shall **refresh only when standing still** (flicker-fast).
  Movement shall never gate on stillness.

### Perfect information (deliberate — do not weaken)
- Opponent position shall emit a light visible **through all geometry**.
- Look-direction shall render as a visible **cone**.
- Spin recording shall be visible; playback shall have a **tell**.
- "A tell for pretty much everything." **No hidden-information mechanics may be added.**

### Match & macro structure
- Players spawn into a **sandbox** that morphs to load a match.
- A match shall be a **series of rounds**.
- Between rounds, each player shall choose exactly one: add/remove a piece of **geometry**, OR
  add/remove an **ability**.
- The ability pool shall be **shared** — removing an ability removes it for both players this
  round (opponent-denial is intended).
- Geometry shall **persist across rounds within a match** and **reset per match**.
- **Both players shall start every round in the exact same state.** The only asymmetry is the
  shaped geometry + shared ability set.
- Placed geometry shall double as **recharge/stillness pockets**.

## Non-functional requirements (these dominate every technology decision)

1. **Bit-exact determinism.** The entire simulation must be reproducible *exactly* from player
   inputs alone.
2. **Fixed-timestep, fully deterministic physics.** No variable substepping in the sim step.
3. **Rollback netcode.** Predict remote inputs; re-simulate on misprediction; re-sim must be
   bit-identical to the original.
4. **Server-authoritative truth.** On disagreement, the server sim is truth. 1v1 may be P2P
   with a referee; 4-player leans on the authoritative server.
5. **Count-agnostic simulation.** No system may assume a fixed player count. Systems iterate
   over `N` entities, never `player1`/`player2`. 1v1 and 4-player run the **same code** at
   different `N`. Minimum shippable target: **1v1**. Design target: **up to 4 players**.
6. **Frame budget under rollback.** Per-tick sim for 4 ropes (up to 6 collision pairs) plus a
   realistic rollback re-sim must fit the frame budget at the locked tick rate. This number —
   not determinism — decides whether 4-player ships day one.
   - **Simulation tick rate: 128 Hz (provisional).** Set to match Counter-Strike competitive
     server cadence. ~2× the rollback re-sim cost of 60 Hz; this is the intended pressure on
     the 4-player frame budget and must be measured by the First Test (Aspect #5), not assumed.

## Highest-risk requirement (resolve first)

The signature mechanic — **stiff coupled-spring ropes that collide with each other** — is the
exact class of stiff, coupled system that historically breaks determinism under rollback
re-simulation. It is **unproven** and is the single highest risk. It must be resolved by the
**First Test** (see [`ARCHITECTURE.md`](./ARCHITECTURE.md) and `PROJECT_BRIEF.md` §4) before any
other build work. Designing around it (constrain or drop rope-rope collision) is acceptable
**only on test evidence**, never pre-emptively.

## Out of scope

- **Hidden-information mechanics** of any kind — they contradict the core (see Perfect
  information).
- **Variable-player-count special-casing** — no `player1`/`player2` code paths.
- **Engine selection by theory** — the engine is chosen by the First Test result, not argued in
  advance.
- **Hosting/provider commitment now** — deliberately deferred and swappable (`PROJECT_BRIEF.md`
  §6).
- **Speculative features** beyond the brief — implement what is specified, nothing "just in
  case."

## Open decisions (need human input)

- [x] **Working title** — **movement.md**. Repo: https://github.com/SpreitzerJacobGit/movement.md
- [x] **4-player ship timing** — **day one.** First Test aspect #5 (Quantum) measured max tick
      0.1997 ms + depth-8 rollback = 1.797 ms / 7.8125 ms (~77% headroom) at the 4-rope worst case
      → 4-player ships day one. See `spike/determinism/FIRST_TEST.md` Run 2.
- [x] **Simulation tick rate** — locked at **128 Hz (provisional)**, revisited after Aspect #5.
