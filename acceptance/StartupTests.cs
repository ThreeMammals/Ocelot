using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests;

public class StartupTests : Steps
{
    /// <summary>
    /// TODO: The test should be moved to the Configuration namespace since it is part of Configuration Repository and/or Configuration Middleware feats.
    /// </summary>
    [Fact]
    [Trait("Bug", "463")] // https://github.com/ThreeMammals/Ocelot/issues/463
    [Trait("PR", "506")] // https://github.com/ThreeMammals/Ocelot/pull/506
    public void Should_not_try_and_write_to_disk_on_startup_when_not_using_admin_api()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        var fakeRepo = new FakeFileConfigurationRepository();
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, "Hello from Laura"))
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

    private class FakeFileConfigurationRepository : IFileConfigurationRepository
    {
        public FileConfiguration Get() => throw new NotImplementedException();
        public Task<FileConfiguration> GetAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Set(FileConfiguration configuration) => throw new NotImplementedException();
        public Task SetAsync(FileConfiguration configuration, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
