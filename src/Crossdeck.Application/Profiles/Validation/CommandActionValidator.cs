using Crossdeck.Core.Mapping.Input;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class CommandActionValidator : AbstractValidator<InputAction.Command>
{
    public CommandActionValidator(string bindingId)
    {
        RuleFor(x => x.Ref)
            .Must(simRef => simRef is SimRef.Plain)
            .WithMessage(x => $"Only sim commands can be sent; '{x.Ref.Key}' is {Describe(x.Ref)}")
            .WithState(_ => bindingId);
    }

    private static string Describe(SimRef simRef) => simRef switch
    {
        SimRef.Variable => "a variable",
        SimRef.Output => "a computed output",
        _ => "a sim reference"
    };
}
