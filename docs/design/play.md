# Play

> Design doc for the **Play** front-end menu. Status: **designed, not implemented.**
> Front-end menus live entirely in the Unity view/presentation layer — they never mutate
> Quantum simulation state. A menu only produces *inputs* (here: a session config) that other
> systems consume; it holds no gameplay truth.

## Purpose

Primary entry point into a match. Lets the player pick **player count (N)**, **intensity
(casual / ranked)**, and **session type (matchmaking / private)**, then hands a session
config to the netcode layer, which loads the sandbox-morphs-to-match flow.

## Navigation

- **Entry:** Main Menu → Play.
- **Exits:**
  - On confirm → matchmaking search → sandbox → match (in-match).
  - On Private Match → private lobby screen.
  - Back → Main Menu.

## Items

- **1v1 — Casual** — mirror match, P2P rollback + referee (locked for v1).
- **1v1 — Ranked** — server-authoritative referee.
- **4-player — Casual** — authoritative server (ships day one; First Test Aspect #5 measured
  ~77% frame-budget headroom at the 4-rope worst case).
- **4-player — Ranked** — authoritative server.
- **Private Match** — invite / lobby code; pick 1v1 or 4-player; select a ruleset (built-in,
  or a custom one from the **Rulesets** menu — see `rulesets.md`).
- **Ruleset info** — short preview of the selected mode (rounds-per-match, win condition).

## State & data

- Menu state is Unity UI state only.
- On confirm, packages a **session config** `{ N, ruleset, ranked: bool, private: bool }`
  and hands it to the netcode/matchmaking layer.
- Does **not** touch the Quantum sim until the match loads. The sandbox morph is driven by
  the match-flow system, not by this menu.

## Determinism / architecture notes

- **Player count is a parameter, not a branch.** Selecting 1v1 vs 4-player sets `N`; the sim
  code is identical at any N (Requirement NFR #5: count-agnostic). The menu must never spawn
  "1v1 logic" vs "4p logic" — only set the parameter.
- **Ruleset differences (casual vs ranked) live in referee/host config**, never in the sim.
  The deterministic sim has one correct path; ranked vs casual differ only in who runs it and
  how inputs are validated.
- Per Decision 4 (ARCHITECTURE.md): 1v1 → **P2P + referee** (locked for casual v1; ranked may
  still route through a server); 4-player → authoritative server. The Play menu chooses
  topology indirectly via N + ranked flag.

## Requirements coverage

- FR: Match & macro structure — "players spawn into a sandbox that morphs to load a match."
- NFR #4: Server-authoritative truth — ranked/hosted sessions take the authoritative path.
- NFR #5: Count-agnostic simulation — Play picks N; sim is unchanged.

## Open questions

- *(Resolved)* Casual 1v1 → **P2P + referee** (per Decision 4; revisitable with hosting).
- *(Resolved)* Rounds-per-match → **configurable, owned by the ruleset** (not the host, not
  per-player).
- Open: the default rounds-per-match and win-condition shape (best-of-N vs score-to-win) are
  ruleset-design questions, settled when the first ruleset is authored.
