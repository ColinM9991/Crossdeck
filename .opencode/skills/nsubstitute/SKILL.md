---
name: NSubstitute
description: >-
  NSubstitute mocking patterns for this repo's unit tests (xUnit + AutoFixture +
  NSubstitute 5.x, with the Crossdeck.Testing helpers). USE FOR: writing or
  reviewing unit tests that mock an interface or collaborator with NSubstitute -
  creating substitutes, arranging return values, raising events, verifying
  calls, or combining AutoFixture AutoNSubstitute data with NSubstitute. DO NOT
  USE FOR: tests that use no mocks, other mocking frameworks, or production
  (non-test) code.
---

# NSubstitute

Mocking guidance for this repository's test stack: xUnit, AutoFixture, and
NSubstitute 5.x, using the shared `tests/Crossdeck.Testing` helpers. Load this
only when the unit test work actually mocks a collaborator with NSubstitute.
Skip it for tests with no mocks or for another mocking framework.

## Usings

- `using NSubstitute;` provides `Substitute.For<T>()`, `Arg`, and the
  `.Returns` / `.ReturnsForAnyArgs` family.
- `.Throws(...)` and `.ThrowsAsync(...)` live in the
  `NSubstitute.ExceptionExtensions` namespace. Add
  `using NSubstitute.ExceptionExtensions;` next to `using NSubstitute;`, or the
  call will not resolve (the `NSubstitute` namespace alone does not import it).
- `Raise` (event raising) is a static class in the `NSubstitute` namespace, so
  `using NSubstitute;` is enough for it.

## Raising events

In NSubstitute 5.x the classic `sub.Raise(e => e.SomeEvent += null, args)`
extension method is gone. Raise an event by adding a delegate produced by
`Raise.Event` to the event:

```csharp
// EventHandler<TEventArgs> event
watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(
    watcher, new DeviceArrivedEventArgs(descriptor));

producer.Ready += Raise.Event<EventHandler<SimReady>>(producer, new SimReady());
```

`Raise.Event<THandler>(sender, args)` returns a `THandler` delegate. Assigning it
with `+=` makes the substitute raise the event to every other handler already
subscribed. Match the generic argument to the exact delegate type of the event
(`EventHandler<TEventArgs>`, or the custom delegate type).

## Placement of triggers (Arrange vs Act)

A `+= Raise.Event(...)` line looks like a subscription, so keep them to the
minimum and move as many as possible into `// Arrange`:

- The SUT subscribes in its constructor (typically writing into an internal
  command channel): fire the event in `// Arrange`. The command buffers until
  the run drains it, so the ordering is preserved.
- The SUT subscribes only mid-run (a handler subscribes to the event when a
  command is processed, e.g. `SimValueRouter` wiring `producer.Ready` in
  `HandleSimulatorChanged`): the event must fire live in `// Act`, after the
  subscription point. Synchronise with a `TaskCompletionSource` completed by a
  stub callback or `logger.When(...).Do(...)`, await it, then fire the event.
- Do not add a stopper trigger (a `Changing`-style event fired only to end the
  run). Cancel the test's own `CancellationTokenSource` instead, so `// Act`
  stays focused on the behaviour under test and the stop is a plain
  `cts.Cancel()`.

## Arranging returns

- `sub.Method(arg).Returns(value)` pins a return for matching arguments.
- `sub.Method(Arg.Any<T>()).Returns(value)` matches any argument.
- For a `Task<T>` use `Returns(Task.FromResult(value))` or an async lambda.
- When the value must come from the call arguments, use
  `Returns(info => ...)` and read `info.Arg<T>()` / `info.ArgAt<T>(n)`.
- Stubbing an async stream the SUT iterates (`IAsyncEnumerable<T>`, a
  `Channel<T>.Reader`): forward the caller's cancellation token into the
  stubbed enumeration. `producer.GetEventsAsync(Arg.Any<CancellationToken>())
  .Returns(call => channel.Reader.ReadAllAsync(call.ArgAt<CancellationToken>(0)))`
  is the pattern. Dropping the token means the SUT's cancellation never reaches
  the loop, its `RunAsync` never completes, and the test hangs the whole run
  (a stuck `testhost` then locks the build output too).

## AutoFixture + NSubstitute (repo convention)

- Test methods take their collaborators as parameters under
  `[Theory, AutoNSubstituteData]` (from `Crossdeck.Testing`); the attribute
  supplies AutoFixture-configured NSubstitute instances.
- Mark an instance `[Frozen]` so the same substitute is reused across
  parameters and is the one the system under test actually receives.
- For `GuardClauseAssertion` (AutoFixture.Idioms) build a fixture that resolves
  constructor parameters to substitutes, then verify:

```csharp
var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
var assertion = new GuardClauseAssertion(fixture);

assertion.Verify(typeof(Sut).GetConstructors());
```

`AutoNSubstituteCustomization` is from the `AutoFixture.AutoNSubstitute`
package; `AutoNSubstituteDataAttribute` and `ReportDescriptorCustomization` are
in `Crossdeck.Testing`.

## Unarranged members return null (`ConfigureMembers = false`)

The shared fixture (`AutoNSubstituteDataAttribute` in `Crossdeck.Testing`)
builds its customization with `ConfigureMembers = false`, so an unarranged
member on a substitute returns `null` for reference types and `default` for
value types. NSubstitute's built-in auto values still apply to async
members: an unarranged `Task`/`Task<T>`/`ValueTask` member returns a
completed task, so an unarranged async method never hangs (an unarranged
`IAsyncEnumerable<T>` member returns `null` and fails fast when the SUT
iterates it, rather than producing an endless auto stream).

This is deliberate. Under the package default (`ConfigureMembers = true`) an
unarranged reference member auto-returns a non-null auto-generated proxy: an
unarranged `IActiveSim` auto-returned a phantom `ISimProducer` from
`Current`, `SimValueRouter.RunAsync` branched on `Current is not null`,
started an event loop for the phantom, whose auto-substituted
`GetEventsAsync` never completes, and the test hung (a stuck `testhost`
then locked the build output). The null default turns the same mistake into
a fast failure: a plainly wrong code path or an immediate NRE instead of a
hang.

Rules:

- Arrange explicitly any member the SUT reads when it branches on the value
  or iterates a stream from it. `activeSim.Current.Returns(producer);` when
  the scenario has an active simulator;
  `activeSim.Current.Returns((ISimProducer?)null);` when it has none. Cast
  the null to the member's nullable type so the `Returns` overload resolves.
- A member the SUT only calls fire-and-forget (a `void` or `Task` whose
  result is never read) is safe to leave unarranged: the substitute yields
  a completed task and a default value.
- Precedents: `SimValueRouterTests` (both directions),
  `InputDispatcherTests` (all four run-loop tests arrange `activeSim.Current`).

## Verifying

- `await sub.Received(1).Method(...)` asserts a call count;
  `await sub.DidNotReceive().Method(...)` asserts none.
- Keep `Received` / `DidNotReceive` checks in the `// Assert` block, after the
  `// Act` block.

## House test rules

Test files follow `agents/unit-testing.md`: AAA block markers only
(`// Arrange`, `// Act`, `// Assert`), `MethodUnderTest_WithScenario_ReturnsExpectedResult`
naming, and no other comments.
