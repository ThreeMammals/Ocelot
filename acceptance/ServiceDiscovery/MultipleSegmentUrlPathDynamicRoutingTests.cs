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
    public async Task DelimiterMode_ShouldUseMultipleUpstreamPathSegmentsToFindService(
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
    public async Task DelimiterMode_ShouldUseOverriddenCustomDelimiterToFindService(
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
    public async Task DelimiterMode_ShouldUseEncodedConfiguredCustomDelimiterToFindService(
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
    public async Task DelimiterMode_ShouldUseUnencodedConfiguredCustomDelimiterToFindService(
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
    [InlineData(true, "%23extra", null, "/service/api%23/path", "service/api", "/path")]
    [InlineData(true, "@extra", null, "/service/api@path", "service/api", "/path")]
    [InlineData(true, "@", "2", "/service/api/path", "service/api", "/path")]
    [InlineData(true, "@", "1", "/service/api:path", "service", "/api:path")]
    [InlineData(true, "@", "2", "/service/api/", "service/api", "/")]
    [InlineData(true, "@", "9", "/service/api/path", "service", "/api/path")]
    [InlineData(true, "@", "0", "/service/api@path", "service/api", "/path")]
    [InlineData(true, "@", "-1", "/service/api@path", "service/api", "/path")]
    [InlineData(true, "@", "invalid", "/service/api@path", "service/api", "/path")]
    [InlineData(true, "%2F", null, "/service/api:path", "service/api", "/path")]
    [InlineData(true, "%2F", null, "/service/api:path?query", "service/api", "/path?query")]
    [InlineData(false, "@", "1", "/service/api:path", "service/api", "/path")]
    public async Task SegmentsMode_ShouldApplyManagementHeadersAndFallbackToDelimiterMode(
        bool isHeaderManagement, string delimiter, string segments, string upstreamPath, string serviceName, string downstreamPath)
    {
        void WithOptions(MultipleSegmentUrlPathDynamicRouterOptions options)
        {
            options.Delimiter = ':';
            options.IsHeaderManagement = isHeaderManagement;
        }
        var serviceUrls = new[] { DownstreamUrl(PortFinder.GetRandomPort()) };
        var configuration = GivenDynamicRouting(new() { { serviceName, serviceUrls } });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(services, WithOptions));
        GivenIAddAHeader(MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter, delimiter);
        if (segments != null)
            GivenIAddAHeader(MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments, segments);

        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Fact]
    public async Task SegmentsMode_ShouldChangeSegmentCountPerRequest()
    {
        var serviceUrls = new[] { DownstreamUrl(PortFinder.GetRandomPort()) };
        var configuration = GivenDynamicRouting(new()
        {
            { "service", serviceUrls },
            { "service/api", serviceUrls },
        });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, "service");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(
            services, options => options.IsHeaderManagement = true));

        const string header = MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments;
        GivenIAddAHeader(header, "2");
        await WhenIGetUrlOnTheApiGateway("/service/api/path?try=1");
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, "/path?try=1");

        ocelotClient.DefaultRequestHeaders.Remove(header);
        GivenIAddAHeader(header, "1");
        await WhenIGetUrlOnTheApiGateway("/service/api/path?try=2");
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, "/api/path?try=2");
        ThenAllServicesShouldHaveBeenCalledTimes(2);
    }

    [Theory]
    [InlineData(1, "/service/api/path", "service", "/api/path")]
    [InlineData(2, "/service/api/path", "service/api", "/path")]
    [InlineData(3, "/service/api/path/downstream", "service/api/path", "/downstream")]
    public async Task SegmentsMode_ShouldUseConfiguredSegmentCount(
        int segments, string upstreamPath, string serviceName, string downstreamPath)
    {
        var serviceUrls = new[] { DownstreamUrl(PortFinder.GetRandomPort()) };
        var configuration = GivenDynamicRouting(new() { { serviceName, serviceUrls } });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithSegmentsOptions(services, segments));

        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Fact]
    public async Task SegmentsMode_ShouldUseOverriddenSegmentCount()
    {
        var serviceUrls = new[] { DownstreamUrl(PortFinder.GetRandomPort()) };
        var configuration = GivenDynamicRouting(new() { { "service/api", serviceUrls } });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, "service/api");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDynamicRouting<TwoSegmentsRouting>(services));

        await WhenIGetUrlOnTheApiGateway("/service/api/path");

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, "/path");
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    [Theory]
    [InlineData("0", "@", "/service/api@downstream", "service/api", "/downstream")]
    [InlineData("2", "@", "/service/api/downstream", "service/api", "/downstream")]
    [InlineData("-1", "@", "/service/api/path/downstream", "service/api/path", "/downstream")]
    [InlineData("invalid", "@", "/service/api/path/downstream", "service/api/path", "/downstream")]
    public async Task SegmentsMode_ShouldApplyHeaderOverridesAndFallbacks(
        string segments, string delimiter, string upstreamPath, string serviceName, string downstreamPath)
    {
        var serviceUrls = new[] { DownstreamUrl(PortFinder.GetRandomPort()) };
        var configuration = GivenDynamicRouting(new() { { serviceName, serviceUrls } });
        GivenMultipleServiceInstancesAreRunning(serviceUrls, serviceName);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(services => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(
            services, options =>
            {
                options.Delimiter = ':';
                options.IsHeaderManagement = true;
                options.Segments = 3;
            }));
        GivenIAddAHeader(MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Segments, segments);
        GivenIAddAHeader(MultipleSegmentUrlPathDynamicRouter.Headers.Ocelot_DynamicRouting_UrlPath_Delimiter, delimiter);

        await WhenIGetUrlOnTheApiGateway(upstreamPath);

        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseHeaderIs(HeaderNames.Path, downstreamPath);
        ThenAllServicesShouldHaveBeenCalledTimes(1);
    }

    private void WithDynamicRouting(IServiceCollection services)
        => WithDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(services);

    private void WithDelimiterOptions(IServiceCollection services, char delimiter)
        => _ = WithDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(services,
                options => options.Delimiter = delimiter);

    private void WithSegmentsOptions(IServiceCollection services, int segments)
        => _ = WithDynamicRouting<MultipleSegmentUrlPathDynamicRouter>(services,
                options => options.Segments = segments);

    private sealed class HashDelimiterRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouter(contextAccessor)
    {
        protected override char ServiceNameDelimiter => '#';
    }

    private sealed class TwoSegmentsRouting(IHttpContextAccessor contextAccessor)
        : MultipleSegmentUrlPathDynamicRouter(contextAccessor)
    {
        protected override int? Segments => 2;
    }
}
