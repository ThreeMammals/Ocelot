using Ocelot.DependencyInjection;

namespace Ocelot;

public static class RegexGlobal
{
    /// <summary>Default value of the <see cref="RegexCacheSize"/> property.</summary>
    public const int DefaultRegexCacheSize = 100;

    /// <summary>Gets or sets the global value to assign to the <see cref="Regex.CacheSize"/> property.
    /// <para>Ocelot forcibly assigns this value during app startup, see <see cref="ConfigurationBuilderExtensions"/> class.</para>
    /// </summary>
    /// <remarks>Default value is <c>100</c> aka <see cref="DefaultRegexCacheSize"/>.<br/>
    /// Default .NET value of <see cref="Regex.CacheSize"/> is <c>15</c>.</remarks>
    /// <value>An <see cref="int"/> value.</value>
    public static int RegexCacheSize { get; set; } = DefaultRegexCacheSize;
}
