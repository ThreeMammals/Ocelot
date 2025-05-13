using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

namespace Ocelot.AcceptanceTests;

public sealed class ReturnsErrorTests : Steps
{
    public ReturnsErrorTests()
    {
    }

    [Fact]
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
