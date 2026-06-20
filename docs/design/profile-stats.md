# Profile / Stats

> Design doc for the **Profile / Stats** front-end menu. Status: **designed, not
> implemented.** Menus are view-layer only; stats are out-of-band metadata, never sim state.

## Purpose

Player identity, match history, and aggregate stats. Deliberately thin at v1, but designed so
the schema does not have to be rewritten later. **Critical constraint:** this game is
perfect-information — identity/cosmetics must never become a hidden-information mechanic.

## Navigation

- **Entry:** Main Menu → Profile / Stats.
- **Exits:**
  - Match-history entry → Replays (loads that match's input stream).
  - Shortcut → Settings (account / privacy, if any).
  - Back → Main Menu.

## Items

- **Identity** — display name, avatar (cosmetic only).
- **Match History** — chronological list; each entry links to the corresponding replay.
- **Summary Stats** — *(deferred to v2)* wins / losses per mode, rounds played. Not built in
  v1; schema designed when we know which stats matter to players.
- **Settings shortcut** — jumps into Settings for any account/privacy controls.

## State & data

- **Stats are derived/aggregate metadata**, not sim state. They live outside the deterministic
  sim entirely and are computed from match results.
- Match-history entries reference replay input files (see `replays.md`).
- Cosmetic identity is view-layer data sent to other clients for presentation only.

## Determinism / architecture notes

- **Stats and history never touch the sim.** They are pure out-of-band metadata; adding,
  removing, or corrupting them must have zero effect on any match outcome or replay.
- **Cosmetic identity must not feed back into gameplay.** The gameplay tells are the
  position light, look cone, spin meter, and playback tell (FR: Perfect information) — not
  nameplates, avatars, or titles. Cosmetics are allowed precisely because they carry no
  gameplay information.
- This menu is therefore the cleanest example of the view-layer rule: read-only, no sim
  coupling, no deterministic consequences.

## Requirements coverage

- Supports the competitive framing of the brief; no direct FR overlap.
- Must NOT introduce hidden-information mechanics (FR: Perfect information is explicit — "No
  hidden-information mechanics may be added").

## Open questions

- **Stats are deferred to v2.** Do not build summary stats in v1. The schema (W/L only, or
  deeper metrics like movement accuracy / spin efficiency / rope-collision outcomes) will be
  designed once we have evidence about what players actually value. Where stats live
  (client-local vs server-backed) is deferred with hosting (`PROJECT_BRIEF.md` §6).
- Cosmetic identity: confirm display name is purely presentation and does not appear in the
  gameplay viewport in a way that could obscure a tell.
