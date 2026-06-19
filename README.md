# ai-dev-template

A batteries-included starting point for AI-driven software projects. Clone this repo, fill in the placeholder docs, and drop your own code alongside them — Claude Code (and other AI assistants) will pick up the context automatically.

## What's in the box

| File / Folder | Purpose |
|---|---|
| `CLAUDE.md` | Entry point for Claude Code — loads all other docs and sets working conventions |
| `docs/REQUIREMENTS.md` | Functional and non-functional requirements |
| `docs/ARCHITECTURE.md` | System design, key decisions, component map |
| `docs/CONVENTIONS.md` | Coding style, naming rules, formatting standards |
| `docs/WORKFLOWS.md` | Development workflows: branching, review, deploy |
| `docs/REVIEWERS.md` | Project-specific reviewer personas used by `/review` — filled in during `/init` |
| `docs/ONBOARDING.md` | The initialization interview process — defines what `/init` does |
| `.claude/settings.json` | Claude Code hooks and permission allow-list scaffold |
| `.claude/commands/` | Custom slash commands — `/init` and `/review` pre-seeded, add your own |

## How to use this template

1. **Clone / fork** this repo into your new project directory.
2. **Open it with Claude Code** — the AI will detect the un-initialized docs and prompt you to run the setup interview. Say yes, or run `/init` yourself.
3. **Answer the interview questions** — the AI fills in the `docs/` files from your answers and proposes a set of project-specific reviewers for the `/review` command.
4. **Add your code** — `CLAUDE.md` is already wired up; the AI has context from the start.
5. **Remove or rename** any docs that don't apply; add new ones and register them in `CLAUDE.md`.

### Manual setup (without the interview)

If you'd rather fill in the docs yourself, replace all `TODO` placeholders in `docs/` directly. The AI will stop prompting for initialization once no TODOs remain. You can still run `/init` later to get reviewer suggestions.

## Philosophy

- Every doc file has a single responsibility.
- `CLAUDE.md` is the index — it should stay short and just point elsewhere.
- Prefer plain prose over rigid templates; the AI reads intent, not just structure.
