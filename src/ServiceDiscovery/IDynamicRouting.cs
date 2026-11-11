namespace Ocelot.ServiceDiscovery;

public interface IDynamicRouting
{
    /// <summary>
    /// Gets the downstream path part (of the URL) without the service name, based on information from the current <c>HttpContext</c> and/or <c>HttpRequest</c>.
    /// </summary>
    /// <returns>A <see cref="string"/> object containing the downstream path.</returns>
    string GetDownstreamPath();

    /// <summary>
    /// Gets the downstream path part of the URL without the service name.
    /// Thus, the first segment of the upstream URL is removed.
    /// </summary>
    /// <param name="upstreamUrlPath">The URL with the service name in the first segment.</param>
    /// <returns>A <see cref="string"/> object containing the downstream path.</returns>
    string GetDownstreamPath(string upstreamUrlPath);


    /// <summary>
    /// Gets the service name and its namespace (from the request URL) based on information from the current <c>HttpContext</c> and/or <c>HttpRequest</c>.
    /// </summary>
    /// <returns>A <see cref="ServiceInfo"/> value.</returns>
    ServiceInfo GetServiceName();

    /// <summary>Gets service name and its namespace of request URL from the first segment of the URL path.
    /// <para>Note: A namespace and service name should be separated by a '.' (dot) character.</para></summary>
    /// <remarks>Example: <c>http://ocelot.net/namespace.service-name/path</c> URL.</remarks>
    /// <param name="upstreamUrlPath">The upstream path.</param>
    /// <returns>A <see cref="ServiceInfo"/> value.</returns>
    ServiceInfo GetServiceName(string upstreamUrlPath);
}
