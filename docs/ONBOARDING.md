# Project Onboarding

This document defines the initialization interview process for this template. Follow it when `CLAUDE.md` detects un-initialized docs, or when the user runs `/init`.

**Goal**: fill in `docs/REQUIREMENTS.md`, `docs/ARCHITECTURE.md`, `docs/CONVENTIONS.md`, `docs/WORKFLOWS.md`, and `docs/REVIEWERS.md` with enough real content that the AI has accurate, actionable context for all future work on this project.

Before starting, read `CLAUDE.md` and all `docs/` files to understand the current state.

## When to run

- **Auto**: on first open, if any doc file still contains TODO placeholders — surface a one-sentence prompt ("This project hasn't been initialized yet — want me to run the setup interview now?") and wait for the user's response. Don't start the interview unrequested.
- **Manual**: user runs `/init` — start immediately.
- **Re-run**: if the docs are already filled in, confirm with the user before overwriting. Note what will change.

## Interview stages

The interview is broken into stages with checkpoints. At the end of each stage, summarize what you gathered and confirm before moving to the next. Each stage can be picked back up in a later session — if stopping partway through, note where you left off in a brief comment at the bottom of the doc being filled in.

### Stage 0 — Project type and scope

Ask first:

> "Quick orientation before we fill in the docs — which of these fits best?
> 1. **New project** — starting from scratch; nothing exists yet.
> 2. **Existing codebase** — code already exists; we're documenting what's there.
> 3. **Design/planning phase** — no code yet, but requirements and ideas to capture."

Then:
- What kind of project is it? (web app, CLI tool, library/SDK, backend service, data pipeline, mobile app, other)
- Rough team size: solo, small team (2–5), larger?

Record the answers in `docs/ARCHITECTURE.md`'s Overview section as they arrive. They calibrate the rest of the interview — for example, solo projects get simpler branching/review questions; existing codebases skip "what will you use?" in favor of "what are you using?"; new projects may have more TBDs in requirements.

**Checkpoint**: confirm the project type before continuing.

### Stage 1 — Requirements (fills `docs/REQUIREMENTS.md`)

Cover:
- What does this system do? (core user-facing or developer-facing behavior — what problem it solves and for whom)
- Who are the primary users or consumers?
- The most important non-functional constraints: performance targets, uptime/reliability requirements, security or compliance mandates, accessibility standards
- What is explicitly out of scope — what should this system *not* do?

Push for specifics, not aspirations. "Fast" is not a requirement; "p99 latency < 200ms for the search endpoint" is. If a detail isn't known yet, mark it TBD with a note about when it will be decided (not "TODO" — that marker means un-initialized).

**Checkpoint**: read back the requirements list and confirm accuracy before filling in the doc.

After confirmation, fill in `docs/REQUIREMENTS.md` — replace all TODOs with real content. Mark unknowns as TBD with context.

### Stage 2 — Architecture (fills `docs/ARCHITECTURE.md`)

Cover:
- Tech stack: language(s), framework(s), major libraries — and whether these are decided or still open
- System shape: how does data flow in? how does it flow out? what processes, services, or layers are involved?
- Key architectural decisions already made and the reasoning behind them (e.g., "we chose Postgres because the data is relational and we need transactions" — not just the what, the why)
- Alternatives considered and rejected for each key decision
- External dependencies: third-party services, APIs, SDKs, and why they're there
- Anything about the design that would surprise a reader, or that will likely be misunderstood

For existing codebases, ask what the original design intent was and whether the current state has drifted from it — note any drift explicitly.

**Checkpoint**: walk through the component map and key decisions; confirm before filling in the doc.

After confirmation, fill in `docs/ARCHITECTURE.md` — replace all TODOs with real content.

### Stage 3 — Conventions & workflows (fills `docs/CONVENTIONS.md` + `docs/WORKFLOWS.md`)

For conventions:
- Language version and package manager (exact versions where possible)
- Linter and formatter: tool name, config file location, deviations from defaults
- Naming conventions: files, functions/methods, classes, constants, database tables, etc.
- Error handling philosophy: throw vs. return, logging approach, what surfaces to users vs. stays internal
- Testing: framework, where tests live, what must be tested, coverage expectations
- Dependency management: how to add/remove/audit; any banned or approved libraries

For workflows:
- Branching model (trunk-based, gitflow, feature branches off main, etc.) and branch naming convention
- Development loop: "I have an idea" → "it's merged" — what are the actual steps?
- Code review bar: who reviews, what automated checks must pass before merge
- Deployment: environment names, how code gets to each environment, any manual steps, rollback procedure
- Release process if any: versioning scheme, changelog generation, artifact publishing
- Incident response if applicable: on-call rotation, escalation path, runbook location

For solo projects, be appropriately lightweight — don't invent process overhead that won't survive first contact with reality.

**Checkpoint**: summarize conventions and workflow; confirm before filling in.

After confirmation, fill in `docs/CONVENTIONS.md` and `docs/WORKFLOWS.md` — replace all TODOs with real content.

### Stage 4 — Brainstorm and create reviewer personas (fills `docs/REVIEWERS.md`)

These are the lenses the `/review` command will apply to every staged change. The goal is a roster of sharp, project-specific reviewers — not a generic checklist. Do this in two steps.

**Step 1 — Brainstorm with the user**

Ask:

> "Before I propose reviewers, I want to make sure I'm covering what actually matters for this project. Two questions:
> 1. What are you most worried about getting wrong — what failure mode would be the most painful or hardest to recover from?
> 2. Is there any domain (security, data integrity, performance, UX, compliance, etc.) where you know the bar needs to be especially high, or where past projects have burned you?"

Use the answers, combined with everything learned in Stages 1–3, to identify the domains that need dedicated reviewers for this project. Think through:
- What are the highest-stakes parts of this system? (data loss, auth failures, SLA breaches, public API breakage, etc.)
- What constraints from `docs/REQUIREMENTS.md` demand ongoing enforcement? (latency targets, compliance mandates, accessibility standards)
- What architectural decisions from `docs/ARCHITECTURE.md` need a guardian? (schema migrations, API versioning, caching correctness)
- What failure modes are non-obvious and easy to miss in code review?

**Step 2 — Propose the full roster**

Build the reviewer list from two sources and combine them:

*From the standard set (include only what genuinely applies — omit the rest):*
- **Security Reviewer** — almost always relevant; OWASP Top 10, auth/authz, secrets handling, input validation
- **Performance Reviewer** — when latency/throughput requirements exist; query efficiency, caching, algorithmic complexity
- **API Contract Reviewer** — for public or client-facing APIs; backward compatibility, versioning, documentation accuracy
- **Data Model Reviewer** — for data-heavy projects; schema design, migration safety, query correctness
- **Accessibility/UX Reviewer** — for user-facing UI; accessible markup, keyboard nav, contrast, responsive layout
- **Reliability Reviewer** — for services with uptime requirements; error handling, retry/backoff, graceful degradation

*From the brainstorm (invent new personas that fit what this project specifically needs):*
Create custom reviewers for any domain the standard set doesn't cover — for example: a **Compliance Reviewer** for a project with regulatory requirements, a **Cost Reviewer** for a project with cloud spend constraints, a **Developer Experience Reviewer** for an SDK or CLI tool, a **Concurrency Reviewer** for a high-throughput system, a **Migration Safety Reviewer** for a project with frequent schema changes. Name each one clearly and write their Focus/Mandate/Will-flag specifically for this project's constraints — don't copy the examples verbatim.

Fewer sharp reviewers beat a long list of generic ones. If a standard reviewer doesn't add value for this project, leave it out. If the brainstorm surfaces something the standard set misses, create it.

**Checkpoint**: present the full proposed roster with Focus/Mandate/Will-flag for each reviewer, clearly indicating which are standard and which are custom. Explain the reasoning for each inclusion and omission. Get explicit approval or edits before finalizing.

After confirmation, fill in `docs/REVIEWERS.md`.

## Finalize

After all stages are complete:

1. Re-read all five docs and check for internal consistency — ARCHITECTURE's tech choices should match CONVENTIONS' tooling; REQUIREMENTS' constraints should be reflected in ARCHITECTURE's decisions; WORKFLOWS' review bar should match the project's team size.
2. Propose any cross-doc fixes before closing.
3. Draft a commit message summarizing the initialization and ask whether to commit.

## Reviewer template

When defining a reviewer in `docs/REVIEWERS.md`, use this exact shape:

```
**[Domain] Reviewer**
- Focus: <one sentence — what aspect of the codebase or change this reviewer examines>.
- Mandate: <the non-negotiable bar for quality in this domain, stated as a concrete standard>.
- Will flag: <specific failure modes, anti-patterns, or omissions this reviewer will call out>.
```

Tone requirements:
- Each reviewer is direct and domain-specific — no vague approval or encouragement.
- Findings name the file, line, or pattern; state why it's a problem; suggest the fix or the question to answer.
- A reviewer who lacks enough context to give a useful opinion says so explicitly rather than producing generic feedback.
