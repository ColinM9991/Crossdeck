using Crossdeck.Core;

namespace Crossdeck.Host.Tests;

public sealed class SupervisorServiceTests
{
    [Theory, AutoNSubstituteData]
    public async Task StartAsync_Always_RunsTheGivenRunnableUntilStopped(IRunnable runner)
    {
        // Arrange
        var sut = new SupervisorService<IRunnable>(runner);
        var runnableStarted = new TaskCompletionSource();
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                runnableStarted.SetResult();
                return Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>());
            });

        // Act
        await sut.StartAsync(CancellationToken.None);
        await runnableStarted.Task;
        await sut.StopAsync(CancellationToken.None);

        // Assert
        await runner.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }
}
