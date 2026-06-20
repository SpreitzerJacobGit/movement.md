# Quit

> Design doc for the **Quit** front-end menu item. Status: **designed, not implemented.**
> Included for completeness — single action, no design depth.

## Purpose

Exit the application.

## Navigation

- **Entry:** Main Menu → Quit.
- **Exits:** → desktop (optionally via a confirm dialog; see Open questions).

## Items

- **Quit** — closes the application.
- *(Conditional)* **Confirm dialog** — only when a match or sandbox session is live, to avoid
  accidental abandonment. From the idle Main Menu, Quit exits immediately.

## State & data

None.

## Determinism / architecture notes

None. No sim interaction. Quitting mid-match forfeits the session; any in-flight input
stream is finalized by the server/referee, not by the quitting client.

## Requirements coverage

None directly.

## Open questions

- **Confirm-dialog policy:** recommend confirm only when a live session exists (match or
  sandbox in progress). From the idle main menu, quit immediately. Needs confirmation during
  implementation.
