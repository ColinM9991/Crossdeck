using Crossdeck.Core.Mapping;

namespace Crossdeck.Application.Routing;

internal sealed class OutputBinding
{
    public OutputBinding(
        OutputMapping outputMapping)
    {
        Mapping = outputMapping ?? throw new ArgumentNullException(nameof(outputMapping));
    }

    public OutputMapping Mapping { get; }
}