# Credits

> Design doc for the **Credits** front-end menu. Status: **designed, not implemented.** Pure
> view layer; no sim interaction.

## Purpose

Attribution screen. Standard scrolling credits.

## Navigation

- **Entry:** Main Menu → Credits.
- **Exits:** Back → Main Menu (also auto-return at scroll end, optional).

## Items

- Scrolling credits: design, code, art (TBD), audio (TBD), tooling (Unity 6000.3.17f1, Photon
  Quantum 3.0.11), special thanks.
- Skip / Back.

## State & data

Static content only. No persistence, no state.

## Determinism / architecture notes

None. Pure Unity UI; never touches the Quantum sim.

## Requirements coverage

None directly.

## Open questions

- Content is TBD — populate at ship. Auto-return-at-end vs require explicit Back: pick during
  implementation (low-stakes; default to explicit Back).
