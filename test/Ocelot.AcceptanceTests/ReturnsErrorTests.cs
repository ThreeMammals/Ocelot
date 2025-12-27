using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

namespace Ocelot.AcceptanceTests;

[Trait("Commit", "84256e7")] // https://github.com/ThreeMammals/Ocelot/commit/84256e7bac0fa2c8ceba92bd8fe64c8015a37cea
public sealed class ReturnsErrorTests : Steps
{
    [Fact]
    [Trait("Feat", "603")] // https://github.com/ThreeMammals/Ocelot/issues/603
    [Trait("PR", "1149")] // https://github.com/ThreeMammals/Ocelot/pull/1149
    [Trait("Release", "15.0.1")] // https://github.com/ThreeMammals/Ocelot/releases/tag/15.0.1
    public void Should_return_bad_gateway_error_if_downstream_service_doesnt_respond()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
            .BDDfy();
    }

    [Fact]
    [Trait("Commit", "1599694")] // https://github.com/ThreeMammals/Ocelot/commit/159969483b64c5491b1d86b1aa4dac7b4b2a3ba1
    [Trait("Commit", "ef3deec")] // https://github.com/ThreeMammals/Ocelot/commit/ef3deec8da78fd282f6b5f2bff8e6d6853496c31
    [Trait("Release", "1.4.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.4.0
    public void Should_return_internal_server_error_if_downstream_service_returns_internal_server_error()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .BDDfy();
    }

    [Fact]
    [Trait("Feat", "492")] // https://github.com/ThreeMammals/Ocelot/issues/492
    [Trait("PR", "1055")] // https://github.com/ThreeMammals/Ocelot/pull/1055
    [Trait("Release", "14.0.4")] // https://github.com/ThreeMammals/Ocelot/releases/tag/14.0.4
    [Trait("Commit", "9da55ea")] // https://github.com/ThreeMammals/Ocelot/commit/9da55ea037d0df3b8b22d32dec9b004a50709251
    [Trait("PR", "1106")] // https://github.com/ThreeMammals/Ocelot/pull/1106
    public void Should_log_warning_if_downstream_service_returns_internal_server_error()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => x.GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithLogger())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenWarningShouldBeLogged(1))
            .BDDfy();
    }

    private void GivenOcelotIsRunningWithLogger()
    {
        GivenOcelotIsRunning(s =>
        {
            s.AddOcelot();
            s.AddSingleton<IOcelotLoggerFactory, MockLoggerFactory>();
        });
    }

    private void GivenThereIsAServiceRunningOn(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, context => throw new Exception("BLAMMMM"));
    }

    private void ThenWarningShouldBeLogged(int howMany)
    {
        var loggerFactory = (MockLoggerFactory)ocelotServer.Host.Services.GetService<IOcelotLoggerFactory>();
        loggerFactory.Verify(Times.Exactly(howMany));
    }

    internal class MockLoggerFactory : IOcelotLoggerFactory
    {
        private Mock<IOcelotLogger> _logger;

        public IOcelotLogger CreateLogger<T>()
        {
            if (_logger != null)
            {
                return _logger.Object;
            }

            _logger = new Mock<IOcelotLogger>();
            _logger.Setup(x => x.LogWarning(It.IsAny<string>())).Verifiable();
            _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>())).Verifiable();

            return _logger.Object;
        }

        public void Verify(Times howMany)
            => _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()), howMany);
    }
}
