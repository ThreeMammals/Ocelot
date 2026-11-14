using Microsoft.Extensions.DependencyInjection;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Acceptance.ServiceDiscovery;

[Trait("Feat", "1188")] // https://github.com/ThreeMammals/Ocelot/issues/1188
public sealed class UrlPathDynamicRoutingTests : DynamicSteps
{
    [Theory]
    [InlineData("/authorization", "authorization.service.web")]
    [InlineData("/authorization/api", "authorization.service.api")]
    public async Task ShouldUseTwoUpstreamPathSegmentsToFindService(string upstreamPath, string serviceName)
    {
        var port = PortFinder.GetRandomPort();
        var serviceUrls = new[] { DownstreamUrl(port) };
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });

        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, "/"); // downstream path
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData("/namespace.authorization", "authorization.service.web", "/")]
    [InlineData("/namespace.authorization/web/path1", "authorization.service.web", "/path1")]
    [InlineData("/namespace.authorization/api", "authorization.service.api", "/")]
    [InlineData("/namespace.authorization/api/path2?query", "authorization.service.api", "/path2?query")]
    public async Task ShouldUseTwoUpstreamPathSegmentsToFindNamespacedService(string upstreamPath, string serviceName, string downstreamPath)
    {
        var port = PortFinder.GetRandomPort();
        var serviceUrls = new[] { DownstreamUrl(port) };
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });

        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    public override void WithDiscovery(IServiceCollection services)
    {
        base.WithDiscovery(services);
        services.AddOcelotDynamicRouting<TwoSegmentsUrlPathDynamicRouting>();
    }
}
