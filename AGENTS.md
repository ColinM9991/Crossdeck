## graphify

This project has a knowledge graph at `graphify-out/` (graph.json, GRAPH_REPORT.md, graph.html, cache/) with god nodes, community structure, and cross-file relationships. It is maintained in place, and the shell hook announces it at command start.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- `graphify query "<question>"` is the first tool for any codebase question: run it before your first grep, glob, search, file listing or source-file read. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. They return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output. Read raw files only for lines the graph or an earlier read already located.
- Do not verify `graphify-out/` with a directory listing or glob before using it; the directory's presence is a given of this project, and the `graphify` CLI (on PATH) confirms it live: its output states the graph path and node count, and an error tells you the graph is missing. If a query seems to miss recently changed code, run `graphify update .` and re-query instead of falling back to raw browsing.
- `graphify` is a command-line program on PATH, not a tool in the agent's toolset. Invoke it only through shell commands. Never attempt a direct tool call named `graphify`, and never conclude it is unavailable because it is absent from the tool list.
- Probe with the first shell invocation of the context and judge the probe by its output, not its exit code alone: a live `query` announces `Graph: <path> (N nodes)` at the top of its output; an `explain` call exits non-zero with an `Ambiguous:` line when a symbol matches multiple files, which is a disambiguation request resolved by retrying with `<path>::<Symbol>` or the full node ID, not a failure; only a missing CLI or a missing graph (a `graph file not found` error) is a genuine probe failure.
- When a query result is truncated, raise the budget (`graphify query "<question>" --budget <tokens>`) or narrow the query (context_filter, get_node for a specific symbol) before resorting to raw file reads.
- A failure to use graphify (a failed CLI call, or reaching a raw grep/glob/source read before the first query) stops the context immediately: flag the issue in the conversation and do not continue that context, and never substitute raw file browsing to cover for it. A subagent that fails to use graphify stops and reports the failure to its parent, and the parent does not accept results derived from raw browsing in that context.
- Put these same rules in every subagent prompt that explores code.
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

## Build and test

- `dotnet build` and `dotnet test` take a single project or solution argument per invocation (MSB1008 otherwise). Run the command once per project.
- A hung `dotnet test` (a test that never completes) leaves a `testhost` process holding the test assembly, which blocks later builds with MSB3027. Find the stuck chain with `Get-CimInstance Win32_Process` (match `CommandLine` on `testhost` or the test assembly name) and stop it child-first before rebuilding. In pwsh, do not use `$pid` as a loop variable; it is reserved.
- `dotnet test --no-build` runs whatever is in `bin`. If you edited that project's sources since its last build, build it first or you are verifying a stale binary.

## State file

- `STATE.md` at the repo root is the single working-state file. It is gitignored and transient: a tally of progress for the agent, not documentation. Never commit it.
- One file, reused for every activity: when a new activity starts, replace its contents with that activity's state rather than accumulating stale activities.
- Contents: objective; a checklist of the work items in scope (each type, file or scope, with its status); progress through each stage; hazards and known defects; persistence rank (this file's helpfulness as the persistence layer, re-ranked at each stage boundary); the next move.
- Update timings, in order:
  - When the activity starts: write the objective, then fill in the work-item checklist as the plan solidifies.
  - When the picture is understood and a plan is proposed: record the plan as the work-item checklist with statuses, plus any design notes or hazards discovered.
  - After each stage of the plan completes: mark that stage's work items and refresh hazards and the next move.
- Keep it current during the activity, not just at the end; a stale `STATE.md` after a completed stage is a defect.
- Before completing each stage, rank the helpfulness of `STATE.md` as the persistence layer for resuming this task, and record the rank with a one-line reason in `STATE.md`. Every agent does this, including subagents, not just the primary one.
- Use a fixed 1-5 scale so ranks are comparable across agents: 1 = redundant with the auto-generated compaction summary or already stale; 5 = the only reliable way to resume without re-deriving state. A low rank mid-task is a warning that the persistence layer is not doing its job, and a stale file misdirects the next agent, so re-rank whenever a new activity starts.
- On session start, or after any compaction, read `STATE.md` first to pick up the tally of progress, and say in the conversation that it is the source of truth so the compaction checkpoint references it.

## Git

- Do not commit or push unless the user asks. Leave the changes in the working tree, staged, and report which files changed.