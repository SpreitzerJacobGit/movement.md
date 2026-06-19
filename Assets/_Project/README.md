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
| Sandbox | `Sandbox` | 1 | Free-form: no goals, all abilities, add/remove geometry, slow-mo |
| Training | `Training` | 1 | Scripted scenarios with objectives + pass/fail (scenarios TBD) |

Singles and Doubles share one Match scene deliberately — the simulation is count-agnostic, so
switching between them changes player count without reloading the scene.

## Architecture (this layer)

```
_Project/
├── Core/          GameMode · ModeConfig · AppFlow (mode state machine + additive scene orchestration)
├── Sim/           ISimContext boundary · NullSimContext stub · Sim accessor (Quantum installs later)
├── Presentation/  OverlaySettings (perfect-info flags, read by the renderer later)
├── UI/            UITheme · MainMenu · HUD (uGUI) · DevMenu (UI Toolkit)
├── Dev/           IDevTool registry · Tunables SO · the six dev tools
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
| Input Record / Replay | `ISimContext.{Start,Stop}InputRecording` | Stub |

`SimHost.Current` defaults to `NullSimContext`, which fails fast with a clear warning rather than
silent no-ops. When Photon Quantum lands, a Quantum-side component calls `SimHost.Set(this)` to install
the real context and every tool lights up with no UI changes.

## Determinism notes (load-bearing)

- `Tunables` holds **float proxies** for feel-tuning/presentation only. The sim consumes fixed-point
  (`Quantum FP`); these are converted at the sim boundary. Never read `Tunables` inside the sim.
- `OverlaySettings` is presentation state and must never feed back into the sim.
- No hidden-information mechanics may be added here (brief §1.3); the overlays default **ON**.

## Next steps (out of scope for this shell)

- Install Photon Quantum 3.0.x and implement `QuantumSimContext : ISimContext`.
- Build the sim systems (movement / grapple / spin / offense) per the build sequence.
- Wire real perfect-info renderers to `OverlaySettings`.
- Author Training scenarios + the between-round macro UI.
