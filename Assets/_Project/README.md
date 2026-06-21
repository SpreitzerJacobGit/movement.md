# movement.md — UI & Shell Groundwork

The host/renderer layer (Unity) for the deterministic sim. This is **shell only** — scene
scaffolding, menu navigation, a count-agnostic mode state machine, and a Developer Menu whose
tools route through a sim boundary (`ISimContext`) that returns a clear "pending" status until
Photon Quantum is installed. No gameplay systems live here yet.

> Read [`docs/REQUIREMENTS.md`](../../../docs/REQUIREMENTS.md) and
> [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md) before changing this layer. The sim is
> bit-exact and count-agnostic; the UI must never assume a fixed player count and must never feed
> floats/state back into the sim.

## One-time setup

1. Open the project in **Unity 6000.3.17f1** (let it import — `.meta` files are generated on
   first import; commit them afterwards).
2. Menu: **MovementMD ▸ Setup ▸ Create Shell Scenes & Assets**. This generates
   `Assets/Scenes/{Boot,Match,Sandbox,Training}.unity`, the `PanelSettings` and `Tunables` assets,
   wires every reference, and configures Build Settings. Safe to re-run.
3. Press **Play**. You land on the Main Menu. **F1** opens the Developer Menu. **ESC** returns to
   the menu from any mode.

## Modes (all count-agnostic — driven by `ModeConfig`)

| Mode | Scene | Players | Notes |
|---|---|---|---|
| Singles | `Match` | 2 (1v1) | |
| Doubles | `Match` | 4 (2v2) | Same scene/code as Singles — only `PlayerCount` differs |
| Sandbox | `Sandbox` | 1 | Free-form: no goals, all abilities, add/remove geometry |
| Training | `Training` | 1 | Scripted scenarios with objectives + pass/fail (scenarios TBD) |

Singles and Doubles share one Match scene deliberately — the simulation is count-agnostic, so
switching between them changes player count without reloading the scene.

## Architecture (this layer)

```
_Project/
├── Core/          GameMode · ModeConfig · AppFlow (mode state machine + additive scene orchestration)
├── Sim/           ISimContext boundary · NullSimContext stub · SimHost accessor (Quantum installs later)
├── Core/Match/    MatchConfig/Phase/State/Controller — count-agnostic match lifecycle (ft-11, bo3, edit windows)
├── Core/Macro/    MacroState (placed geometry, persists whole match) · GeometryPalette SO
├── Presentation/  OverlaySettings (perfect-info flags) · GeometryBuilder · PlacedGeometryRenderer · PlacedPieceView
├── UI/            UITheme · MainMenu (+ Settings) · HUD (uGUI) · DevMenu · Match (Scoreboard) · Macro (Place) · Settings
├── Dev/           IDevTool registry · Tunables SO · the dev tools (incl. Match Scoring)
└── Editor/        ShellSetup (one-time scene/asset/build-settings wizard)
```

- **App flow** = traditional funnel for now: Boot → Main Menu → mode select. The brief's
  "sandbox-as-hub that morphs into a match" (§1.4) is a later polish goal, not this shell.
- **UI tech = hybrid**: UI Toolkit for menus + Developer menu; uGUI Canvas for the thin in-match
  HUD. Perfect-info rendering (through-wall light, look cone, spin meter, playback tell) will be
  world-space meshes reading `OverlaySettings` — not screen-space UI.
- Menus are built in **C# (VisualElement API)** with inline styles (`UITheme`), not UXML/USS, so
  the shell runs with zero asset wiring. Migrate to UXML/USS via UI Builder later if desired.

## Developer Menu (F1)

Each tool is a `MonoBehaviour` implementing `IDevTool`; it self-registers into `DevToolRegistry`
in `OnEnable`, so the menu is extensible — drop a new `IDevTool` in the Boot scene and it appears.

| Tool | Routes through | Status |
|---|---|---|
| Scene / Mode Loader | `AppFlow` | **Live** (load/reload any mode) |
| Live Param Tweaker | `Tunables` (float proxies) | **Live** (edit at runtime; Save is editor-only) |
| Determinism Test Runner | `ISimContext.RunDeterminismTest` | Stub — run the spike from `spike/determinism` until Quantum is wired |
| Spawn / Clear Dummies | `ISimContext.SpawnDummy` | Stub |
| Perfect-info Overlays | `OverlaySettings` | Flags **live**; rendering TBD |
| Grapple Visual | `GrappleVisualSettings` | **Live** — color/glow/width-curve tweaks; "Show demo rig" spawns a mock until the sim is wired |
| Input Record / Replay | `ISimContext.{Start,Stop}InputRecording` | Stub |
| Match Scoring | `MatchController` | **Live** (manual) — award a point to a side / reset, to drive the match flow without a sim |

`SimHost.Current` defaults to `NullSimContext`, which fails fast with a clear warning rather than
silent no-ops. When Photon Quantum lands, a Quantum-side component calls `SimHost.Set(this)` to install
the real context and every tool lights up with no UI changes.

## Match flow & place-geometry (front-end)

Driven by `MatchController` (Core-only; UI subscribes to its events), `MatchState` (model), and
`MacroState` (placed geometry). All count-agnostic: scores and game-wins are tracked per **side**
(a list — Singles = 2 solo sides, Doubles = 2 team sides; the sim's N movers aggregate into them).

**Structure:** first-to-11 points wins a **game**; best-of-3 games wins the **match**.

**Edit cadence** (per design): each game, the first time any side reaches **6** → a mid-game edit
window; after each game (someone reaches 11) → a between-game edit window. In every window **each
side places one piece** of geometry. Placed pieces **persist the entire match** and reset per match
(they accumulate — the macro layer of complicating/stripping the arena).

- **Scoreboard** (`UI/Match`): two-side points + best-of-3 game pips + phase label + match-over
  (Rematch / To Menu). UI Toolkit overlay, shown during matches.
- **Place-geometry** (`UI/Macro`): free-3D mouse placement — a ghost follows the cursor on the
  ground plane (y=0); left-click drops. Remove mode: click a placed piece. One piece per side per
  window; "Switch side" + "Done" controls. `PlacedGeometryRenderer` mirrors `MacroState` as
  primitives in the Match scene, tinted per side (perfect information — both sides' pieces visible).
- **Match Scoring** dev tool: award a point to Side A/B or reset — drives the flow until the sim
  exists. Open the Dev Menu (F1) → *Match Scoring*.

> Caveat to verify in Play mode: the place UI ignores clicks over its panel via a screen-space
> bounds check with a Y-axis flip (UI Toolkit `worldBound` is top-left origin; Input System mouse is
> bottom-left). If clicking the panel also drops a piece, that flip is the place to revisit.

## Settings (front-end)

`UI/Settings` — Display (resolution, fullscreen, quality, V-Sync) + Audio (master/SFX/music),
persisted via PlayerPrefs. Master volume → `AudioListener.volume`; SFX/Music are stored and need an
AudioMixer to take effect (later). Opened from the Main Menu's **Settings** button.

## Determinism notes (load-bearing)

- `Tunables` holds **float proxies** for feel-tuning/presentation only. The sim consumes fixed-point
  (`Quantum FP`); these are converted at the sim boundary. Never read `Tunables` inside the sim.
- `OverlaySettings` and `MacroState` are presentation/host state and must never feed back into the
  sim. No hidden-information mechanics (brief §1.3); the overlays default **ON**; both sides'
  placed geometry is always visible.

## Next steps (out of scope for this shell)

- Install Photon Quantum 3.0.x and implement `QuantumSimContext : ISimContext`.
- Build the sim systems (movement / grapple / spin / offense) per the build sequence.
- Wire real perfect-info renderers to `OverlaySettings`.
- Author Training scenarios + wire the ability-pool half of the macro layer (add/remove shared
  abilities — currently only geometry placement is built).
