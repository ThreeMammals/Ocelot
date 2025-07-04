namespace Ocelot.Infrastructure.Extensions;

public static class StringExtensions
{
    /// <summary>Removes the prefix from the beginning of the string repeatedly until all occurrences are eliminated.</summary>
    /// <param name="source">The string to trim.</param>
    /// <param name="prefix">The prefix string to remove.</param>
    /// <param name="comparison">The 2nd argument of the <see cref="string.StartsWith(string)"/> method.</param>
    /// <returns>A new <see cref="string"/> without the prefix all occurrences.</returns>
    public static string TrimPrefix(this string source, string prefix, StringComparison comparison = StringComparison.Ordinal)
    {
        if (source == null || string.IsNullOrEmpty(prefix))
        {
            return source;
        }

        var s = source;
        while (s.StartsWith(prefix, comparison))
        {
            s = s[prefix.Length..];
        }

        return s;
    }

    public const char Slash = '/';

    /// <summary>Ensures that the last char of the string is forward slash, '/'.</summary>
    /// <param name="source">The string to check its last slash char.</param>
    /// <returns>A <see cref="string"/> witl the last forward slash.</returns>
    public static string LastCharAsForwardSlash(this string source)
        => source.EndsWith(Slash) ? source : source + Slash;

    public static string Plural(this int count) => count == 1 ? string.Empty : "s";
    public static string Plural(this string source, int count) => count == 1 ? source : string.Concat(source, "s");
}
