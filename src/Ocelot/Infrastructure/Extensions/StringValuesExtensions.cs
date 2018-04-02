using Microsoft.Extensions.Primitives;
using System.Linq;

namespace Ocelot.Infrastructure.Extensions
{
    internal static class StringValueExtensions
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
