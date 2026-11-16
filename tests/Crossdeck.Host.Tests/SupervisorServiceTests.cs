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
