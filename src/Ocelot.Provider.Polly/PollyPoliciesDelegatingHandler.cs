using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;

using Polly.CircuitBreaker;

using System.Diagnostics;

namespace Ocelot.Provider.Polly;

public class PollyPoliciesDelegatingHandler : DelegatingHandler
{
    private readonly DownstreamRoute _route;
    private readonly IHttpContextAccessor _contextAccessor;

    public PollyPoliciesDelegatingHandler(
        DownstreamRoute route,
        IHttpContextAccessor contextAccessor)
    {
        _route = route;
        _contextAccessor = contextAccessor;
    }

    private IPollyQoSProvider<HttpResponseMessage> GetQoSProvider()
    {
        Debug.Assert(_contextAccessor.HttpContext != null, "_contextAccessor.HttpContext != null");
        return _contextAccessor.HttpContext.RequestServices.GetService<IPollyQoSProvider<HttpResponseMessage>>();
    }

    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
    /// </summary>
    /// <param name="request">Downstream request</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="BrokenCircuitException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var qoSProvider = GetQoSProvider();
        // at least one policy (timeout) will be returned
        // AsyncPollyPolicy can't be null
        // AsyncPollyPolicy constructor will throw if no policy is provided
        var policy = qoSProvider.GetPollyPolicyWrapper(_route).AsyncPollyPolicy;
        return await policy.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
    }
}
