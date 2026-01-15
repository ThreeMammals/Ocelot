using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Request.Middleware;

namespace Ocelot.Configuration;

public class CacheOptions
{
    public const int NoSeconds = 0;

    /// <summary>
    /// Separation of concerns between Ocelot's native caching control and the industry-standard <c>Cache-Control</c> header, which governs downstream caching behavior.
    /// </summary>
    public const string Oc_Cache_Control = "OC-Cache-Control";

    internal CacheOptions() { }
    public CacheOptions(FileCacheOptions from, string defaultRegion)
        : this(from.TtlSeconds, from.Region.IfEmpty(defaultRegion), from.Header, from.EnableContentHashing)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheOptions"/> class.
    /// </summary>
    /// <remarks>
    /// Internal defaults:
    ///   <list type="bullet">
    ///   <item>The default value for <see cref="EnableContentHashing"/> is <see langword="false"/>, but it is set to null for route-level configuration to allow global configuration usage.</item>
    ///   <item>The default value for <see cref="TtlSeconds"/> is 0.</item>
    ///   </list>
    /// </remarks>
    /// <param name="ttlSeconds">Time-to-live seconds. If not speciefied, zero value is used by default.</param>
    /// <param name="region">The region of caching.</param>
    /// <param name="header">The header name to control cached value.</param>
    /// <param name="enableContentHashing">The switcher for content hashing. If not speciefied, false value is used by default.</param>
    public CacheOptions(int? ttlSeconds, string region, string header, bool? enableContentHashing)
    {
        TtlSeconds = ttlSeconds ?? NoSeconds;
        Region = region;
        Header = header.IfEmpty(Oc_Cache_Control);
        EnableContentHashing = enableContentHashing ?? false;
    }

    /// <summary>Time-to-live seconds.</summary>
    /// <remarks>Default value is 0. No caching by default.</remarks>
    /// <value>An <see cref="int"/> value of seconds.</value>
    public int TtlSeconds { get; }
    public string Region { get; }
    public string Header { get; }

    /// <summary>Enables MD5 hash calculation of the <see cref="HttpRequestMessage.Content"/> of the <see cref="DownstreamRequest.Request"/> object.</summary>
    /// <remarks>Default value is <see langword="false"/>. No hashing by default.</remarks>
    /// <value><see langword="true"/> if hashing is enabled, otherwise it is <see langword="false"/>.</value>
    public bool EnableContentHashing { get; }

    public bool UseCache => TtlSeconds > NoSeconds;
}
