using Ocelot.DependencyInjection;

namespace Ocelot.Infrastructure;

public static class RegexGlobal
{
    static RegexGlobal()
    {
        RegexCacheSize = DefaultRegexCacheSize;
        DefaultMatchTimeout = TimeSpan.FromMilliseconds(DefaultMatchTimeoutMilliseconds);
        AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", DefaultMatchTimeout);
    }

    /// <summary>Default value of the <see cref="RegexCacheSize"/> property.</summary>
    public const int DefaultRegexCacheSize = 100;

    /// <summary>Gets or sets the global value to assign to the <see cref="Regex.CacheSize"/> property.
    /// <para>Ocelot forcibly assigns this value during app startup, see <see cref="ConfigurationBuilderExtensions"/> class.</para>
    /// </summary>
    /// <remarks>Default value is <c>100</c> aka <see cref="DefaultRegexCacheSize"/>.<br/>
    /// Default .NET value of <see cref="Regex.CacheSize"/> is <c>15</c>.</remarks>
    /// <value>An <see cref="int"/> value.</value>
    public static int RegexCacheSize { get; set; }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS1574 // File name must match first type name
    /// <summary>Default value for the <see cref="GeneratedRegexAttribute"/> and the <see cref="Regex"/> constructors.</summary>
#pragma warning restore CS1574 // File name must match first type name
#pragma warning restore IDE0079 // Remove unnecessary suppression
    public const int DefaultMatchTimeoutMilliseconds = 100;

    /// <summary>Default match timeout for the <see cref="Regex"/> constructors.</summary>
    /// <remarks>Default value is <c>100</c> ms aka <see cref="DefaultMatchTimeoutMilliseconds"/>.</remarks>
    /// <value>A <see cref="TimeSpan"/> value.</value>
    public static TimeSpan DefaultMatchTimeout { get; set; }

    public static Regex New(string pattern)
        => new(pattern, RegexOptions.Compiled, DefaultMatchTimeout);
    public static Regex New(string pattern, RegexOptions options)
        => new(pattern, options | RegexOptions.Compiled, DefaultMatchTimeout);
    public static Regex New(string pattern, RegexOptions options, TimeSpan matchTimeout)
        => new(pattern, options | RegexOptions.Compiled, matchTimeout);
}
