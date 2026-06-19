# Project Brief — movement.md (a deterministic movement shooter)

> **How to use this document:** This is the authoritative starting brief for the project.
> Read it top to bottom before writing any code. **Nothing in the "Build Sequence"
> section may begin until the First Test (Section 4) has been run and its result
> recorded.** The First Test decides the engine, the topology, and whether the
> signature mechanic ships at all. Do not pour architecture on top of an unproven
> physics assumption.

---

## 0. One-line vision

A perfect-information, mirror-match movement shooter where the only hidden variable is
*the future* — what emerges when two fully-visible, deterministic, deeply-interacting
physical systems collide at speed. Skill at every layer is the ability to predict and
shape that future faster than your opponent.

---

## 1. Design summary

### 1.1 The three-tier skill model

The game is designed for a **low skill floor, high skill ceiling**, organized into three
interacting layers:

- **Micro (execution):** Precise input. Hitting movement and aim inputs cleanly.
  A novice can grapple, slide, and shoot on day one. A master composes stacked-spin
  trajectories and reads rope-tangle physics in real time.
- **Meso (reading & adaptation):** **NOT bluffing.** This game has perfect information
  (see 1.3), so the meso layer is *pattern recognition and real-time adaptation to
  highly complex systems interacting in new ways.* The uncertainty is not in what you
  can see — it is in what you can predict from what you see. This is the chess model:
  full information, unbounded emergent complexity.
- **Macro (optimization):** Shaping the arena. Between rounds players edit geometry and
  the shared ability pool to build a round their own execution style wins. The central
  macro question: *"Can I shape a symmetric arena so my execution beats yours inside it?"*

### 1.2 Core mechanics

**Movement (always free — never gated, never on cooldown):**
- Standard layer: sprint, slide, jump, wall-jump, wall-run.
- **Grapple — stiff rubber-band (spring), not a fixed rope.** Grapples are
  *instantaneously created physical ropes that collide with each other.* When one player
  swings into the path of another's rope, the ropes collide and deflect both
  trajectories, exactly as physical ropes would. **The grapple is the STEERING tool.**
- **Spin — the acceleration tool.** Hold a button and rotate the camera. The camera
  still moves normally; the rotation is *recorded* as stored spin. Stored spin discharges
  into momentum over time as an **additive** vector.

**Spin rules (these are exact and load-bearing):**
- Stored spin is measured by **total angle swept, NOT angular velocity.** Identical
  intended motion must produce identical spin regardless of how fast the camera was
  moved, DPI, or frame timing. *This is the line between learnable tech and a slot
  machine. Integrate angle, not angular velocity.*
- **Record and playback are separated by player choice.** You may bank spin in safety
  and discharge it later in combat.
- **Inverse-speed recording:** the slower you are while recording, the greater the stored
  effect; the faster you are, the weaker it. This restores the offense/defense tradeoff —
  banking big spin requires being slow, and slow means exposed.
- **Stacking is order-independent and fully deterministic.** `A then B == B then A`.
  Reproducible every time from the exact momentum-at-record plus the camera motion
  recorded.
- Spin state is shown to the player as an **abstract geometric meter** so they learn to
  recognize their state visually as well as by feel.

**Offense:**
- Shooting. Offensive output **scales with speed and rate-of-change of direction.**
- **Special abilities refresh only when standing still** (flicker-fast refresh).
  Movement never gates on stillness — only special/offensive abilities do.
- Net effect: moving well *feeds* offense rather than costing it. The remaining tension
  lives in the fact that recording strong spin and refreshing abilities both require
  being slow/still, which is the most exposed state in the game.

### 1.3 Perfect information (deliberate)

Information is **not** a limiting factor, by design:
- Opponent position emits a light visible **through all geometry**.
- Look-direction is shown as a visible **cone**.
- Spin recording is visible; playback has a tell.
- "A tell for pretty much everything."

This is a deliberate "one correct path" choice: skill expression is unambiguous, nothing
is hidden, the better executor demonstrably wins, and an entire class of "I lost to info I
couldn't have had" complaints is removed. **Do not add hidden-information mechanics — they
contradict the core.**

### 1.4 Match structure & macro layer

- Players spawn into a **sandbox**; the environment morphs to load them into a match.
- Matches are a **series of rounds.**
- **Between rounds**, each player chooses to: add or remove a piece of **geometry**, OR
  add or remove an **ability**.
- **The ability pool is SHARED.** Removing an ability removes it for both players this
  round (opponent-denial is intended).
- Geometry **persists across rounds within a match** and **resets per match.**
- **Both players start every round in the exact same state.** The only asymmetry is the
  geometry placement and the shared ability set both players shaped.
- Because geometry favors the better mover (a wall is an opportunity to the strong mover,
  an obstacle to the weak one), the real macro skill is reading the gap: complicate the
  map when ahead on movement, strip it to dead space when behind. Placed geometry also
  functions as **recharge/stillness pockets** — safe spots to refresh abilities and bank
  spin.

---

## 2. Hard technical constraints (non-negotiable)

These are the requirements that dominate every technology decision. They cannot be bolted
on later.

1. **Bit-exact determinism.** The entire simulation must be reproducible *exactly* from
   player inputs alone.
2. **Fixed-timestep, fully deterministic physics.** No variable substepping in the
   simulation step.
3. **Rollback netcode.** Predict remote inputs, re-simulate on misprediction.
4. **Server-authoritative truth.** If clients ever disagree, the server's simulation is
   truth. For 1v1, peer-to-peer with a referee is acceptable; for 4-player, lean on the
   authoritative server.
5. **Count-agnostic simulation.** **No system may assume a fixed player count.** Systems
   iterate over entities (N movers), never over `player1`/`player2`. 1v1 and 4-player must
   run the *same code* at different N. The minimum shippable target is 1v1; the design
   target is up to 4 players.

### 2.1 The gating risk

The signature mechanic — **stiff coupled-spring ropes that collide with each other** — is
exactly the kind of stiff, coupled physical system that historically *breaks* determinism
under rollback re-simulation, because tiny float differences compound. **This is unproven.
It is the single highest risk in the project and must be resolved first.** It is acceptable
to design around it (drop or constrain rope-rope collision) if the test fails — but that
decision must be made on evidence, not hope.

---

## 3. Technology options (the engine is decided BY the First Test, not before it)

Do not pick an engine on theory. The First Test (Section 4) selects between these.

- **Path 1 — Buy the determinism: Unity + Photon Quantum.**
  Deterministic ECS with fixed-point physics, rollback and input-only networking built in,
  free for development (current version 3.0.x as of 2026). The simulation has no Unity
  dependency and can run headless as the server-side referee. *Caveat:* the coupled-spring
  rope solver is not built in — it must be written inside Quantum's fixed-point math, and
  Quantum's 3D physics is less mature than its 2D. **This is the preferred path if the test
  passes inside Quantum.**

- **Path 2 — Own the determinism: Rust (Bevy + Rapier + GGRS / lightyear).**
  Rapier has a cross-platform deterministic mode; GGRS provides rollback. You control every
  float and the rope solver is yours to make deterministic. Fits "clarity over
  compatibility / one way / no fallbacks" — but enormous surface area: you own all
  determinism bugs and tooling. **Justified only if the test fails inside Quantum.**

- **Path 3 — Fully custom engine / fixed-point from scratch.** Maximum control, almost
  certainly overengineering at this stage. **Ruled out unless Paths 1 and 2 both fail.**

---

## 4. ⭐ THE FIRST TEST — Determinism Spike (DO THIS BEFORE ANYTHING ELSE) ⭐

**This test is the entire reason the project starts here and not with gameplay.** It
simultaneously (a) proves or kills the signature mechanic and (b) selects the engine. One
experiment, two answers. Build nothing else until it has run and its result is recorded.

### 4.1 What to build

A standalone spike: **the stiff coupled-spring rope-rope collision solver**, built **inside
Photon Quantum's fixed-point math library.** (Building it in Quantum is deliberate: if it
passes here, the feature *and* Path 1 are validated together. If Quantum's primitives can't
express it, that failure is the evidence that justifies the cost of Path 2.)

### 4.2 Test at FOUR ropes, not two

**Critical.** Two ropes have exactly one possible rope-rope collision and will pass clean
while hiding a fatal bug. Four ropes create up to **six simultaneous collision pairs**,
which exposes **collision-pair resolution order** as a source of nondeterminism. A two-rope
spike gives false confidence. **Test at the worst case you intend to ship (4), or the test
is theater.**

### 4.3 Mandatory implementation rule inside the spike

- **Resolve collision pairs in a stable order sorted by entity ID — never by iteration
  order** (hash maps / spatial hashes return pairs in machine- or run-dependent order, and
  stiff springs amplify the resulting divergence into a full desync). This rule is the
  primary thing the four-rope test is checking.

### 4.4 What to evaluate (each aspect, explicitly)

| # | Aspect | How to evaluate | Pass condition |
|---|--------|-----------------|----------------|
| 1 | **Determinism across runs** | Run identical inputs through the 4-rope sim ~10,000 times. Diff final state bit-for-bit. | Every run bit-identical. |
| 2 | **Determinism across rollback** | Force a rollback and re-simulate the same frames. Diff the re-simulated state against the original bit-for-bit. | Re-sim bit-identical to original. |
| 3 | **Collision-pair order independence** | Confirm ID-sorted pair resolution holds; verify result does not depend on iteration order (shuffle internal iteration, expect identical output). | Output invariant to iteration order. |
| 4 | **Expressibility in Quantum** | Can Quantum's fixed-point primitives represent a stiff coupled-spring solver at all? | Solver implementable without leaving fixed-point. |
| 5 | **Frame budget under load** | Measure per-tick sim time for 4 ropes, then measure cost of a *rollback* (re-simulating the heavier 4-rope step, at the chosen tick rate and a realistic rollback depth). | Fits frame budget with rollback headroom (see 4.6). |

### 4.5 Why frame budget is the real ceiling (not determinism)

Determinism is a yes/no proven once. **Cost is the number that decides whether 4-player
ships.** Two things scale together and multiply:
- **Rope-rope interaction pairs scale as n-choose-2:** 1 pair at 2 players, **6 pairs at 4
  players.**
- **Rollback frequency rises with remote player count:** 3 remote players mispredict far
  more often than 1, and *each* misprediction re-simulates the entire (now 6×-heavier)
  physics step.
The product — frequent rollbacks each re-running a heavier step — is the true performance
ceiling and the thing Aspect #5 must measure. The same number sizes the server-side
referee requirement.

### 4.6 Decision tree from the result

- **All five aspects pass →** take **Path 1 (Unity + Photon Quantum).** Feature and engine
  validated together. Proceed to Build Sequence.
- **Determinism passes but frame budget only fits at 2 ropes →** ship **1v1 first**, treat
  4-player rope-collision as a later optimization or a constrained mode. Core design
  survives.
- **Quantum can't express the solver, or determinism fails inside Quantum →** this is the
  evidence to justify **Path 2 (Rust)**. Re-run the same five-aspect test there before
  committing.
- **Both fail →** redesign around the rope-rope collision (constrain to impulse-based with
  fixed iteration count, or drop rope-rope collision). Make this call on the test result,
  not before.

### 4.7 Required input before the spike can produce a real number

- **Simulation tick rate — UNSET, needs confirmation.** Rollback cost ≈
  `tick_rate × rollback_depth × per_tick_sim_cost`, so a 60 Hz sim costs roughly double a
  30 Hz one to re-simulate. A movement shooter this twitchy likely wants **60 Hz or higher**.
  Set this before running Aspect #5; until then, run the spike at a provisional 60 Hz and
  flag the number as provisional.

---

## 5. Build sequence (only after the First Test passes)

1. **Lock the simulation as count-agnostic.** Entities, not players. No `player1`/`player2`
   anywhere.
2. **Run the First Test (Section 4)** at four ropes, ID-sorted resolution. Record the
   result. This selects the engine.
3. **The spike's frame-time number decides** whether 4-player ships day one or trails the
   1v1 launch.
4. **Topology falls out of the result:**
   - 1v1 → peer-to-peer rollback, each client predicts one opponent, server optional as
     referee.
   - 4-player → authoritative server runs the truth sim at worst-case 4-rope load; clients
     predict and roll back against it. Still input-only, so bandwidth stays trivial; the
     *server* carries the physics cost.
5. **Build the core sim:** movement (sprint/slide/jump/wall-jump/wall-run), grapple
   (steering), spin record/playback (acceleration), offense scaling, stillness-refresh.
6. **Layer perfect-information presentation** (position light, look cone, spin meter,
   playback tell).
7. **Macro systems:** between-round geometry editing + shared ability pool, identical
   per-round start state, geometry persistence within match.
8. **Hosting — decide LATE, it is swappable.** (See Section 6.)

---

## 6. Hosting (deliberately deferred — swappable, do not commit now)

Input-only deterministic netcode means the server is featherweight: it relays/validates
inputs and optionally runs a referee sim. It does not stream heavy world-state. This means
casual play may not need dedicated per-match servers at all (P2P rollback with a server as
referee), reserving dedicated servers for ranked/4-player.

The market churned hard in 2026 — **Unity Multiplay ended direct support March 31 2026 and
Hathora shut down May 5 2026** — so do not lock in a provider early. Live container-based
options when you do need orchestration: **Edgegap, Gameye, GameFabric (Nitrado), AWS
GameLift.** All take a Docker image + REST call, so they remain swappable. Decide after the
spike tells you whether you're running heavy servers at all.

---

## 7. Implementation principles (apply throughout)

- **Don't overengineer:** simple beats complex.
- **No fallbacks:** one correct path, no alternatives.
- **One way:** one way to do things, not many.
- **Clarity over compatibility:** clear code beats backward compatibility.
- **Throw errors / fail fast** when preconditions aren't met.
- **Redundant checks** with descriptive error handling at boundaries.
- **Separation of concerns:** each function/system has a single responsibility.
- **Surgical changes only:** minimal, focused fixes.
- **Evidence-based debugging:** minimal, targeted logging; fix root causes, not symptoms.
- **Detective method:** form a theory of the problem, gather evidence, and only fix once
  evidence proves the theory. Do not build on a beautiful theory the evidence hasn't
  confirmed. **The First Test is this principle applied to the whole project.**

---

## 8. Open decisions requiring human input

- [ ] **Simulation tick rate** (Section 4.7) — recommended 60 Hz+; required before the
      frame-budget aspect of the spike produces a real number.
- [x] **Working title** — **movement.md** (repo: https://github.com/SpreitzerJacobGit/movement.md).
- [x] **4-player ship timing** — **day one.** First Test aspect #5 (Quantum) measured max tick
      0.1997 ms + depth-8 rollback = 1.797 ms / 7.8125 ms (~77% headroom) at the 4-rope worst case
      → 4-player ships day one. See `spike/determinism/FIRST_TEST.md` Run 2.
