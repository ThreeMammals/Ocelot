//using Ocelot.Administration;
//x using CacheManager.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Authentication;
//x using Ocelot.Cache.CacheManager;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
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
        //int port = PortFinder.GetRandomPort();
        //var ocelotUrl = DownstreamUrl(port);
        var configuration = new FileConfiguration
        {
            Routes = [
                GivenRoute(),
                GivenRoute("/test"),
            ],
            GlobalConfiguration = new()
            {
                //BaseUrl = ocelotUrl,
            },
        };
        //GivenThereIsAConfiguration(configuration);
        const string AdminPath = "/administration";

        //GivenOcelotIsRunning(s => WithCacheManagerAndAdministrationForExternalJwtServer(s, AdminPath));
        var port = await GivenOcelotHostIsRunning(
            WithBasicConfiguration, // Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate,
            s => WithCacheManagerAndAdministrationForExternalJwtServer(s, AdminPath), // Action<IServiceCollection> configureServices,
            WithUseOcelot, // Action<IApplicationBuilder> configureApp,
            null, null, null, null);
        bool isExternal = true;
        await GivenThereIsExternalJwtSigningService([OcelotScopes.OcAdmin], Xunit.TestContext.Current.CancellationToken);
        var token = await GivenIHaveATokenWithUrlPath(
            path: !isExternal ? AdminPath : string.Empty,
            scope: OcelotScopes.OcAdmin);
        GivenIHaveAddedATokenToMyRequest(token);

        //await WhenIGetUrlOnTheApiGateway("/");
        //ThenTheStatusCodeShouldBeOK(); // currently HttpStatusCode.BadGateway
        response = await ocelotClient.DeleteAsync($"{AdminPath}/outputcache/{TestName()}", Xunit.TestContext.Current.CancellationToken);
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
        //x static void WithSettings(ConfigurationBuilderCachePart settings)
        //x {
        //x    settings.WithDictionaryHandle();
        //x }
        services.AddMvc(option => option.EnableEndpointRouting = false);
        services.AddOcelot()
            //x.AddCacheManager(WithSettings)

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
