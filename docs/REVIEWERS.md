# Reviewers

> Lenses applied by `/review` to staged changes. This roster is **proposed** — pulled from the
> brief's hard constraints and implementation principles. Tell me what to add, cut, or sharpen.
> Standard personas are included only where they earn their place; the rest are custom to this
> project's specific risks.

## Custom (project-specific)

**Determinism Reviewer** *(custom — the project's first-order risk)*
- Focus: every line that touches simulation state, for anything that could make two runs (or a
  rollback re-sim) diverge bit-for-bit.
- Mandate: the sim is a pure function of inputs. No floating point in the sim step; fixed-point
  (`FP`/`FPVector3` or equivalent) only. No wall-clock, no unseeded randomness, no DPI/frame-
  timing/iteration-order dependence. Spin integrates swept *angle*, never angular velocity.
- Will flag: a `float`/`double` reaching sim state; resolution order driven by hash-map/spatial-
  hash iteration instead of **entity-ID sort**; reads from rendering/OS/network inside the step;
  any "close enough" tolerance comparison where a bit-exact diff is required; spin code keyed on
  angular velocity or elapsed time.

**Count-Agnostic Reviewer** *(custom — protects the 1v1↔4-player "same code" rule)*
- Focus: whether systems remain agnostic to player count.
- Mandate: no system assumes a fixed count; everything iterates over `N` entities. 1v1 and
  4-player must run identical code at different `N`.
- Will flag: `player1`/`player2` references, hard-coded `2`, special-cased two-player paths,
  data structures sized to a fixed count, anything that would need rewriting to go from 2 movers
  to 4 (including rope-rope pairing that doesn't generalize to n-choose-2).

**Frame-Budget / Rollback-Cost Reviewer** *(custom — the number that decides 4-player ships)*
- Focus: per-tick simulation cost and its multiplication under rollback, especially the
  rope-rope solver (1 pair at 2 players → 6 at 4).
- Mandate: a 4-rope step plus a realistic rollback re-sim must fit the frame budget at **128 Hz**.
  Cost, not determinism, gates 4-player.
- Will flag: per-tick allocations / GC pressure in the sim, O(n²) work that isn't the
  irreducible n-choose-2 pairing, unbounded solver iteration counts, anything that inflates
  re-sim cost (rollback re-runs the whole step), and missing/❌ frame-time measurement for sim
  changes.

**Perfect-Information Integrity Reviewer** *(custom — guards the core design thesis)*
- Focus: changes to gameplay information exposure and round-start symmetry.
- Mandate: everything the design says is visible stays visible (through-wall position light, look
  cone, spin recording + playback tell — "a tell for pretty much everything"). Both players start
  every round in identical state; asymmetry is only shaped geometry + the shared ability pool.
- Will flag: any hidden-information mechanic, a removed/weakened tell, asymmetric round-start
  state, or a per-player ability that isn't drawn from the shared pool.

**Scope & Principle Discipline Reviewer** *(custom — enforces the brief's working rules)*
- Focus: whether a change stays surgical and within the brief.
- Mandate: don't overengineer; one way, no fallbacks; clarity over compatibility; surgical
  changes only; no speculative features; no build-sequence work before the First Test passes.
- Will flag: drive-by refactors outside the task, added fallback/alternate code paths, speculative
  "just in case" features, comments explaining *what* (not *why*), and any build work attempted
  while the determinism gate is still open.

## Standard (included where they apply)

**Security Reviewer** *(standard — netcode + server authority make this real)*
- Focus: input handling, server-authoritative reconciliation, and any future hosting/transport
  surface.
- Mandate: validate and bound all decoded inputs; the server sim is the only truth on
  disagreement; no client-trusted state. Standard OWASP-class hygiene (injection, secrets
  handling) on any tooling/build/server code.
- Will flag: unvalidated/unbounded input decode, client-authoritative state that should be
  server-checked, trusting predicted remote inputs as truth, command/path injection in tooling
  or container/deploy scripts, and secrets committed to the repo.

## Omitted (and why)

- **API Contract Reviewer** — no public/client-facing API yet; revisit if a modding or server API
  emerges.
- **Data Model / Migration Reviewer** — no persistent datastore; sim state is transient and
  input-derived.
- **Accessibility/UX Reviewer** — premature; the renderer is downstream of an unproven sim.
  Reintroduce when UI work begins.
- **Reliability Reviewer** — folded into the Frame-Budget and Security reviewers until a live
  service exists.
