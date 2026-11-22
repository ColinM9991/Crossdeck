using AutoFixture;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
using NSubstitute;

namespace Crossdeck.Testing;

/// <summary>
/// Registers an <see cref="IExpressionCompiler"/> substitute whose <see cref="IExpressionCompiler.Validate"/>
/// returns success. NSubstitute would otherwise return a default <see cref="ValidationResult"/>, which holds no case.
/// </summary>
public sealed class ExpressionCompilerCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register<IExpressionCompiler>(() =>
        {
            var compiler = Substitute.For<IExpressionCompiler>();
            compiler.Validate(Arg.Any<ExpressionSpec>(), Arg.Any<ExpressionScope>())
                .Returns(ValidationResult.Success());

            return compiler;
        });
    }
}
