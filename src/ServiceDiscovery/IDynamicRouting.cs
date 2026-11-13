using Microsoft.AspNetCore.Http;

namespace Ocelot.ServiceDiscovery;

public interface IDynamicRouting
{
    HttpRequest Request { get; }

    /// <summary>
    /// Gets the downstream path part (of the URL) without the service name, based on information from the current <c>HttpContext</c> and/or <c>HttpRequest</c>.
    /// </summary>
    /// <returns>A <see cref="string"/> object containing the downstream path.</returns>
    string GetDownstreamPath();

    /// <summary>
    /// Gets the service name and its namespace based on information from the current <c>HttpContext</c> and/or <c>HttpRequest</c>.
    /// </summary>
    /// <returns>A <see cref="ServiceInfo"/> value.</returns>
    ServiceInfo GetServiceName();

    /// <summary>Gets service name and its namespace of request URL from the first segment of the URL path.
    /// <para>Note: A namespace and service name should be separated by a '.' (dot) character.</para></summary>
    /// <remarks>Example: <c>http://ocelot.net/namespace.service-name/path</c> URL.</remarks>
    /// <param name="upstreamUrlPath">The upstream path.</param>
    /// <returns>A <see cref="ServiceInfo"/> value.</returns>
    ServiceInfo GetServiceName(string upstreamUrlPath);

    /// <summary>
    /// Sets service name information into a state object, updating it or creating a new state as an adapter for the current service discovery provider.
    /// </summary>
    /// <remarks>This is the opposite of the <see cref="GetServiceName()"/> method, which imports the info from a state of an unknown type.</remarks>
    /// <param name="state">The current or initial state.</param>
    /// <param name="service">The service name information.</param>
    /// <returns>An object with the updated state.</returns>
    object SetServiceName(object state, ServiceInfo service);

    // Task<T> SetAsync<T>(T state, ServiceInfo service, CancellationToken cancellation);
}
