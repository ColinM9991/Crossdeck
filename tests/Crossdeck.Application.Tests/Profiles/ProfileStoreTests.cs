using Crossdeck.Application.Routing.Output;
using Crossdeck.Core.Profiles;

namespace Crossdeck.Application.Tests.Profiles;

public sealed class ProfileStoreTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);
        var type = GetProfileStoreType();

        // Act / Assert
        assertion.Verify(type.GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public async Task LoadAsync_WhenNoProfileExistsWithTheId_ReturnsNotFound(
        Guid id,
        [Frozen] IProfileSerializer profileSerializer)
    {
        // Arrange
        var sut = CreateStore(profileSerializer);

        // Act
        var result = await sut.LoadAsync(id, CancellationToken.None);

        // Assert
        Assert.Equal(new ProfileLoadResult.NotFound(), result);
        Assert.Empty(profileSerializer.ReceivedCalls());
    }

    [Theory, AutoNSubstituteData]
    public async Task DeleteAsync_WhenNoProfileExistsWithTheId_CompletesWithoutError(
        Guid id,
        IProfileSerializer profileSerializer)
    {
        // Arrange
        var sut = CreateStore(profileSerializer);

        // Act
        var deleteTask = sut.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.Same(Task.CompletedTask, deleteTask);
        await deleteTask;
    }

    private static IProfileStore CreateStore(IProfileSerializer profileSerializer)
    {
        var type = GetProfileStoreType();

        return (IProfileStore)Activator.CreateInstance(type, profileSerializer)!;
    }

    private static Type GetProfileStoreType()
    {
        return typeof(SimValueRouter).Assembly
            .GetType("Crossdeck.Application.Profiles.ProfileStore")
            ?? throw new InvalidOperationException();
    }
}
