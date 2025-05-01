using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

namespace Ocelot.AcceptanceTests;

public sealed class ReturnsErrorTests : Steps
{
    private readonly ServiceHandler _serviceHandler;

    public ReturnsErrorTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_return_bad_gateway_error_if_downstream_service_doesnt_respond()
    {
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = 53877,
                            },
                        },
                        DownstreamScheme = "http",
                    },
                },
        };

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
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
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
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => x.GivenOcelotIsRunningWithLogger())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenWarningShouldBeLogged(1))
            .BDDfy();
    }

    private void GivenOcelotIsRunningWithLogger()
    {
        GivenOcelotIsRunningWithServices(s =>
        {
            s.AddOcelot();
            s.AddSingleton<IOcelotLoggerFactory, MockLoggerFactory>();
        });
    }

    private void GivenThereIsAServiceRunningOn(string url)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(url, context => throw new Exception("BLAMMMM"));
    }

    private void ThenWarningShouldBeLogged(int howMany)
    {
        var loggerFactory = (MockLoggerFactory)_ocelotServer.Host.Services.GetService<IOcelotLoggerFactory>();
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
