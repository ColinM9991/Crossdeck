using System.Globalization;
using Crossdeck.Core.Mapping;
using Crossdeck.Core.Mapping.Expressions;
using Crossdeck.Core.Simulators.Models;
using NCalc;

namespace Crossdeck.Application.Expressions;

internal sealed class NCalcValueTransform : IValueTransform
{
    private readonly Expression _expression;

    public NCalcValueTransform(
        Expression expression)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public OutputValue? Apply(IReadOnlyDictionary<string, SimValue?> sources)
    {
        if (_expression.Parameters.Any(x => !sources.ContainsKey(x.Key)))
            return null;

        foreach (var source in sources)
        {
            _expression.Parameters[source.Key] = source.Value switch
            {
                SimValue.Number num => num.Value,
                SimValue.String val => val.Value,
                _ => throw new InvalidOperationException()
            };
        }

        var result = _expression.Evaluate();

        return result switch
        {
            bool b => new OutputValue.Boolean(b),
            string s => new OutputValue.Text(s),
            byte or sbyte or short or ushort
                or int or uint or long or ulong
                or float or double
                or decimal => new OutputValue.Number(Convert.ToDouble(result, CultureInfo.InvariantCulture)),
            _ => null
        };
    }
}