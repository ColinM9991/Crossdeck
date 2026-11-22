using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Profiles.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class InputDefinitionValidator : AbstractValidator<KeyValuePair<InputId, InputDefinition>>
{
    public InputDefinitionValidator(Profile profile, IExpressionCompiler compiler)
    {
        RuleFor(x => x.Value.Id)
            .NotEmpty()
            .WithMessage("Binding id is empty")
            .WithState(x => x.Value.Id);

        RuleFor(x => x.Value.Events)
            .NotEmpty()
            .WithMessage("Input has no events; it does nothing")
            .WithSeverity(Severity.Warning)
            .WithState(x => x.Value.Id);

        RuleForEach(x => x.Value.Sources)
            .OverrideIndexer((_, _, source, _) => $"/{source.Key}")
            .SetValidator(x => new SourceValidator(profile, x.Value.Id));

        RuleForEach(x => x.Value.Events)
            .OverrideIndexer((_, _, entry, _) => $"/{entry.Key}")
            .SetValidator(x => new InputEventValidator(profile, compiler, x.Value));
    }
}
