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

## Unit testing

- The unit testing rules are in `agents/unit-testing.md`. Read that file in full before you write, change or review unit tests, and follow it exactly. Do not read it for any other task.

## Project context

- Crossdeck syncs flight simulators with cockpit hardware in both directions. Sim values go to device outputs (LEDs, CDU screens, segment displays). Device inputs go to sim commands. Simulators: X-Plane (UDP and the 12.1+ Web API) and MSFS (SimConnect, Windows only, behind `#if`). Hardware: WINWING panels and CDUs over HID, MOZA FCD over serial plus HID, other HID devices.
- The user builds the application. Agents act as consultants: advise, review, answer questions and help plan. No agent writes or builds code, scaffolds projects or opens pull requests unless the user explicitly asks for that specific thing, and then only `developer` does it.
- One topic per task. If a request is not clearly part of the current task, ask the user whether it belongs to the current task or should be a new one, and wait for the answer. Do not fold it into the current task on a guess.
- Projects, their references and their tests: infer them from the graph (`graphify query`, `graphify explain`) at the start of a task. Do not rely on a fixed list.
- Dependency rule: Routing and Profiles may depend on Devices and Simulators, never the reverse. `Crossdeck.Core` holds interfaces, records and enums only. Implementations live in `Crossdeck.Application` or the device and sim projects.
- Design pages are HTML under `docs/`. Recorded decisions: `docs/decisions.html`, `docs/decisions-devices.html`, `docs/decisions-output.html`. Specs: `docs/spec-*.html`. Diagram colours: `docs/files/diagram-legend.md`. `docs/files/Contracts.cs` and `docs/files/interfaces.md` are early design sketches, not the code.
- Vendor sources of truth: WINWING behaviour comes from SimAppPro (the vendor app), as recorded on the `docs/winwing-*.html` pages. MOZA FCD behaviour comes from `docs/files/vendor/moza/fcd-display/` (developer guide and example scripts). Anything else is unverified and is labelled "community" at best.
