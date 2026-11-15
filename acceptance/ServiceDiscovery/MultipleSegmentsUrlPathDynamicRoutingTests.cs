using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Ocelot.ServiceDiscovery;

namespace Ocelot.Acceptance.ServiceDiscovery;

[Trait("Feat", "1188")] // https://github.com/ThreeMammals/Ocelot/issues/1188
[Trait("PR", "1198")] // https://github.com/ThreeMammals/Ocelot/pull/1198
public sealed class MultipleSegmentsUrlPathDynamicRoutingTests : DynamicSteps
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
    [InlineData("/authorization/api::", "authorization/api", "/")]
    [InlineData("/authorization/api::path", "authorization/api", "/path")]
    public async Task ShouldUseMultipleCharacterDelimiterToFindService(
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
        GivenOcelotIsRunning(WithMultiCharacterDelimiterRouting);
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData("/authorization/api||", "authorization/api", "/")]
    [InlineData("/authorization/api||path", "authorization/api", "/path")]
    [InlineData("/authorization/api%7C%7C", "authorization/api", "/")]
    [InlineData("/authorization/api%7C%7Cpath", "authorization/api", "/path")]
    public async Task ShouldDecodeEncodedServiceNameEnding(
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
        GivenOcelotIsRunning(WithEncodedDelimiterRouting);
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    private void WithDynamicRouting(IServiceCollection services)
        => WithDynamicRouting<MultipleSegmentsUrlPathDynamicRouting>(services);

    private void WithMultiCharacterDelimiterRouting(IServiceCollection services)
        => WithDynamicRouting<MultiCharacterDelimiterRouting>(services);

    private void WithEncodedDelimiterRouting(IServiceCollection services)
        => WithDynamicRouting<EncodedDelimiterRouting>(services);

    private sealed class MultiCharacterDelimiterRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentsUrlPathDynamicRouting(contextAccessor)
    {
        protected override string ServiceNameEnding => "::";
    }

    private sealed class EncodedDelimiterRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentsUrlPathDynamicRouting(contextAccessor)
    {
        protected override string ServiceNameEnding => "||";
    }
}
