## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- When graphify-out/graph.json exists, run `graphify query "<question>"` before your first grep, search or source-file read for a codebase question, and put this rule in every subagent prompt that explores code. Read raw files only for lines the graph or an earlier read already located. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- A committed graphify-out/graph.json is rewritten whole on every build, so a large diff to it in a pull request is expected. Do not flag it as churn or suggest skipping the graph rebuild or its hooks.

## Communication style

Applies to all written communication.

- Never use em-dashes. Use a comma, full stop, or separate sentence instead.
- Use correct grammar and spelling in UK English (e.g. "colour", "organise", "prioritise").
- Prioritise clarity. Be concise and direct. Avoid AI fluff: no filler praise, no restating the question, no unnecessary hedging or preamble.
- Use consistent terminology. If something has a name, use that exact name every time rather than switching to a synonym or paraphrase.
- Back claims with evidence: cite sources, logs, test output, or data rather than speculating. State explicitly when something is an assumption rather than a verified fact.
- When information is missing, flag the gap and ask or say so, rather than filling it in with a guess.
- Avoid buzzwords and jargon where a plain word works.
- Avoid hyperbole and poetic or flowery language. Describe things plainly.

## Code comments

- Do not write comments that restate what the code does or narrate a change. In production code the only comments are XMLDOC (`///`) on types and members; any other comment must carry information the code itself cannot, such as a non-obvious workaround or a `TODO` marking missing work.
- Exception: the `// Arrange` / `// Act` / `// Assert` block markers that `agents/unit-testing.md` requires in test files.

## C# style

- The C# style rules are in `agents/csharp-style.md`. Read that file in full before you write or modify C# code, and follow it exactly. Do not read it for any other task.

## Unit testing

- The unit testing rules are in `agents/unit-testing.md`. Read that file in full before you write, change or review unit tests, and follow it exactly. Do not read it for any other task.

## Markdown

- The markdown rules are in `agents/markdown.md`. Read that file in full before you write or edit a markdown document, and follow it exactly. Do not read it for any other task.

## .NET skills (dotnet/skills)

Microsoft's .NET agent skills are vendored as a git submodule at `.opencode/skills/dotnet-skills` (dotnet/skills, branch `main`). The seven wired-in plugins are `dotnet`, `dotnet-advanced`, `dotnet-msbuild`, `dotnet-diag`, `dotnet-test`, `dotnet-nuget` and `dotnet-upgrade`; the `permissions` array in `.opencode/opencode.json` allows only their skills, so the rest of the submodule is invisible to the model.

- Load a skill with the `skill` tool only when the current activity matches, at most one or two per task.
- The full list, grouped by activity, is in `agents/dotnet-skills.md`. Read it only when picking a skill for a .NET task.
- Updating: `git submodule update --remote --recurse-submodules .opencode/skills/dotnet-skills`, then `pwsh ./sync-dotnet-skills.ps1` to regenerate the `permissions` rules and `agents/dotnet-skills.md`.

## Git

- Do not commit or push unless the user asks. Leave the changes in the working tree, staged, and report which files changed.