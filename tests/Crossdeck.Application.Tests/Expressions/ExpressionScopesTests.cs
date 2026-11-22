using Crossdeck.Application.Expressions;
using Crossdeck.Core.Mapping.Expressions;

namespace Crossdeck.Application.Tests.Expressions;

public sealed class ExpressionScopesTests
{
    [Theory, AutoNSubstituteData]
    public void InputGuard_WithSourceAliases_ReturnsAGuardScopeOfTheAliases(
        string first,
        string second)
    {
        // Act
        var scope = ExpressionScopes.InputGuard([first, second]);

        // Assert
        Assert.Equal(ExpressionKind.Guard, scope.Kind);
        Assert.Equal([first, second], scope.Names);
    }

    [Theory, AutoNSubstituteData]
    public void InputValue_WithSourceAliases_ReturnsAValueScopeOfTheAliasesAndSelf(
        string first,
        string second)
    {
        // Act
        var scope = ExpressionScopes.InputValue([first, second]);

        // Assert
        Assert.Equal(ExpressionKind.Value, scope.Kind);
        Assert.Equal([first, second, "self"], scope.Names);
    }

    [Theory, AutoNSubstituteData]
    public void OutputGuard_WithSourceAliases_ReturnsAGuardScopeOfTheAliases(
        string first,
        string second)
    {
        // Act
        var scope = ExpressionScopes.OutputGuard([first, second]);

        // Assert
        Assert.Equal(ExpressionKind.Guard, scope.Kind);
        Assert.Equal([first, second], scope.Names);
    }

    [Theory, AutoNSubstituteData]
    public void OutputTransform_WithSourceAliases_ReturnsAValueScopeOfTheAliases(
        string first,
        string second)
    {
        // Act
        var scope = ExpressionScopes.OutputTransform([first, second]);

        // Assert
        Assert.Equal(ExpressionKind.Value, scope.Kind);
        Assert.Equal([first, second], scope.Names);
    }
}
