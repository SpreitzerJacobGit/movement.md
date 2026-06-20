# Rulesets

> Design doc for the **Rulesets** front-end menu. Status: **designed, not implemented.**
> A ruleset is **match config, identical for both players** — never per-player, never mutated
> during a match. Once a match loads a ruleset, its values become sim constants for that match.

## Purpose

Browse, create, edit, and select **rulesets**. A ruleset is the bundle of match-level
parameters that are the same for every player: the **spin-generation function**, rounds-per-
match, win condition, and ability-pool defaults. This is where the spin-generation function
gets configured ("highly configurable" by design decision), because it must be identical for
both players to preserve the mirror-match invariant — which is exactly why it lives here and
not in per-player Settings.

## Navigation

- **Entry:** Main Menu → Rulesets.
- **Exits:**
  - Select a ruleset → returns to Play (Private Match) as the active ruleset.
  - New / Edit → ruleset editor.
  - Back → Main Menu.

## Items

- **Browse Rulesets** — built-in defaults + user-saved rulesets. Each shows a summary (spin
  function profile, rounds, win condition, ability pool).
- **New / Edit Ruleset** — opens the ruleset editor (below).
- **Duplicate / Delete / Export / Import** — manage user rulesets. Export = shareable data
  file.
- **Validate** — runs the ruleset through a deterministic sanity check (does the spin function
  stay within the load-bearing invariants? does it desync across rollback re-sim?) before it
  can be saved or used. **A ruleset that fails validation cannot enter a match** — fail fast.

### Ruleset editor

The editor configures, at minimum:

- **Spin-generation function** (the highly-configurable core). Per `REQUIREMENTS.md` Spin rules,
  this is a complex function:
  - **Inputs:** the mover's current velocity, acceleration, and recorded camera (mouse) motion.
  - **Output:** a velocity + acceleration + impulse effect, applied on discharge, triggered by
    the player on command.
  - **Configurable surface:** input weights (how strongly velocity / acceleration / camera
    motion each contribute), the recording curve (inverse-speed shape), the discharge curve
    (how stored spin maps to output velocity / acceleration / impulse over time), stacking
    behavior, and the impulse profile. The exact parameter set is ruleset-design work (see Open
    questions) — do not speculatively build knobs with no proven meaning.
  - **Hard constraint:** the function must stay **within** the load-bearing spin invariants —
    angle integration (not angular velocity), inverse-speed, order-independent stacking,
    additivity, bit-exact determinism. The editor's Validate step enforces this.
- **Rounds-per-match** — configurable (decided; ruleset-owned, not host-owned).
- **Win condition** — e.g., best-of-N vs score-to-win (ruleset-design TBD).
- **Ability-pool defaults** — the starting shared ability set for round 1 of a match.
  Between-round macro edits modify this within a match; the ruleset sets the round-1 baseline.

## State & data

- A ruleset is **match config data**, stored as a deterministic, versioned data file.
- Rulesets are **not sim state and not per-player state.** On match load, the chosen ruleset's
  values become sim constants for that match; they do not mutate during play.
- User-authored rulesets persist locally; exported rulesets are shareable as data files.

## Determinism / architecture notes (load-bearing)

- **A ruleset is identical for both players.** This is the mirror-match invariant
  (`REQUIREMENTS.md`: "both players start every round in the exact same state"). Per-player
  tuning of the spin-generation function is forbidden by design — that is why this surface is
  ruleset-level, not in Settings.
- **Ruleset values become sim constants at match load.** They feed the spin-generation function
  and match structure as fixed parameters; they never change mid-match and never depend on
  wall-clock, render rate, or input timing.
- **The spin-generation function must stay within the load-bearing invariants.** Specifically:
  "recorded camera motion" means angular displacement (angle integration), not raw mouse
  velocity — or DPI/frame-timing independence breaks. Velocity/acceleration inputs must be
  sim-state-derived only. The impulse output must be order-independent and bit-reproducible
  across rollback re-sim. (Flagged for explicit verification in the determinism test —
  `spike/determinism/FIRST_TEST.md`.)
- **Validation is mandatory.** A ruleset that can desync (e.g., a spin function that
  reintroduces angular-velocity dependence) must be rejected at save/select time, not
  discovered mid-match. Fail fast (`CONVENTIONS.md`).

## Requirements coverage

- FR: Spin rules — the spin-generation function is defined and constrained here; invariants
  preserved.
- FR: Match & macro structure — rounds-per-match, win condition, ability-pool baseline.
- FR: Mirror-match ("both players start every round in the exact same state") — enforced by
  ruleset-level (not per-player) config.
- NFR #1, #2, #3: Determinism / fixed timestep / rollback — ruleset config must not introduce
  nondeterminism; validate before use.

## Open questions

- **Spin-generation function parameter set:** which exact knobs does the editor expose? Needs
  ruleset-design work; do not speculatively build knobs that have no proven meaning to players.
- **Default ruleset values:** the shipped default spin function, rounds-per-match, win
  condition, ability-pool baseline. Set when the first ruleset is authored.
- **Validation depth:** can static validation catch all invariant violations, or do some
  require a run-time determinism test? Likely a combination — decide during implementation.
- **Ruleset sharing:** online library/browser for shared rulesets, or v1 local-only with file
  export/import? Recommendation: **local-only + file import/export for v1** (online sharing
  depends on hosting/backend, deferred per `PROJECT_BRIEF.md` §6).
