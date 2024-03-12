using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Polly.CircuitBreaker;

namespace Ocelot.Provider.Polly.v7;

/// <summary>Delegates <see cref="HttpRequest"/> sending to downstream.</summary>
/// <remarks>Outdated V7 design! Use the <see cref="PollyResiliencePipelineDelegatingHandler"/> class.</remarks>
[Obsolete("Due to new v8 policy definition in Polly 8 (use PollyResiliencePipelineDelegatingHandler)")]
public class PollyPoliciesDelegatingHandler : DelegatingHandler
{
    private readonly DownstreamRoute _route;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IOcelotLogger _logger;

    public PollyPoliciesDelegatingHandler(
        DownstreamRoute route,
        IHttpContextAccessor contextAccessor,
        IOcelotLoggerFactory loggerFactory)
    {
        _route = route;
        _contextAccessor = contextAccessor;
        _logger = loggerFactory.CreateLogger<PollyPoliciesDelegatingHandler>();
    }

    private IPollyQoSProvider<HttpResponseMessage> GetQoSProvider()
    {
        Debug.Assert(_contextAccessor.HttpContext != null, "_contextAccessor.HttpContext != null");
        return _contextAccessor.HttpContext.RequestServices.GetService<IPollyQoSProvider<HttpResponseMessage>>();
    }

    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
    /// </summary>
    /// <param name="request">Downstream request.</param>
    /// <param name="cancellationToken">Token to cancel the task.</param>
    /// <returns>A <see cref="Task{HttpResponseMessage}"/> object of a <see cref="HttpResponseMessage"/> result.</returns>
    /// <exception cref="BrokenCircuitException">Exception thrown when a circuit is broken.</exception>
    /// <exception cref="HttpRequestException">Exception thrown by <see cref="HttpClient"/> and <see cref="HttpMessageHandler"/> classes.</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var qoSProvider = GetQoSProvider();

        // At least one policy (timeout) will be returned
        // AsyncPollyPolicy can't be null
        // AsyncPollyPolicy constructor will throw if no policy is provided
        var policy = qoSProvider.GetPollyPolicyWrapper(_route).AsyncPollyPolicy;

        return await policy.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
    }
}
