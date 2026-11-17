using Crossdeck.Core.Mapping.Input;

namespace Crossdeck.Application.Routing;

internal sealed class InputBinding
{
    public InputBinding(
        InputMapping mapping)
    {
        Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
    }

    public InputMapping Mapping { get; }
}