using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;

namespace Ocelot.Acceptance.QualityOfService;

public class FakeQualityOfServiceDelegatingHandler : DelegatingHandler
{
    private readonly DownstreamRoute _route;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IOcelotLogger _logger;

    public FakeQualityOfServiceDelegatingHandler(
        DownstreamRoute route,
        IHttpContextAccessor contextAccessor,
        IOcelotLoggerFactory loggerFactory)
    {
        _route = route;
        _contextAccessor = contextAccessor;
        _logger = loggerFactory.CreateLogger<FakeQualityOfServiceDelegatingHandler>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_route.QosOptions.UseQos)
        {
            _logger.LogDebug(() => $"No pipeline was detected by QoS provider for the route with downstream URL '{request.RequestUri}'.");
            return await base.SendAsync(request, cancellationToken); // shortcut -> no qos
        }

        _logger.LogInformation(() => $"QoS has enabled for the route with downstream URL '{request.RequestUri}'. Going to execute request...");
        return await base.SendAsync(request, cancellationToken);
    }
}
