using Ocelot.Logging;

namespace Ocelot.ManualTest.DelegatingHandlers;

public class FakeHandler : DelegatingHandler
{
    private readonly IOcelotLogger _logger;

    public FakeHandler(IOcelotLoggerFactory factory)
    {
        _logger = factory.CreateLogger<FakeHandler>();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(() => $"{nameof(request.RequestUri)}: {request.RequestUri}");

        // Do stuff and optionally call the base handler..
        return base.SendAsync(request, cancellationToken);
    }
}
