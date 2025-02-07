using NeoSmart.Unicode;
using System.Text;

namespace StreamSchedule.Extensions;

public static class CodepointExtensions
{
    public static string ToStringRepresentation(this ReadOnlySpan<Codepoint> input)
    {
        StringBuilder result = new(input.Length);
        foreach (var codepoint in input)
        {
            result.Append(codepoint.AsString());
        }
        return result.ToString();
    }
}
