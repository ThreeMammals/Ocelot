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
    public async Task ShouldUseOverriddenCustomDelimiterToFindService(
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
        GivenOcelotIsRunning(services => WithDynamicRouting<HashDelimiterRouting>(services));
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData('#', "/authorization/api%23", "authorization/api", "/")]
    [InlineData(':', "/authorization/api%3Apath", "authorization/api", "/path")]
    [InlineData('@', "/authorization/api%40/path?query", "authorization/api", "/path?query")]
    public async Task ShouldUseEncodedConfiguredCustomDelimiterToFindService(
        char delimiter, string upstreamPath, string serviceName, string downstreamPath)
    {
        var port = PortFinder.GetRandomPort();
        var serviceUrls = new[] { DownstreamUrl(port) };
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });

        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDelimiterOptions(services, delimiter));
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData(':', "/authorization/api:", "authorization/api", "/")]
    [InlineData(':', "/authorization/api:path", "authorization/api", "/path")]
    [InlineData('@', "/authorization/api@/path?query", "authorization/api", "/path?query")]
    public async Task ShouldUseUnencodedConfiguredCustomDelimiterToFindService(
        char delimiter, string upstreamPath, string serviceName, string downstreamPath)
    {
        var port = PortFinder.GetRandomPort();
        var serviceUrls = new[] { DownstreamUrl(port) };
        var configuration = GivenDynamicRouting(new()
        {
            { serviceName, serviceUrls },
        });

        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDelimiterOptions(services, delimiter));
        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData(true, "%23extra", null, "/service/api%23/details", "service/api", "/details")]
    [InlineData(true, "@extra", null, "/service/api@details", "service/api", "/details")]
    [InlineData(true, "@", "2", "/service/api/details", "service/api", "/details")]
    [InlineData(true, "@", "1", "/service/api:path", "service", "/api:path")]
    [InlineData(true, "@", "2", "/service/api/", "service/api", "/")]
    [InlineData(true, "@", "9", "/service/api/details", "service", "/api/details")]
    [InlineData(true, "@", "0", "/service/api@details", "service/api", "/details")]
    [InlineData(true, "@", "-1", "/service/api@details", "service/api", "/details")]
    [InlineData(true, "@", "invalid", "/service/api@details", "service/api", "/details")]
    [InlineData(true, "%2F", null, "/service/api:path", "service/api", "/path")]
    [InlineData(true, "%2F", null, "/service/api:path?query", "service/api", "/path?query")]
    [InlineData(false, "@", "1", "/service/api:path", "service/api", "/path")]
    public async Task ShouldApplyManagementHeadersAndFallbacks(
        bool isHeaderManagement, string delimiter, string segments, string upstreamPath, string serviceName, string downstreamPath)
    {
        var serviceUrls = new[] { DownstreamUrl(PortFinder.GetRandomPort()) };
        var configuration = GivenDynamicRouting(new() { { serviceName, serviceUrls } });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouting>(services, options =>
        {
            options.Delimiter = ':';
            options.IsHeaderManagement = isHeaderManagement;
        }));
        GivenIAddAHeader(MultipleSegmentUrlPathDynamicRouting.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter, delimiter);
        if (segments != null)
            GivenIAddAHeader(MultipleSegmentUrlPathDynamicRouting.Headers.Ocelot_DynamicRouting_UrlPath_Segments, segments);

        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Fact]
    public async Task ShouldChangeSegmentCountPerRequest()
    {
        var serviceUrls = new[] { DownstreamUrl(PortFinder.GetRandomPort()) };
        var configuration = GivenDynamicRouting(new()
        {
            { "service", serviceUrls },
            { "service/api", serviceUrls },
        });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, "service");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouting>(
            services, options => options.IsHeaderManagement = true));

        const string header = MultipleSegmentUrlPathDynamicRouting.Headers.Ocelot_DynamicRouting_UrlPath_Segments;
        GivenIAddAHeader(header, "2");
        await WhenIGetUrlOnTheApiGateway("/service/api/details");
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, "/details");

        ocelotClient.DefaultRequestHeaders.Remove(header);
        GivenIAddAHeader(header, "1");
        await WhenIGetUrlOnTheApiGateway("/service/api/details");
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, "/api/details");
        ThenAllServicesShouldHaveBeenCalledTimes(2);
    }

    private void WithDynamicRouting(IServiceCollection services)
        => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouting>(services);

    private void WithDelimiterOptions(IServiceCollection services, char delimiter)
        => _ = WithDynamicRouting<MultipleSegmentUrlPathDynamicRouting>(
                services,
                options => options.Delimiter = delimiter);

    private sealed class HashDelimiterRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouting(contextAccessor)
    {
        protected override char ServiceNameDelimiter => '#';
    }
}
