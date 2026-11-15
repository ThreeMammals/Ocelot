using Microsoft.Extensions.DependencyInjection;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Acceptance.ServiceDiscovery;

[Trait("Feat", "1188")] // https://github.com/ThreeMammals/Ocelot/issues/1188
public sealed class MultipleSegmentsUrlPathDynamicRoutingTests : DynamicSteps
{
    [Theory]
    [InlineData("/authorization$", "authorization", "/")]
    [InlineData("/authorization/api$", "authorization/api", "/")]
    [InlineData("/authorization/api/user$", "authorization/api/user", "/")]
    [InlineData("/authorization/jobs$", "authorization/jobs", "/")]
    [InlineData("/authorization/api/user$/details", "authorization/api/user", "/details")]
    [InlineData("/authorization/api/user$details", "authorization/api/user", "/details")]
    [InlineData("/authorization/api/user/$/details", "authorization/api/user", "/details")]
    public async Task ShouldUseMultipleUpstreamPathSegmentsToFindService(
        string upstreamPath, string serviceName, string downstreamPath)
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
        services.AddOcelotDynamicRouting<MultipleSegmentsUrlPathDynamicRouting>();
    }
}
