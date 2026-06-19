Review the staged changes. Before starting, read docs/REQUIREMENTS.md, docs/ARCHITECTURE.md, docs/CONVENTIONS.md, and docs/REVIEWERS.md.

Run the review in two parts:

**Part 1 — Project-specific reviewers** (from docs/REVIEWERS.md)
For each reviewer defined there, examine the staged changes from their perspective and surface any findings. Group output by reviewer. Each finding should name the file/line or pattern, state why it's a problem, and suggest a fix or the question to answer.

If docs/REVIEWERS.md hasn't been initialized yet (contains TODO placeholders), note that /init hasn't been run and skip this part.

**Part 2 — Standard checklist**
- Correctness relative to docs/REQUIREMENTS.md — does this change serve the stated requirements, or does it drift from them?
- Consistency with docs/ARCHITECTURE.md and docs/CONVENTIONS.md — naming, structure, patterns
- Security issues (OWASP Top 10: injection, XSS, broken auth, insecure deserialization, etc.) — fix any found, even if out of scope
- Missing tests for new behavior — note gaps

Then draft a concise commit message (one imperative-mood subject line; optional body for non-obvious context) and ask whether to commit.
