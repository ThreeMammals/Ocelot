using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Acceptance.ServiceDiscovery;

[Trait("Feat", "1188")] // https://github.com/ThreeMammals/Ocelot/issues/1188
[Trait("PR", "1198")] // https://github.com/ThreeMammals/Ocelot/pull/1198
public sealed class MultipleSegmentUrlPathDynamicRoutingTests : DynamicSteps
{
    [Theory]
    [InlineData("/authorization$", "authorization", "/")]
    [InlineData("/authorization/api$", "authorization/api", "/")]
    [InlineData("/authorization/api/user$", "authorization/api/user", "/")]
    [InlineData("/authorization/jobs$", "authorization/jobs", "/")]
    [InlineData("/authorization/api/user$/path", "authorization/api/user", "/path")]
    [InlineData("/authorization/api/user$path", "authorization/api/user", "/path")]
    [InlineData("/authorization/api/user/$/path", "authorization/api/user", "/path")]
    [InlineData("/namespace.authorization/api$", "authorization/api", "/")]
    [InlineData("/namespace.authorization/api$/path", "authorization/api", "/path")]
    [InlineData("/namespace.authorization/api$/path?query", "authorization/api", "/path?query")]
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
        GivenOcelotIsRunning(WithDynamicRouting);
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData("/authorization/api%23", "authorization/api", "/")]
    [InlineData("/authorization/api%23path", "authorization/api", "/path")]
    [InlineData("/authorization/api%23/path?query", "authorization/api", "/path?query")]
    public async Task ShouldUseEncodedCustomDelimiterToFindService(
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
        GivenOcelotIsRunning(WithHashDelimiterRouting);
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    private void WithDynamicRouting(IServiceCollection services)
        => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouting>(services);

    private void WithHashDelimiterRouting(IServiceCollection services)
        => WithDynamicRouting<HashDelimiterRouting>(services);

    private sealed class HashDelimiterRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouting(contextAccessor)
    {
        protected override char ServiceNameDelimiter => '#';
    }
}
