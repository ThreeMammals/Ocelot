using Microsoft.Extensions.Primitives;

namespace Ocelot.Tracing.Butterfly;

public static class StringValuesExtensions
{
    public static string GetValue(this StringValues stringValues)
    {
        if (stringValues.Count == 1)
        {
            return stringValues;
        }

        return stringValues.ToArray().LastOrDefault();
    }
}
