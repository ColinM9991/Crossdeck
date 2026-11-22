using Crossdeck.Application.Expressions;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Profiles.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class OutputDefinitionValidator<TKey> : AbstractValidator<KeyValuePair<TKey, OutputDefinition>>
{
    public OutputDefinitionValidator(Profile profile, IExpressionCompiler compiler)
    {
        RuleFor(x => x.Value.Id)
            .NotEmpty()
            .WithMessage("Binding id is empty")
            .WithState(x => x.Value.Id);

        RuleFor(x => x.Value.Sources)
            .NotEmpty()
            .WithMessage("Output has no sources")
            .WithState(x => x.Value.Id);

        RuleForEach(x => x.Value.Sources)
            .OverrideIndexer((_, _, source, _) => $"/{source.Key}")
            .SetValidator(x => new SourceValidator(profile, x.Value.Id));

        RuleFor(x => x.Value.Transform)
            .MustBeValidExpression(
                compiler,
                x => ExpressionScopes.OutputTransform(x.Value.Sources.Keys),
                x => x.Value.Id);

        RuleFor(x => x.Value.When)
            .MustBeValidExpression(
                compiler,
                x => ExpressionScopes.OutputGuard(x.Value.Sources.Keys),
                x => x.Value.Id);

        RuleFor(x => x.Value.Transform)
            .NotNull()
            .When(x => x.Value.Sources.Count > 1)
            .WithMessage(x => $"Output has {x.Value.Sources.Count} sources but no transform to combine them")
            .WithState(x => x.Value.Id);
    }
}
