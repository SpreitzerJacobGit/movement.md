# movement.md

A perfect-information, mirror-match movement shooter where the only hidden variable is *the
future* — what emerges when two fully-visible, deterministic, deeply-interacting physical
systems collide at speed. Skill at every layer is the ability to predict and shape that future
faster than your opponent.

Working title. Repo: https://github.com/SpreitzerJacobGit/movement.md

## Status

- **Engine: Unity 6000.3.17f1 + Photon Quantum 3.0.11 — validated by the First Test
  (2026-06-17).** Determinism across runs, rollback re-simulation, and fixed-point
  expressibility all passed at 4 ropes / 128 Hz. The build sequence is unlocked.
- **Simulation tick rate: 128 Hz** (`SessionConfig.UpdateFPS = 128`).
- **Ship target: 1v1 (minimum) and 4-player (day one).** First Test Aspect #5 measured ~77%
  frame-budget headroom at the 4-rope worst case → 4-player ships with the launch.
- **Signature mechanic (highest risk, now de-risked):** stiff coupled-spring grapple ropes that
  physically collide with each other, kept bit-exact under rollback.

## Stack

| Layer | Choice |
|---|---|
| Engine / renderer | Unity 6000.3.17f1 (LTS) |
| Deterministic sim | Photon Quantum 3.0.11 (deterministic ECS, fixed-point physics) |
| Netcode | Input-only transport, remote-input prediction, rollback re-simulation, server-authoritative |
| Topology | 1v1 → P2P + referee; 4-player → authoritative server |
| Hosting | Deferred and swappable (Edgegap / Gameye / GameFabric / GameLift) |

## Where to start

| Read this | For |
|---|---|
| [`PROJECT_BRIEF.md`](./PROJECT_BRIEF.md) | The authoritative starting brief — read first |
| [`CLAUDE.md`](./CLAUDE.md) | AI-assistant entry point / doc index |
| [`docs/REQUIREMENTS.md`](./docs/REQUIREMENTS.md) | Functional and non-functional requirements |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | System design, component map, key decisions |
| [`docs/CONVENTIONS.md`](./docs/CONVENTIONS.md) | Coding style + the non-negotiable determinism rules |
| [`spike/determinism/FIRST_TEST.md`](./spike/determinism/FIRST_TEST.md) | The First Test that gated everything |
| [`docs/design/`](./docs/design/) | Front-end menu design docs |

## Repo layout

```
movement.md/
├── PROJECT_BRIEF.md        # authoritative starting brief — read first
├── CLAUDE.md               # AI entry point / index
├── README.md               # this file
├── docs/                   # REQUIREMENTS / ARCHITECTURE / CONVENTIONS / WORKFLOWS / REVIEWERS
│   └── design/             # front-end menu design docs
├── spike/                  # the First Test — determinism spike + setup guide
│   └── determinism/        # 4-rope solver + 5-aspect validation harness
├── Assets/_Project/        # Unity project code (Core, Sim, UI, Presentation, Dev tools)
├── Assets/Scenes/          # Boot / Match / Sandbox / Training scenes
└── ProjectSettings/        # Unity project settings
```

## Core principles

These dominate every decision (see `PROJECT_BRIEF.md` §7):

- **Bit-exact determinism** — the sim is a pure function of inputs.
- **Count-agnostic** — no `player1`/`player2`; systems iterate `N` movers.
- **Perfect information** — no hidden-information mechanics may be added.
- **Evidence over theory** — the engine was chosen by the First Test, not argued in advance.

## AI assistants

This repo is set up for Claude Code and other AI assistants. `CLAUDE.md` is the entry point and
loads conventions, required docs, and working rules. Custom slash commands live in
`.claude/commands/` (`/init`, `/review`).
