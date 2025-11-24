namespace Ocelot.Infrastructure.Extensions;

public static class IEnumerableExtensions
{
    /// <summary>
    /// Converts a collection of <see cref="string"/> representations of HTTP methods (verbs) into a hashed set of <see cref="HttpMethod"/> objects.
    /// </summary>
    /// <remarks>Note:
    /// <list type="number">
    /// <item>Trims each string in the collection.</item>
    /// <item>Does not throw <see cref="ArgumentNullException"/> if the collection is <see langword="null"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="collection">The collection of HTTP method strings.</param>
    /// <returns>A <see cref="HashSet{HttpMethod}"/> object, where T is <see cref="HttpMethod"/>.</returns>
    public static HashSet<HttpMethod> ToHttpMethods(this IEnumerable<string> collection)
    {
        collection ??= Enumerable.Empty<string>();
        return collection.Select(verb => new HttpMethod(verb.Trim())).ToHashSet();
    }

    /// <summary>
    /// Helper function to convert multiple strings into a comma-separated string aka CSV.
    /// </summary>
    /// <param name="values">The collection of strings to join by comma separator.</param>
    /// <returns>A <see langword="string"/> in the comma-separated format.</returns>
    public static string Csv(this IEnumerable<string> values)
        => string.Join(',', values.NotNull());

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection)
        => collection ?? Enumerable.Empty<T>();
}
