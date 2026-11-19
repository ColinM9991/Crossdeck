namespace Crossdeck.Core.Mapping;

public closed record ExpressionSpec
{
    public sealed record Raw(string Expr) : ExpressionSpec;
};