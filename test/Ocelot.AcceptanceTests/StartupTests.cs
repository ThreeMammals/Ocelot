using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Responses;

namespace Ocelot.AcceptanceTests;

public class StartupTests : Steps
{
    private readonly ServiceHandler _serviceHandler;
    private string _downstreamPath;

    public StartupTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    [Fact]
    public void Should_not_try_and_write_to_disk_on_startup_when_not_using_admin_api()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                },
            },
        };

        var fakeRepo = new FakeFileConfigurationRepository();

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithBlowingUpDiskRepo(fakeRepo))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    private void GivenOcelotIsRunningWithBlowingUpDiskRepo(IFileConfigurationRepository fake)
    {
        GivenOcelotIsRunning(s => s.AddSingleton(fake).AddOcelot());
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;

            if (_downstreamPath != basePath)
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync("downstream path didnt match base path");
            }
            else
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            }
        });
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }

    private class FakeFileConfigurationRepository : IFileConfigurationRepository
    {
        public Task<Response<FileConfiguration>> Get()
        {
            throw new NotImplementedException();
        }

        public Task<Response> Set(FileConfiguration fileConfiguration)
        {
            throw new NotImplementedException();
        }
    }
}
