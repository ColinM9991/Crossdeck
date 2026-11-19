using System.Collections.Frozen;
using Crossdeck.Core.Devices.Display;
using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Core.Models;
using Crossdeck.Core.Routing.Adapters;
using Crossdeck.Core.Simulators.Models;

namespace Crossdeck.Sim.XPlane.Adapters;

/// <summary>
/// Builds <see cref="TextScreen"/> instances for the Flight Factor 777 v2 CDU displays in X-Plane.
/// </summary>
public sealed class FlightFactor777AircraftScreenAdapter : IAircraftScreenAdapter
{
    private static readonly TextCell BlankCell = new(' ', TextColour.White, TextColour.Black, TextSize.Large);

    /// <inheritdoc/>
    public Simulator Simulator => Simulator.XPlane;

    /// <inheritdoc/>
    public bool CanSatisfy(SimulatorAircraft aircraft) => aircraft.AircraftPath.StartsWith("Boeing 777");

    /// <inheritdoc/>
    public IReadOnlySet<SimRef> GetRequiredRefs(DisplayRole role)
    {
        var device = GetDevice(role);

        return new HashSet<SimRef>
            {
                new SimRef.Plain(SymbolsRef(device)),
                new SimRef.Plain(ArrayRef(device, "symbolsSize")),
                new SimRef.Plain(ArrayRef(device, "symbolsColor")),
                new SimRef.Plain(ArrayRef(device, "symbolsEffects"))
            }
            .ToFrozenSet();
    }

    /// <inheritdoc/>
    public TextScreen? BuildScreen(DisplayRole role, IReadOnlyDictionary<SimRef, SimValue> simValues)
    {
        var device = GetDevice(role);

        if (!simValues.TryGetValue(new SimRef.Plain(SymbolsRef(device)), out var symbolsValue) ||
            symbolsValue is not SimValue.String symbols ||
            symbols.Value.Length < TextScreen.CellsCount)
        {
            return null;
        }

        if (!TryGetArray(simValues, ArrayRef(device, "symbolsSize"), out var sizes) ||
            !TryGetArray(simValues, ArrayRef(device, "symbolsColor"), out var colors) ||
            !TryGetArray(simValues, ArrayRef(device, "symbolsEffects"), out var effects))
        {
            return null;
        }

        var cells = new TextCell[TextScreen.CellsCount];

        for (var i = 0; i < cells.Length; i++)
        {
            var character = symbols.Value[i];

            if (character is ' ' or '\0')
            {
                cells[i] = BlankCell;
                continue;
            }

            cells[i] = new TextCell(
                TranslateCharacter(character),
                TranslateForeground(colors[i]),
                TranslateBackground(effects[i]),
                TranslateSize(sizes[i]));
        }

        return new TextScreen(cells);
    }

    private static bool TryGetArray(
        IReadOnlyDictionary<SimRef, SimValue> simValues,
        string key,
        out double[] values)
    {
        if (simValues.TryGetValue(new SimRef.Plain(key), out var simValue) &&
            simValue is SimValue.NumberArray array &&
            array.Values.Count
            >= TextScreen.CellsCount)
        {
            values = [.. array.Values];
            return true;
        }

        values = [];
        return false;
    }

    private static string GetDevice(DisplayRole role) => role switch
    {
        DisplayRole.Captain => "cduL",
        DisplayRole.FirstOfficer => "cduR",
        DisplayRole.Observer => "cduC",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    private static string SymbolsRef(string device) => $"1-sim/{device}/display/symbols";

    private static string ArrayRef(string device, string field) => $"1-sim/{device}/display/{field}";


    private static char TranslateCharacter(char character) => character switch
    {
        '#' => '☐',
        '*' => '°',
        _ => character
    };

    private static TextColour TranslateForeground(double code) => code switch
    {
        1 => TextColour.White,
        2 => TextColour.Magenta,
        3 => TextColour.Green,
        4 => TextColour.Cyan,
        5 => TextColour.Gray,
        6 => TextColour.Cyan,
        _ => TextColour.White
    };

    private static TextColour TranslateBackground(double effect) =>
        (int)effect == 1 ? TextColour.Gray : TextColour.Black;

    private static TextSize TranslateSize(double size) =>
        (int)size == 2 ? TextSize.Small : TextSize.Large;
}