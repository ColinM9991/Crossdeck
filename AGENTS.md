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

- Framework: xUnit.
- Global usings: every test project declares its common namespaces once in a `GlobalUsings.cs` at the project root (xUnit, NSubstitute, `Crossdeck.Testing`, and the AutoFixture family / `Microsoft.Extensions.DependencyInjection` where the project references them). Don't redeclare these per file, and don't add a namespace to a suite's `GlobalUsings.cs` unless that project references the package providing it.
- Naming: `MethodUnderTest_WithScenario_ReturnsExpectedResult` (underscore-separated three-part name). The method name alone should describe the system's behaviour without needing to read the body.
- No comments beyond `// Arrange` / `// Act` / `// Assert`: write clean, self-explanatory test code instead — descriptive test/variable names, small focused helpers, no clever one-liners — so no other comment is ever needed. Never add a comment explaining why a test, a guard-clause test, or a particular edge case was skipped, or why an assertion is sufficient; that reasoning belongs in the PR description or chat, not the file.
- Structure: Arrange, Act, Assert, each block marked with a `// Arrange` / `// Act` / `// Assert` comment and separated by a blank line. Within Arrange, group statements by concern and put a blank line between each distinct concern (e.g. token/cancellation setup, one dependency's stubbed behaviour, another dependency's stubbed behaviour, captured state) rather than one unbroken block — the `// Arrange` comment goes once, above the first concern. Consecutive `Assert.*` calls that verify the same overall outcome stay together with no blank line between them.
- Async cancellation: inside an async test body, or an NSubstitute `.Returns(...)` callback for a member that already returns `Task`/`Task<T>`, use the async form when one exists (`await cts.CancelAsync()`, not `cts.Cancel()`). A callback for a genuinely synchronous member (e.g. `IHidReportEncoder.Encode`) stays synchronous — don't introduce `async`/`await` there just to reach `CancelAsync`.
- Test data and dependency injection: use xUnit's `[Theory]`-driven AutoData style (`AutoFixture.Xunit2`'s `AutoDataAttribute`), not manual `new Fixture()` calls inside the test body. Each test project defines its own `[AutoNSubstituteData]` attribute (subclassing `AutoDataAttribute`, constructed with a fixture customized via `AutoNSubstituteCustomization` from `AutoFixture.AutoNSubstitute`) and test methods declare `[Theory, AutoNSubstituteData]` with the values/substitutes/SUT they need as method parameters.
  - Any parameter that's incidental to the scenario (the test doesn't care what the value is, only that it exists/varies) is just declared as a parameter — AutoFixture supplies it. Never hand-pick arbitrary literals like `"foo"` or `42` for data the test doesn't actually assert on.
  - Interface-typed parameters are auto-substituted by NSubstitute via the customization. Mark a dependency parameter `[Frozen]` when the same substitute instance also needs to be the one injected into the SUT parameter (so you can configure/verify it after the fact).
  - Only fall back to building a `Fixture`/`Substitute.For<T>()` by hand inside the test body when a value must be a specific, assertion-relevant literal, or when wiring can't be expressed through the attribute.
- Complex/navigation properties: the base fixture used by `[AutoNSubstituteData]` sets `OmitAutoProperties = true` and swaps `ThrowingRecursionBehavior` for `OmitOnRecursionBehavior`. AutoFixture should only populate a type through its constructor, never by walking and auto-filling its mutable properties — that's what blows up on EF-style navigation properties and circular object graphs (entity A references B which references A). If a test needs a specific property/navigation set, generate that value as its own AutoData parameter and assign it onto the owning object explicitly in Arrange (e.g. `order.Customer = customer;`), rather than relying on the fixture to populate it automatically.
- Coverage shape: cover both the happy-path input/output and the edge cases (nulls, empty collections, boundary values, first-occurrence vs repeat, etc.) as separate tests, not one parameterized test trying to prove everything at once unless a `[Theory]` genuinely represents the same behaviour across inputs.
- No branching in tests: a test must not contain `if`/`switch`/ternary logic that makes the expected outcome depend on runtime values. If a scenario needs branching to describe the expected result, it is two scenarios — split it into two tests.
- Guard clauses: for any SUT with constructor dependencies, add one `[Fact] Constructors_VerifyGuardClauses` test using `AutoFixture.Idioms`' `GuardClauseAssertion` against `typeof(Sut).GetConstructors()`, instead of writing a separate null-argument test per constructor parameter.
- Every test must document real system behaviour. If a test doesn't assert something a reader would care about (a contract, an invariant, a observable side effect), don't write it — it's noise, not coverage.
- Testing a `BackgroundService`: `StartAsync` returning does not mean the overridden `ExecuteAsync` body has reached its first real `await` yet (it schedules the continuation and returns once that continuation yields, e.g. via `Task.Yield()`). Don't call `StopAsync` immediately after `StartAsync` and expect the service's work to have started — synchronize on a signal the mocked dependency sets when its own method is actually invoked (e.g. a `TaskCompletionSource` completed inside a `.Returns(...)` callback), await that, then call `StopAsync`.
- NSubstitute `Returns(...)` overload ambiguity: for a member returning `Task<T>` where `T` is a reference/abstract type (e.g. `Task<CommandResult>`), an `async _ => { await x; return value; }` callback can hit CS1660 because it collides with `ReturnsExtensions`' unwrapped-value sugar overload. Use a synchronous callback instead (`_ => { x.Cancel(); return value; }`) in that case — this is the one place a Task-returning callback stays sync rather than using the async form the cancellation rule above otherwise asks for. This didn't come up for `Task<int>`/sealed value types, only reference/abstract return types.
- SUT accessibility: xUnit's analyzers (xUnit1000) require test classes to be `public`, and CS0051 then forbids an `internal` SUT type appearing as a parameter of a `public` test method. Don't work around it by making the test class `internal` — make the SUT `public` instead. This matches codebase precedent: `HidDeviceDriver` and `WinCtrlOutputWriter` are both `public` sealed classes.
- Raising events under test: never invoke an event via reflection (`EventInfo.GetEventHandler` + `DynamicInvoke`). Drive the real dependency chain instead: stub the transport read loop so the real driver/decoder runs and raises the event the normal way (see `HidDeviceDriverTests` for the established pattern).
- Cancellation in loop tests: when a `.Returns(...)` transport callback cancels a `CancellationTokenSource`, the token passed to the SUT's `RunAsync` must be that same source's token (`sut.RunAsync(cts.Token)`). Passing `CancellationToken.None` instead means the SUT's linked token is never cancelled and the read loop spins forever, hanging the test run.
- NSubstitute throwing async members: NSubstitute 5.x has no `ThrowsAsync` on `Task<T>` return types (CS1061). Stub a throwing async call with `.Returns(Task.FromException<T>(exception))`.
- `[Frozen]` lives in `AutoFixture.Xunit2` — the `using` is required in any test file that declares `[Frozen]` parameters, even when `[AutoNSubstituteData]` (from the same namespace) is resolved by the shared attribute.
