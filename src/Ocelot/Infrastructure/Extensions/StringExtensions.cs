namespace Ocelot.Infrastructure.Extensions;

public static class StringExtensions
{
    /// <summary>Indicates whether a specified string is <see langword="null"/>, empty, or consists only of white-space characters.</summary>
    /// <remarks>This is shortcut for the <see cref="string.IsNullOrWhiteSpace(string?)"/> method.</remarks>
    /// <param name="str">The string to test.</param>
    /// <returns><see langword="true"/> if the <paramref name="str"/> parameter is <see langword="null" /> or <see cref="string.Empty"/>, or if <paramref name="str"/> consists exclusively of white-space characters.</returns>
    public static bool IsEmpty(this string str) => string.IsNullOrWhiteSpace(str);

    /// <summary>Defaults to the default string if the current string is null or empty.</summary>
    /// <remarks>Based on the <see cref="string.IsNullOrWhiteSpace(string?)"/> method.</remarks>
    /// <param name="str">The current string.</param>
    /// <param name="def">The default string.</param>
    /// <returns>The <paramref name="def"/> string if <paramref name="str"/> is empty; otherwise, the <paramref name="str"/> string.</returns>
    public static string IfEmpty(this string str, string def) => string.IsNullOrWhiteSpace(str) ? def : str;

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
