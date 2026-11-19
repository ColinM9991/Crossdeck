using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Core.Mapping.Expressions;

public interface IValueTransform
{
    OutputValue? Apply(IReadOnlyDictionary<string, SimValue?> sources);
}