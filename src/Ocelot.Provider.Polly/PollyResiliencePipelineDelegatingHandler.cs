using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;
using System.Diagnostics;

namespace Ocelot.Provider.Polly;

public class PollyResiliencePipelineDelegatingHandler : DelegatingHandler
{
    private readonly DownstreamRoute _route;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IOcelotLogger _logger;

    public PollyResiliencePipelineDelegatingHandler(
        DownstreamRoute route,
        IHttpContextAccessor contextAccessor,
        IOcelotLoggerFactory loggerFactory)
    {
        _route = route;
        _contextAccessor = contextAccessor;
        _logger = loggerFactory.CreateLogger<PollyResiliencePipelineDelegatingHandler>();
    }

    private IPollyQoSResiliencePipelineProvider<HttpResponseMessage> GetQoSProvider()
    {
        Debug.Assert(_contextAccessor.HttpContext != null, "_contextAccessor.HttpContext != null");

        // TODO: Move IPollyQoSResiliencePipelineProvider<HttpResponseMessage> object injection to DI container by a DI helper
        return _contextAccessor.HttpContext.RequestServices.GetService<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>>();
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
        var qoSProvider = this.GetQoSProvider();
        var pipeline = qoSProvider.GetResiliencePipeline(_route);

        if (pipeline == null)
        {
            _logger.LogDebug(() => $"No {nameof(pipeline)} was detected by QoS provider for the route with downstream URL '{request.RequestUri}'.");
            return await base.SendAsync(request, cancellationToken); // shortcut > no qos
        }

        _logger.LogInformation(() => $"The {pipeline.GetType().Name} {nameof(pipeline)} has detected by QoS provider for the route with downstream URL '{request.RequestUri}'. Going to execute request...");
        return await pipeline.ExecuteAsync(async (token) => await base.SendAsync(request, token), cancellationToken);
    }
}
