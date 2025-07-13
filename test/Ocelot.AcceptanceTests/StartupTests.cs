using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Responses;

namespace Ocelot.AcceptanceTests;

public class StartupTests : Steps
{
    public StartupTests()
    {
    }

    [Fact]
    public void Should_not_try_and_write_to_disk_on_startup_when_not_using_admin_api()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        var fakeRepo = new FakeFileConfigurationRepository();
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithBlowingUpDiskRepo(fakeRepo))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    private void GivenOcelotIsRunningWithBlowingUpDiskRepo(IFileConfigurationRepository fake)
    {
        void WithFakeRepo(IServiceCollection s) => s.AddSingleton(fake).AddOcelot();
        GivenOcelotIsRunning(WithFakeRepo);
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value) ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(oK ? responseBody : "downstream path didn't match base path");
        });
    }

    private class FakeFileConfigurationRepository : IFileConfigurationRepository
    {
        public Task<Response<FileConfiguration>> Get() => throw new NotImplementedException();
        public Task<Response> Set(FileConfiguration fileConfiguration) => throw new NotImplementedException();
    }
}
