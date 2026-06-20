# Sandbox / Training

> Design doc for the **Sandbox / Training** front-end menu. Status: **designed, not
> implemented.** Menus are view-layer only and never mutate Quantum sim state.

## Purpose

Offline, free-play practice space. Distinct from the *match-loading* sandbox described in the
brief ("players spawn into a sandbox that morphs to load a match") — this is the **practice
sandbox**: no opponent (or a scripted dummy), local-only sim, no networking. The natural
place to learn movement, the spin meter, grapple physics, and the perfect-information tells
in isolation.

## Navigation

- **Entry:** Main Menu → Sandbox / Training.
- **Exits:**
  - On pick → Sandbox scene (offline, single-client).
  - Back → Main Menu.

## Items

- **Free Movement** — empty arena, all movement abilities enabled, no timer.
- **Spin Range** — exercises the spin system; surfaces the abstract geometric spin meter so the
  player learns to read state visually (FR: Spin rules).
- **Grapple Range** — anchor points to practice stiff-spring rope physics. No rope-rope
  collision unless a dummy with its own rope is present.
- **Training Dummy** — stationary or scripted-moving bot to practice offense scaling, reads,
  and the position-light / look-cone tells.
- *(Flagged — speculative, do not build for v1)* **Replay Takeover** — load a saved replay and
  take control at a chosen tick. Listed only to reserve the design space; cut if it complicates
  the input-pipeline contract.

## State & data

- Runs a **local deterministic Quantum sim**, single client. No remote inputs → no rollback
  needed, but the sim itself stays identical.
- Dummies are simulated entities driven by scripted or recorded input streams — still
  deterministic, still count-agnostic (they are just more movers in the set).
- Sandbox layout / dummy behavior is view/config state; not persisted unless presets are added
  (out of scope for v1 — "no speculative features").

## Determinism / architecture notes

- **Same sim, single-player instance.** Nothing about the sandbox weakens determinism rules
  (`CONVENTIONS.md` §Determinism rules still apply — fixed point only, stable pair order,
  seeded RNG, no wall-clock in the step).
- Ideal surface for validating the **presentation layer** (spin meter, look cone, position
  light, playback tell) in isolation, since there is no networked opponent.
- Dummies must feed inputs through the **same input path** as a real player — never get a
  privileged backdoor into sim state. A dummy is just a mover with an input source.

## Requirements coverage

- FR: Movement — every ability is free and ungated; the sandbox is where this is felt.
- FR: Spin rules — meter visualization and record/playback separation are best taught here.
- FR: Perfect information — the tells are best learned in a no-pressure space.
- NFR #1–2: Determinism / fixed timestep — the sandbox uses the same sim, so it doubles as a
  run-to-run determinism sanity check.

## Open questions

- *(Resolved)* **Tutorial is deferred** — a scripted Tutorial flow will be needed later, but
  not in v1. The Sandbox is the v1 entry point for learning; revisit when onboarding needs
  become clear.
- Are sandbox layouts saveable/sharable? Default: **no** for v1 (out of scope).
