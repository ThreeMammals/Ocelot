using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests
{
    public class ReturnsErrorTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;

        public ReturnsErrorTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
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

            this.Given(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
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
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
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
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithLogger())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenWarningShouldBeLogged(1))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, context => throw new Exception("BLAMMMM"));
        }

        public void Dispose()
        {
            _serviceHandler?.Dispose();
            _steps.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
