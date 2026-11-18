using AutoFixture.Xunit2;
using Crossdeck.Core;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Host.Tests;

public sealed class SupervisorServiceTests
{
    [Theory, AutoNSubstituteData]
    public async Task StartAsync_Always_RunsTheGivenRunnableUntilStopped(
        IRunnable runner,
        ILogger<SupervisorService<IRunnable>> logger)
    {
        // Arrange
        var sut = new SupervisorService<IRunnable>(runner, logger);
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

    [Theory, AutoNSubstituteData]
    public async Task StopAsync_Never_Throws_WhenTheRunnableFaults(
        InvalidOperationException fault,
        IRunnable runner)
    {
        // Arrange
        var logEntries = new List<LogEntry>();
        var logger = new CapturingLogger<SupervisorService<IRunnable>>(logEntries);
        var sut = new SupervisorService<IRunnable>(runner, logger);
        var runnableFaulted = new TaskCompletionSource();
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                runnableFaulted.SetResult();
                return Task.FromException(fault);
            });

        // Act
        await sut.StartAsync(CancellationToken.None);
        await runnableFaulted.Task;
        await sut.StopAsync(CancellationToken.None);

        // Assert
        Assert.Contains(logEntries, entry => entry.Level == LogLevel.Error && entry.Exception == fault);
    }

    [Theory, AutoNSubstituteData]
    public async Task StartAsync_WhenTheRunnableFaults_RestartsItAfterTheBackoff(
        InvalidOperationException fault,
        IRunnable runner)
    {
        // Arrange
        var logEntries = new List<LogEntry>();
        var logger = new CapturingLogger<SupervisorService<IRunnable>>(logEntries);
        var sut = new SupervisorService<IRunnable>(runner, logger, TimeSpan.FromMilliseconds(50));

        var secondRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runCount = 0;
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                runCount++;
                if (runCount == 1)
                {
                    return Task.FromException(fault);
                }

                secondRunStarted.TrySetResult();
                return Task.Delay(Timeout.Infinite, call.ArgAt<CancellationToken>(0));
            });

        // Act
        await sut.StartAsync(CancellationToken.None);
        await secondRunStarted.Task;
        await sut.StopAsync(CancellationToken.None);

        // Assert
        await runner.Received(2).RunAsync(Arg.Any<CancellationToken>());
        Assert.Contains(logEntries, entry => entry.Level == LogLevel.Error && entry.Exception == fault);
    }

    [Theory, AutoNSubstituteData]
    public async Task StopAsync_WhenTheHostStopsAfterTheRunnableFaults_DoesNotRestartIt(
        InvalidOperationException fault,
        IRunnable runner)
    {
        // Arrange
        var logEntries = new List<LogEntry>();
        var logger = new CapturingLogger<SupervisorService<IRunnable>>(logEntries);
        var sut = new SupervisorService<IRunnable>(runner, logger, TimeSpan.FromMinutes(5));

        var runnableFaulted = new TaskCompletionSource();
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                runnableFaulted.TrySetResult();
                return Task.FromException(fault);
            });

        // Act
        await sut.StartAsync(CancellationToken.None);
        await runnableFaulted.Task;
        await sut.StopAsync(CancellationToken.None);

        // Assert
        await runner.Received(1).RunAsync(Arg.Any<CancellationToken>());
        Assert.Contains(logEntries, entry => entry.Level == LogLevel.Error && entry.Exception == fault);
    }

    [Theory, AutoNSubstituteData]
    public async Task StartAsync_WhenTheRunnableCompletesNormally_DoesNotRestartIt(
        IRunnable runner,
        [Frozen] ILogger<SupervisorService<IRunnable>> logger)
    {
        // Arrange
        var sut = new SupervisorService<IRunnable>(runner, logger, TimeSpan.FromMilliseconds(50));

        var runCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runner.RunAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                runCompleted.TrySetResult();
                return Task.CompletedTask;
            });

        // Act
        await sut.StartAsync(CancellationToken.None);
        await runCompleted.Task;
        await Task.Delay(100);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        await runner.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }

    private readonly record struct LogEntry(LogLevel Level, Exception? Exception, string Message);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _entries;

        public CapturingLogger(List<LogEntry> entries) => _entries = entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string?> formatter)
            => _entries.Add(new LogEntry(logLevel, exception, formatter(state, exception) ?? string.Empty));
    }
}
