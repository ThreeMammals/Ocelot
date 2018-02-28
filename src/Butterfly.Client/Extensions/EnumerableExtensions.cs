using System.Collections.Generic;
using System.Linq;

namespace Butterfly.Client
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerable<IEnumerable<T>> Chunked<T>(this IEnumerable<T> source, int chunkedCapacity)
        {
            while (source.Any())
            {
                yield return source.Take(chunkedCapacity);
                source = source.Skip(chunkedCapacity);
            }
        }
    }
}
