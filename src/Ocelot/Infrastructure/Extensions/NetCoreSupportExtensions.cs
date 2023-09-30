namespace Ocelot.Infrastructure.Extensions
{
    /// <summary>
    /// Trivial implementations of methods present in .NET Core 2 but not supported on .NET Standard 2.0.
    /// </summary>
    internal static class NetCoreSupportExtensions
    {
        internal static void AppendJoin<T>(this StringBuilder builder, char separator, IEnumerable<T> values)
        {
            builder.Append(string.Join(separator, values));
        }

        internal static string[] Split(this string input, string separator, StringSplitOptions options = StringSplitOptions.None) => input.Split(separator, options);

        internal static bool StartsWith(this string input, char value) => input.StartsWith(value);

        internal static bool EndsWith(this string input, char value) => input.EndsWith(value);
    }
}
