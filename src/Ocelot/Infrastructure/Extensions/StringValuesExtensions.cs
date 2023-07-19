using System.Linq;

using Microsoft.Extensions.Primitives;

namespace Ocelot.Infrastructure.Extensions
{
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
}
