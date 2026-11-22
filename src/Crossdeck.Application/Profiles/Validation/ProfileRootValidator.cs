using System.Text.RegularExpressions;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Simulators.Models;
using FluentValidation;
using FluentValidation.Results;

namespace Crossdeck.Application.Profiles.Validation;

public sealed partial class ProfileRootValidator : AbstractValidator<Profile>
{
    public ProfileRootValidator(IExpressionCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);

        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty)
            .WithMessage("Profile id is empty");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Profile name is empty");

        RuleFor(x => x.Aircraft.Matches)
            .NotEmpty()
            .WithMessage("No aircraft match; the profile is only activated by hand")
            .WithSeverity(Severity.Warning);

        RuleForEach(x => x.Variables)
            .OverrideIndexer((_, _, entry, _) => $"/{entry.Key}")
            .SetValidator(new VariableValidator());

        RuleForEach(x => x.Outputs)
            .OverrideIndexer((_, _, entry, _) => $"/{entry.Key}")
            .Must(entry => VariableValidator.NamePattern().IsMatch(entry.Key))
            .WithMessage((_, entry) => $"Output name '{entry.Key}' must start with a letter or _ and contain only letters, digits and _")
            .WithState((_, entry) => entry.Value.Id);

        RuleForEach(x => x.Outputs)
            .OverrideIndexer((_, _, entry, _) => $"/{entry.Key}")
            .SetValidator(profile => new OutputDefinitionValidator<string>(profile, compiler));

        RuleForEach(x => x.Devices)
            .OverrideIndexer((_, _, entry, _) => $"/{entry.Key}")
            .SetValidator(profile => new DeviceSlotValidator(profile, compiler));

        RuleFor(x => x).Custom(ValidateOutputCycles);
        RuleFor(x => x).Custom(ValidateBindingIdsAreUnique);
    }

    private static void ValidateOutputCycles(Profile profile, ValidationContext<Profile> context)
    {
        foreach (var (key, definition) in profile.Outputs)
        {
            var cycle = FindCycle(profile, key);
            if (cycle is null)
                continue;

            context.AddFailure(new ValidationFailure(
                $"Outputs/{key}",
                $"Output '{key}' is part of a cycle: {string.Join(" → ", cycle)}")
            {
                CustomState = definition.Id
            });
        }
    }

    private static List<string>? FindCycle(Profile profile, string start)
    {
        var paths = new Dictionary<string, List<string>>();
        var queue = new Queue<string>();
        paths[start] = [start];
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in OutputReferences(profile, current))
            {
                if (next == start)
                    return [.. paths[current], start];

                if (paths.ContainsKey(next))
                    continue;

                paths[next] = [.. paths[current], next];
                queue.Enqueue(next);
            }
        }

        return null;
    }

    private static IEnumerable<string> OutputReferences(Profile profile, string key) =>
        profile.Outputs[key].Sources.Values
            .OfType<SimRef.Output>()
            .Select(reference => reference.Id)
            .Where(profile.Outputs.ContainsKey);

    private static void ValidateBindingIdsAreUnique(Profile profile, ValidationContext<Profile> context)
    {
        var seen = new HashSet<string>();
        foreach (var (path, id) in EnumerateBindings(profile))
        {
            if (seen.Add(id))
                continue;

            context.AddFailure(new ValidationFailure(path, $"Binding id '{id}' is used more than once")
            {
                CustomState = id
            });
        }
    }

    private static IEnumerable<(string Path, string Id)> EnumerateBindings(Profile profile)
    {
        foreach (var (key, definition) in profile.Outputs)
            yield return ($"Outputs/{key}", definition.Id);

        foreach (var (slotKey, slot) in profile.Devices)
        {
            foreach (var (inputId, definition) in slot.Inputs)
                yield return ($"Devices/{slotKey}.Value.Inputs/{inputId}", definition.Id);

            foreach (var (outputId, definition) in slot.Outputs)
                yield return ($"Devices/{slotKey}.Value.Outputs/{outputId}", definition.Id);
        }
    }
}
