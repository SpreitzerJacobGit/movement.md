# CLAUDE.md

This file is the entry point for Claude Code and other AI assistants working in this repository. Read the files listed below before starting any task — they contain the conventions, architecture, and requirements that should guide all decisions.

## Is this project initialized?

Check whether any file in `docs/` still contains a `TODO` placeholder. If so, this project hasn't been set up yet. Surface a one-sentence prompt — "This project hasn't been initialized yet — want me to run the setup interview?" — and wait for the user's response before doing anything else. The full initialization process is in [`docs/ONBOARDING.md`](./docs/ONBOARDING.md); the `/init` command triggers it directly.

## Required reading

| Doc | What it covers |
|---|---|
| [`docs/REQUIREMENTS.md`](./docs/REQUIREMENTS.md) | Functional and non-functional requirements — the "what" |
| [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) | System design, component map, key decisions — the "how" |
| [`docs/CONVENTIONS.md`](./docs/CONVENTIONS.md) | Coding style, naming, formatting, tooling — the "how consistently" |
| [`docs/WORKFLOWS.md`](./docs/WORKFLOWS.md) | Branching, review, deploy, and release process — the "when and who" |
| [`docs/REVIEWERS.md`](./docs/REVIEWERS.md) | Project-specific reviewer personas used by `/review` — populated during `/init` |
| [`docs/ONBOARDING.md`](./docs/ONBOARDING.md) | The initialization interview process — read this if running `/init` |

## Working conventions

- **Ask before assuming structure.** If a task's scope is unclear, ask one targeted question rather than guessing.
- **Prefer editing existing files** over creating new ones. Only create a new file when the task clearly requires it.
- **No drive-by cleanup.** A bug fix should not also refactor unrelated code. Keep changes scoped to the task.
- **No speculative features.** Implement what is asked; don't add capabilities "just in case."
- **No comments that explain what the code does** — good names do that. Comments are reserved for non-obvious *why*.
- **Security first.** Never introduce command injection, XSS, SQL injection, or other OWASP Top 10 vulnerabilities. Fix any you notice, even if not in scope.

## Decision-making

### Gaps in understanding

When something is genuinely unknown — intent is ambiguous, a constraint isn't stated, or proceeding requires an assumption about priorities or business logic — **stop and ask one short, targeted question** rather than guessing and building on a wrong foundation. State what is known, what is unclear, and why it matters to the decision at hand. Do not make multiple guesses in a row; one precise question is more useful than a paragraph of hedged options.

Do not ask about things that can be derived from the code, the docs, or sensible defaults. Only escalate what the user is actually the right person to answer.

### Architectural decisions

For any decision that affects the system's structure, major dependencies, data flow, or long-term maintainability — before committing to an approach:

1. **Enumerate 2–3 concrete options** (the real contenders, not an exhaustive list).
2. **For each, state:** what it is, why it could be the right choice, and the meaningful tradeoff or risk.
3. **Make a recommendation** — don't end on "it depends." Pick the option that best fits the project's stated requirements and constraints, and say why.
4. **Wait for confirmation** before building — structural decisions are expensive to undo.

This applies to: framework/library selection, data modeling, API shape, service boundaries, auth strategy, caching, deployment architecture, and anything else that's hard to reverse.

### Applying changes

- **Low-risk changes** (wording, doc updates, formatting, small refactors within scope) can be applied directly.
- **Structural changes** (new files, folder reorganization, dependency additions, convention changes) should be proposed and confirmed before making them.

## After major work

After completing any significant piece of work:

1. Review staged changes against `docs/REQUIREMENTS.md` and `docs/ARCHITECTURE.md` — flag any drift.
2. Apply each reviewer in `docs/REVIEWERS.md` to the staged changes — surface findings grouped by reviewer. Skip this step if `REVIEWERS.md` hasn't been initialized yet (run `/init` to populate it).
3. Check for security issues (OWASP Top 10: injection, XSS, broken auth, etc.). Fix any found, even if out of scope.
4. Verify tests exist for new behavior; note any gaps.
5. Draft a concise commit message: one imperative-mood subject line, optional body for non-obvious context.
6. Show the message and ask whether to commit (and optionally push) — do not commit automatically.

You can also run `/review` (a custom slash command) to trigger this checklist.

## Hooks

This repo is pre-configured with a Claude Code hooks scaffold in `.claude/settings.json`. Hooks run shell commands on events — useful for notifications, enforcement, or logging. Edit the arrays there to add your own:

- **Stop** — runs when Claude finishes a turn (e.g. `notify-send "Claude done"`, play a sound)
- **PreToolUse** — runs before a tool call; exit non-zero to block it
- **PostToolUse** — runs after a tool call
- **Notification** — runs when Claude needs user input

Each entry shape: `{"type": "command", "command": "your-shell-command"}`. Add `"matcher": "ToolName"` to target a specific tool.

## Slash commands and subagents

Custom slash commands live in `.claude/commands/`. Each `.md` file becomes `/command-name` — the file content is the prompt template. `/init` (project setup interview) and `/review` (pre-commit checklist) are pre-seeded; add more for your repetitive workflows.

For tasks with independent sub-questions (research, multi-file analysis), the `Agent` tool can spawn parallel subagents so the main context stays clean. Use it when a task would otherwise require 3+ exploratory tool calls that don't depend on each other.

## Adding new docs

If you need a doc that isn't listed above, create it under `docs/`, give it a single clear responsibility, and add a row to the table in this file.
