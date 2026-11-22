using Crossdeck.Core.Mapping.Expressions;

namespace Crossdeck.Application.Expressions;

public static class ExpressionScopes
{
    private const string SelfName = "self";

    public static ExpressionScope InputGuard(IEnumerable<string> sourceAliases) =>
        new([.. sourceAliases], ExpressionKind.Guard);

    public static ExpressionScope InputValue(IEnumerable<string> sourceAliases) =>
        new([.. sourceAliases, SelfName], ExpressionKind.Value);

    public static ExpressionScope OutputGuard(IEnumerable<string> sourceAliases) =>
        new([.. sourceAliases], ExpressionKind.Guard);

    public static ExpressionScope OutputTransform(IEnumerable<string> sourceAliases) =>
        new([.. sourceAliases], ExpressionKind.Value);
}
