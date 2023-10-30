using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;

namespace Ocelot.Provider.Polly;

public class PollyCircuitBreakingDelegatingHandler : DelegatingHandler
{
    private readonly DownstreamRoute _route;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IOcelotLogger _logger;

    public PollyCircuitBreakingDelegatingHandler(
        DownstreamRoute route,
        IHttpContextAccessor contextAccessor,
        IOcelotLoggerFactory loggerFactory)
    {
        _route = route;
        _contextAccessor = contextAccessor;
        _logger = loggerFactory.CreateLogger<PollyCircuitBreakingDelegatingHandler>();
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
            var policy = qoSProvider.GetCircuitBreaker(_route).CircuitBreakerAsyncPolicy;
            if (policy == null)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            return await policy.ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError("Reached to allowed number of exceptions. Circuit is open", ex);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error in {nameof(PollyCircuitBreakingDelegatingHandler)}.{nameof(SendAsync)}", ex);
            throw;
        }
    }
}
