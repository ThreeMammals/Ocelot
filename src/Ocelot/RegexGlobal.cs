﻿using Ocelot.DependencyInjection;

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

    /// <summary>Default value for the <see cref="GeneratedRegexAttribute"/> attribute and the <see cref="Regex"/> constructors.</summary>
    public const int DefaultMatchTimeoutMilliseconds = 100;

    /// <summary>Default match timeout for the <see cref="Regex"/> constructors.</summary>
    /// <remarks>Default value is <c>100</c> ms aka <see cref="DefaultMatchTimeoutMilliseconds"/>.</remarks>
    /// <value>A <see cref="TimeSpan"/> value.</value>
    public static TimeSpan DefaultMatchTimeout { get; set; } = TimeSpan.FromMilliseconds(DefaultMatchTimeoutMilliseconds);
}
