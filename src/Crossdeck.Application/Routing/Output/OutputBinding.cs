using Crossdeck.Core.Mapping.Output;

namespace Crossdeck.Application.Routing.Output;

internal sealed class OutputBinding
{
    public OutputBinding(
        OutputMapping outputMapping)
    {
        Mapping = outputMapping ?? throw new ArgumentNullException(nameof(outputMapping));
    }

    public OutputMapping Mapping { get; }
}