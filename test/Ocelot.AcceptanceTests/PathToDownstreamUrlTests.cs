using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public class PathToDownstreamUrlTests : IDisposable
{
    private IWebHost _servicebuilder;
    private readonly Steps _steps;
    private readonly Action<IdentityServerAuthenticationOptions> _options;
    private readonly string _serverRootUrl;
    private string _downstreamFinalPath;

    public PathToDownstreamUrlTests()
    {
        var serverPort = PortFinder.GetRandomPort();
        _serverRootUrl = $"http://localhost:{serverPort}";
        _steps = new Steps();
        _options = o =>
        {
            o.ApiName = "api";
            o.RequireHttpsMetadata = false;
        };
    }

    [Fact]
    [Trait("Bug", "2116")]
    public void Should_change_downstream_path_by_upstream_path_when_path_contains_malicious_characters()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                   {
                       new()
                       {
                           DownstreamPathTemplate = "/routed/api/{path}",
                           DownstreamHostAndPorts = new List<FileHostAndPort>
                           {
                               new()
                               {
                                   Host = "localhost",
                                   Port = port,
                               },
                           },
                           DownstreamScheme = "http",
                           UpstreamPathTemplate = "/api/{path}",
                           UpstreamHttpMethod = new List<string> { "Get" },
                       },
                   },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning(_options, "Test"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/api/debug("))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheDownstreamPathIs("/routed/api/debug("))
                .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string url, int statusCode)
    {
        _servicebuilder = new WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .Configure(app =>
            {
                app.Run(context =>
                {
                    _downstreamFinalPath = context.Request.Path.Value;
                    return Task.CompletedTask;
                });
            })
            .Build();

        _servicebuilder.Start();
    }

    private void ThenTheDownstreamPathIs(string path)
    {
        _downstreamFinalPath.ShouldBe(path);
    }

    public void Dispose()
    {
        _servicebuilder?.Dispose();
        _steps.Dispose();
    }
}
