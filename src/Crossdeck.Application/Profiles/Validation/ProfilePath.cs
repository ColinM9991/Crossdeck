using System.Text;
using System.Text.RegularExpressions;

namespace Crossdeck.Application.Profiles.Validation;

public static partial class ProfilePath
{
    public static string FromPropertyName(string propertyName)
    {
        var pointer = new StringBuilder();
        var previousWasKeyed = false;
        foreach (var segment in propertyName.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (previousWasKeyed && segment is "Value" or "Key")
            {
                previousWasKeyed = false;
                continue;
            }

            previousWasKeyed = segment.Contains('/');
            pointer.Append('/').Append(ConvertSegment(segment));
        }

        return pointer.Length == 0 ? "/" : pointer.ToString();
    }

    [GeneratedRegex(@"\[(\d+)\]")]
    private static partial Regex ArrayIndex();

    private static string ConvertSegment(string segment)
    {
        var nameLength = segment.AsSpan().IndexOfAny('/', '[');
        if (nameLength < 0)
            nameLength = segment.Length;

        var name = char.ToLowerInvariant(segment[0]) + segment[1..nameLength];
        return name + ArrayIndex().Replace(segment[nameLength..], "/$1");
    }
}
