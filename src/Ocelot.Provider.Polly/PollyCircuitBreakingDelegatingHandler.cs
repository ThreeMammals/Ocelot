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

    private IPollyQoSProvider GetQoSProvider()
    {
        Debug.Assert(_contextAccessor.HttpContext != null, "_contextAccessor.HttpContext != null");
        return _contextAccessor.HttpContext.RequestServices.GetService<IPollyQoSProvider>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var qoSProvider = GetQoSProvider();
        try
        {
            var policies = qoSProvider.GetCircuitBreaker(_route).Policies;
            if (!policies.Any())
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var policy = policies.Length > 1
                ? Policy.WrapAsync(policies)
                : policies[0];

            var policyResult = await policy.ExecuteAsync(async () =>
            {
                var result = await base.SendAsync(request, cancellationToken);
                if (result.StatusCode != HttpStatusCode.InternalServerError)
                {
                    return result;
                }

                var content = await result.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(content);

            });

            return policyResult;
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
