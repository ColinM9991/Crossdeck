using Crossdeck.Core;
using Crossdeck.Testing;
using Xunit;

namespace Crossdeck.Core.Tests;

public sealed class SignalTests
{
    [Theory, AutoNSubstituteData]
    public async Task WaitAsync_AfterSet_YieldsSignal(Signal sut)
    {
        // Arrange
        sut.Set();

        // Act
        await using var enumerator = sut.WaitAsync(CancellationToken.None).GetAsyncEnumerator();
        var moved = await enumerator.MoveNextAsync();

        // Assert
        Assert.True(moved);
        Assert.True(enumerator.Current);
    }

    [Theory, AutoNSubstituteData]
    public async Task Set_WhenCalledTwiceBeforeConsumption_CoalescesIntoSingleSignal(Signal sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        sut.Set();
        sut.Set();

        var enumerator = sut.WaitAsync(cts.Token).GetAsyncEnumerator();

        // Act
        var firstMoved = await enumerator.MoveNextAsync();
        await cts.CancelAsync();

        // Assert
        Assert.True(firstMoved);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
    }

    [Theory, AutoNSubstituteData]
    public async Task SetAsync_WhenAlreadyCancelled_ThrowsOperationCanceledException(Signal sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.SetAsync(cts.Token));
    }

    [Theory, AutoNSubstituteData]
    public async Task WaitAsync_WhenCancelledBeforeAnySignal_ThrowsOperationCanceledException(Signal sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in sut.WaitAsync(cts.Token))
            {
            }
        });
    }
}
