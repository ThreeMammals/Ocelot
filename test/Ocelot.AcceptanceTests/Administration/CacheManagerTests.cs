//using Ocelot.Administration;
using CacheManager.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Cache.CacheManager;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.Administration;

public sealed class CacheManagerTests : AuthenticationSteps
{
    public CacheManagerTests() : base()
    { }

    [Fact(
        DisplayName = "TODO " + nameof(ShouldClearCacheRegionViaAdministrationAPI),
        Skip = "TODO: Requires redevelopment after deprecation of Ocelot.Administration.IdentityServer4 package.")]
    public async Task ShouldClearCacheRegionViaAdministrationAPI()
    {
        int port = PortFinder.GetRandomPort();
        var ocelotUrl = DownstreamUrl(port);
        var configuration = new FileConfiguration
        {
            Routes = [
                GivenRoute(),
                GivenRoute("/test"),
            ],
            GlobalConfiguration = new()
            {
                BaseUrl = ocelotUrl,
            },
        };
        GivenThereIsAConfiguration(configuration);
        const string AdminPath = "/administration";

        //GivenOcelotIsRunning(s => WithCacheManagerAndAdministrationForExternalJwtServer(s, AdminPath));
        using var ocelot = await GivenOcelotHostIsRunning(
            WithBasicConfiguration, // Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate,
            s => WithCacheManagerAndAdministrationForExternalJwtServer(s, AdminPath), // Action<IServiceCollection> configureServices,
            WithUseOcelot, // Action<IApplicationBuilder> configureApp,
            (host) => host.UseUrls(ocelotUrl) // Action<IWebHostBuilder> configureHost
        );
        ocelotClient = new()
        {
            BaseAddress = new(ocelotUrl),
        };
        bool isExternal = true;
        await GivenThereIsExternalJwtSigningService(OcelotScopes.OcAdmin);
        var token = await GivenIHaveATokenWithUrlPath(
            path: !isExternal ? AdminPath : string.Empty,
            scope: OcelotScopes.OcAdmin);
        GivenIHaveAddedATokenToMyRequest(token);

        //await WhenIGetUrlOnTheApiGateway("/");
        //ThenTheStatusCodeShouldBeOK(); // currently HttpStatusCode.BadGateway
        response = await ocelotClient.DeleteAsync($"{AdminPath}/outputcache/{TestName()}");
        ThenTheStatusCodeShouldBe(HttpStatusCode.NoContent); // currently HttpStatusCode.Unauthorized
    }

    public static FileCacheOptions DefaultFileCacheOptions { get; set; } = new()
    {
        TtlSeconds = 10,
    };

    private FileRoute GivenRoute(string upstream = null, FileCacheOptions options = null) => new()
    {
        DownstreamHostAndPorts = [ Localhost(80) ],
        DownstreamScheme = Uri.UriSchemeHttps,
        DownstreamPathTemplate = "/",
        UpstreamHttpMethod = [HttpMethods.Get],
        UpstreamPathTemplate = upstream ?? "/",
        FileCacheOptions = options ?? DefaultFileCacheOptions,
    };

    private void WithCacheManagerAndAdministrationForExternalJwtServer(IServiceCollection services,
        string adminPath,
        [CallerMemberName] string testName = nameof(CacheManagerTests))
    {
        static void WithSettings(ConfigurationBuilderCachePart settings)
        {
            settings.WithDictionaryHandle();
        }
        services.AddMvc(option => option.EnableEndpointRouting = false);
        services.AddOcelot()
            .AddCacheManager(WithSettings)

            //.AddAdministration(adminPath, "secret") // this is for internal server
            .AddAdministration(adminPath, testName,
                externalJwtServer: new Uri(JwtSigningServerUrl)); // this is for external server
    }

    public override void Dispose()
    {
        Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE", string.Empty);
        Environment.SetEnvironmentVariable("OCELOT_CERTIFICATE_PASSWORD", string.Empty);
        base.Dispose();
    }
}
