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

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var qoSProvider = GetQoSProvider();
        try
        {
            // at least one policy (timeout) will be returned
            // AsyncPollyPolicy can't be null
            // AsyncPollyPolicy constructor will throw if no policy is provided
            var policy = qoSProvider.GetPollyPolicyWrapper(_route).AsyncPollyPolicy;
            return await policy.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError("Reached to allowed number of exceptions. Circuit is open", ex);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error in {nameof(PollyPoliciesDelegatingHandler)}.{nameof(SendAsync)}", ex);
            throw;
        }
    }
}
