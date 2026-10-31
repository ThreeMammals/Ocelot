using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Request.Middleware;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.ServiceFabric;

public sealed class ServiceFabricDownstreamPathFormatter : IDownstreamPathFormatter
{
    private readonly IDownstreamPathPlaceholderReplacer _replacer;

    public ServiceFabricDownstreamPathFormatter(IDownstreamPathPlaceholderReplacer replacer)
    {
        _replacer = replacer;
    }

    public bool TryFormat(
        DownstreamRequest downstreamRequest,
        DownstreamRoute downstreamRoute,
        IInternalConfiguration configuration,
        List<PlaceholderNameAndValue> placeholders,
        DownstreamPath downstreamPath)
    {
        if (!ServiceFabricServiceDiscoveryProvider.Type.Equals(configuration.ServiceProviderConfiguration.Type, StringComparison.OrdinalIgnoreCase)
            || !downstreamRoute.UseServiceDiscovery)
        {
            return false;
        }

        var serviceName = _replacer.Replace(downstreamRoute.ServiceName, placeholders);
        downstreamRequest.AbsolutePath = $"/{serviceName.Value}{downstreamPath.Value}";
        return true;
    }
}
