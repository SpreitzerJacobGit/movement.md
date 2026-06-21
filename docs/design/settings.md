# Settings

> Design doc for the **Settings** front-end menu. Status: **designed, not implemented.** Menus
> are view-layer only and never mutate Quantum sim state. Settings affect how **physical
> input becomes sim input**, and how state is **rendered** — never how the sim processes a
> given input.

## Purpose

All user-tunable config. The biggest menu because the game has many inputs (movement suite,
grapple, two spin actions, fire, aim, between-round macro edits) plus standard A/V. This is
also where the project's hardest rule is most easily violated by accident, so the
determinism notes below are load-bearing.

## Navigation

- **Entry:** Main Menu → Settings.
- **In-match:** reachable via pause for non-sim-affecting tabs (Controls rebind mid-match is
  out of scope for v1 — disallow or defer).
- **Exits:** Back; changes save-on-back (or autosave per field — pick one, see Open questions).

## Items (tabs)

### Controls / Keybinds

Rebind every sim input. Distinct binds are required for:

- **Movement:** sprint, slide, jump, wall-jump, wall-run.
- **Grapple** (the steering tool).
- **Spin record** (hold) and **Spin discharge** — must be **separable** (FR: Spin rules —
  "record and playback are separated by player choice"). One bind each, never a toggle that
  couples them.
- **Fire**, **Aim** (mouse).
- **Between-round macro:** geometry add / remove, ability add / remove (these are the
  round-edit inputs from FR: Match & macro structure).
- Per-action rebind + reset-to-default.

### Mouse / Aim

Sensitivity, inversion, raw input.

> **Worth surfacing to players (tooltip):** spin integrates **swept angle**, not angular
> velocity (FR: Spin rules). So sensitivity changes camera *feel* but does **not** change how
> much spin a given camera motion banks. This is a deliberate, load-bearing property of the
> spin system; the tooltip prevents the misconception that "lower sens = more spin."

### Video

Resolution, display mode, **render-rate cap** (independent of the fixed 128 Hz sim tick — see
notes), quality presets, FOV, and per-tell render toggles for accessibility (position light,
look cone) — presentation only.

### Audio

Master, music, SFX, UI; optional positional-audio toggle.

### Gameplay (presentation / accessibility)

Crosshair; **spin meter presentation** (opacity, on-screen position, colorblind palette); HUD
scale; tell opacity; colorblind-friendly palettes for the perfect-information tells.

> **The spin matrix itself is NOT a per-player setting.** The matrix — its values and how it
> shapes movement — is a **ruleset-level parameter**: identical for both players, authored in
> ruleset config (see *Open questions*; rulesets doc TBD). Settings here only controls how a
> player's own HUD *displays* the matrix-driven meter.

**Constraint:** these options may adjust presentation but must not *remove* information the
perfect-information rule requires (see notes).

### Network (advanced / hidden by default)

Rollback depth (read-only display), input delay for online, region preference.

## State & data

- Settings = view/config layer, persisted to a local config file.
- **Keybinds** are the mapping from physical input → sim input. They are decoded at the
  input-sampling step (ARCHITECTURE.md Data flow step 1). Keybinds are **not** sim state.
- **Sensitivity / mouse options** alter how physical mouse motion becomes an aim-delta input.
  The sim still receives only the resulting aim input; it never sees the sensitivity value.

## Determinism / architecture notes (load-bearing — read before editing this menu)

- **Settings never enter the sim as state.** The sim is a pure function of inputs
  (`CONVENTIONS.md`). A sensitivity change alters input *generation*; it never alters input
  *processing*.
- **Tick rate (128 Hz) is fixed and NOT user-tunable.** It is a sim constant
  (`SessionConfig.UpdateFPS = 128`). Only the *render* rate is user-tunable, and the renderer
  never feeds sim state back.
- **Between-round macro inputs flow through the same input path** as everything else — they
  are not a special menu-only channel. The round-edit UI produces inputs; the sim consumes
  them.
- **Accessibility options must not weaken perfect information.** Tell-opacity / colorblind
  settings may reskin or reposition a tell but must not let a player hide a tell that gameplay
  requires everyone to see. Concretely: a player may make *their own* view cleaner, but the
  opponent's tells (position light, look cone, spin meter, playback tell) must remain visible
  to them at all times.

## Requirements coverage

- FR: Movement — every ability needs a bind.
- FR: Spin rules — record/playback binds must be separable; meter is abstract-geometric.
- FR: Perfect information — settings may adjust presentation but must not remove required
  tells.
- FR: Match & macro structure — round-edit inputs need binds.
- NFR: Fixed timestep — 128 Hz sim is not exposed as a setting.
- NFR #1: Determinism — settings never touch sim state.

## Open questions

- *(Resolved)* **Spin matrix is a ruleset-level parameter**, not a per-player setting. The
  matrix (its values + how it shapes movement) is identical for both players and authored in
  ruleset config — consistent with the mirror-match rule (REQUIREMENTS.md: "both players start
  every round in the exact same state"). Settings owns only *presentation* of the
  matrix-driven meter (opacity, position, colorblind palette). → This implies (a) a
  ruleset-authoring surface not in the current menu list, and (b) an amendment to REQUIREMENTS.md
  spin rules to define the matrix parameter. Both proposed separately; not done in this doc.
- **Save model:** save-on-back vs autosave-per-field. Recommendation: save-on-back with an
  explicit Apply (matches "one way / no fallbacks").
- **Controls rebind mid-match:** disallow for v1 (scope), revisit later.
- **Network tab visibility:** hide rollback depth for v1 (confusing for new players); expose
  only input-delay slider and region preference.
