using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Polly.CircuitBreaker;

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
        this._route = route;
        this._contextAccessor = contextAccessor;
        this._logger = loggerFactory.CreateLogger<PollyResiliencePipelineDelegatingHandler>();
    }

    private IPollyQoSResiliencePipelineProvider<HttpResponseMessage> GetQoSProvider()
    {
        Debug.Assert(this._contextAccessor.HttpContext != null, "_contextAccessor.HttpContext != null");
        return this._contextAccessor.HttpContext.RequestServices.GetService<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>>();
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
        var pipeline = qoSProvider.GetResiliencePipeline(this._route);

        if (pipeline == null)
        {
            return await base.SendAsync(request, cancellationToken); // shortcut > no qos
        }

        return await pipeline.ExecuteAsync(async (token) => await base.SendAsync(request, token), cancellationToken);
    }
}
