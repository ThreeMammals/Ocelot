using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.ServiceDiscovery;
using Ocelot.Testing.Steps;
using System.Net;

namespace Ocelot.Acceptance.ServiceDiscovery;

[Trait("Feat", "1188")] // https://github.com/ThreeMammals/Ocelot/issues/1188
public sealed class UrlPathDynamicRoutingTests : DiscoverySteps
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

        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/");
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData("/authorization", "authorization.service.web", "namespace")]
    [InlineData("/authorization/api", "authorization.service.api", "namespace")]
    public async Task ShouldUseTwoUpstreamPathSegmentsToFindNamespacedService(
        string upstreamPath, string serviceName, string serviceNamespace)
    {
        var port = PortFinder.GetRandomPort();
        var serviceUrls = new[] { DownstreamUrl(port) };
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });
        var namespacedPath = $"/{serviceNamespace}.{upstreamPath.TrimStart('/')}";

        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithDiscovery);
        await WhenIGetUrlOnTheApiGateway(namespacedPath);

        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/");
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    public override void WithDiscovery(IServiceCollection services)
    {
        base.WithDiscovery(services);
        services.AddOcelotDynamicRouting<TwoSegmentsUrlPathDynamicRouting>();
    }

    public override CancellationToken CancelMe => TestContext.Current.CancellationToken;
}
