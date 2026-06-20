# Replays

> Design doc for the **Replays** front-end menu. Status: **designed, not implemented.**
> Menus are view-layer only; replays re-run the deterministic sim from stored inputs and
> never mutate live sim state.

## Purpose

Browse saved matches and re-run them. Bit-exact determinism + input-only netcode make
replays nearly free: a replay file is just the **input stream + session config + checksum**,
and re-running the deterministic sim reconstructs the full world state. This is also a free,
permanent determinism canary — a desyncing replay is always a sim bug.

## Navigation

- **Entry:** Main Menu → Replays.
- **Exits:**
  - On replay select → Replay Viewer (re-runs sim from inputs).
  - Back → Main Menu.

## Items

- **My Replays** — list of saved matches; metadata per entry: date, mode, N, opponents,
  result, duration, input-file size (small — inputs only).
- **In-replay controls:**
  - Play / pause, tick-accurate timeline scrub, slow-mo.
  - Free camera; toggle each perfect-information tell individually (position light, look cone,
    spin meter, playback tell) — useful for analysis and accessibility.
  - Per-player input overlay (read-only display of the sampled inputs — instructional value).

## State & data

- **Replay file** = `{ input stream, session config, checksum, metadata }`. Re-running the
  deterministic sim from the input stream reconstructs the entire match bit-for-bit.
- **Storage: inputs only** (decided). No periodic state snapshots — seek re-simulates forward
  from tick 0. Simplest; revisit only if seek latency proves bad on real match lengths.
- Metadata is small and stored alongside the input file.

## Determinism / architecture notes

- **Replay re-run MUST be bit-identical to the original match.** This is NFR #1 and #2 applied
  to stored inputs. If a replay ever desyncs, it is a sim bug — never "expected drift."
- **Timeline scrub reuses the rollback machinery.** Seeking to tick T re-simulates forward
  from tick 0 (inputs-only storage) — same code path as rollback re-sim, which is why it must
  be bit-identical (ARCHITECTURE.md Decision 2).
- The viewer is **read-only over sim state** (presentation layer rule). Camera, overlays, and
  tell toggles affect rendering only.

## Requirements coverage

- NFR #1: Bit-exact determinism — replays are a permanent, free determinism test.
- NFR #2: Fixed-timestep deterministic physics — required for replay to reconstruct state.
- NFR #3: Rollback — scrub/seek reuses the rollback re-sim path.
- Supports the competitive / esports framing of the brief (perfect information → watchable).

## Out of scope (deferred)

- **Live spectate** — deferred; revisit when prioritized. Will need a spectate-delay policy to
  prevent ghosting in ranked.
- **Featured / shared replays** — deferred; needs sharing infrastructure that doesn't exist in
  v1.

## Open questions

- How long are replays retained locally, and is there ever a server-side highlight store?
  Deferred with hosting (`PROJECT_BRIEF.md` §6).
