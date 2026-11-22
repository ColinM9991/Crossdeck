using System.Globalization;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Profiles.Models;
using FluentValidation;

namespace Crossdeck.Application.Profiles.Validation;

public sealed class DeviceSlotValidator : AbstractValidator<KeyValuePair<string, DeviceSlot>>
{
    public DeviceSlotValidator(Profile profile, IExpressionCompiler compiler)
    {
        RuleFor(x => x.Value.Model.Vid)
            .Must(IsFourHexDigits)
            .WithMessage("VID must be 4 hex digits");

        RuleFor(x => x.Value.Model.Pid)
            .Must(IsFourHexDigits)
            .WithMessage("PID must be 4 hex digits");

        RuleFor(x => x.Value.Assigned)
            .NotNull()
            .WithMessage("No device assigned; this slot's bindings are not active")
            .WithSeverity(Severity.Warning);

        RuleFor(x => x.Value.Assigned)
            .Must((entry, assigned) => assigned!.Value.VendorId == ParseHex(entry.Value.Model.Vid))
            .When(x => x.Value.Assigned is not null && IsFourHexDigits(x.Value.Model.Vid))
            .WithMessage(x => $"Assigned device VID {x.Value.Assigned!.Value.VendorId:X4} does not match the slot model {x.Value.Model.Vid}");

        RuleForEach(x => x.Value.Inputs)
            .OverrideIndexer((_, _, entry, _) => $"/{entry.Key}")
            .SetValidator(_ => new InputDefinitionValidator(profile, compiler));

        RuleForEach(x => x.Value.Outputs)
            .OverrideIndexer((_, _, entry, _) => $"/{entry.Key}")
            .SetValidator(_ => new OutputDefinitionValidator<OutputId>(profile, compiler));
    }

    private static bool IsFourHexDigits(string value) =>
        value is { Length: 4 } && int.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _);

    private static int ParseHex(string value) =>
        int.Parse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
}
