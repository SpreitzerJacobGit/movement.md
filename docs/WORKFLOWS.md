# Workflows

> Team size is currently **assumed solo** (single brief author); revise if that's wrong. Process
> is intentionally lightweight. The one hard rule overrides everything: **no build-sequence work
> begins until the First Test has run and its result is recorded.**

## The gate (read before anything else)

```
First Test (determinism spike, 4 ropes, ID-sorted) ──passes──▶ build sequence unlocked
                                                    ──fails───▶ redesign / switch engine path
```

Nothing in `PROJECT_BRIEF.md` §5 (build sequence) may start until the First Test in `spike/`
has produced a recorded result. This is non-negotiable.

## Branching strategy

- **Trunk-based** off `main` (solo cadence). This repo is **not yet a git repository** —
  initialize with `git init` before the first commit.
- Short-lived feature branches named `spike/<topic>`, `sim/<system>`, `net/<topic>`,
  `macro/<topic>`, `docs/<topic>`. Merge to `main` when the change is self-contained and its
  determinism test (where applicable) passes.

## Development loop

1. **Idea → scope check.** Confirm the change is permitted by the gate (is the First Test done?)
   and is in scope per `REQUIREMENTS.md`. If structural, propose options first (see CLAUDE.md).
2. **Branch.**
3. **Implement** surgically — one system, one responsibility. Fixed-point in the sim; ID-sorted
   pair resolution; no float/wall-time/iteration-order dependence.
4. **Test.** For sim changes, run the relevant determinism test (run-to-run diff + rollback
   re-sim diff). Bit-for-bit, not tolerance.
5. **Review.** Run `/review` — applies the personas in `REVIEWERS.md` plus the security and
   requirements/architecture-drift checklist from `CLAUDE.md`.
6. **Merge** to `main`. Commit only when asked; never auto-commit.

## Code review

- **Reviewer:** solo self-review via the `/review` command and the `REVIEWERS.md` personas.
- **Bar before merge:**
  - Determinism Reviewer must pass for any sim change (no float in sim, ID-sorted pairs, pure
    function of inputs).
  - Count-Agnostic Reviewer must pass (no fixed player count, no `player1`/`player2`).
  - Security Reviewer must pass (input validation, server-authority integrity, no injection).
  - No requirements/architecture drift (CLAUDE.md "After major work" checklist).
- Tests for new sim behavior must exist.

## Deployment

| Environment | Trigger | Notes |
|---|---|---|
| Local dev | manual run | Needs the chosen engine installed (Unity+Quantum, or Rust). None installed yet. |
| Headless referee / server | TBD | Sim runs Unity-independent; provider deferred & swappable (`PROJECT_BRIEF.md` §6). |
| Dedicated multiplayer host | TBD, post-spike | Container image + REST (Edgegap/Gameye/GameFabric/GameLift). Decide after the spike says whether heavy servers are needed at all. |

Rollback procedure: TBD once a deploy target exists.

## Release process

- **TBD** — no shippable artifact until the First Test passes and the 1v1 core is built.
- When versioning begins, default to SemVer; cut from `main`. Revisit during build sequence.

## Incident response

- **N/A while pre-alpha and solo.** Define an escalation/runbook path once there is a live
  multiplayer service. Until then, the only "incident" that matters is a **determinism desync**,
  whose runbook is: capture the input log, replay it, bit-diff to the divergent tick, fix root
  cause (usually a float-in-sim or non-ID-sorted resolution), add a regression determinism test.
