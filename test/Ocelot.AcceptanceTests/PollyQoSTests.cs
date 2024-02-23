﻿using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests
{
    public class PollyQoSTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;

        public PollyQoSTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        private static FileConfiguration FileConfigurationFactory(int port, QoSOptions options, string httpMethod = nameof(HttpMethods.Get))
            => new()
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = Uri.UriSchemeHttp,
                        DownstreamHostAndPorts = new()
                        {
                            new("localhost", port),
                        },
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new() {httpMethod},
                        QoSOptions = new FileQoSOptions(options),
                    },
                },
            };

        [Fact]
        public void Should_not_timeout()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(10, 500, 1000, null), HttpMethods.Post);

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, string.Empty, 10))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithPolly())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void Should_timeout()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(0, 0, 1000, null), HttpMethods.Post);

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 201, string.Empty, 2100))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithPolly())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .BDDfy();
        }

        [Fact]
        public void Should_open_circuit_breaker_after_two_exceptions()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(2, 5000, 100000, null));

            this.Given(x => x.GivenThereIsABrokenServiceRunningOn($"http://localhost:{port}"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithPolly())
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .BDDfy();
        }

        [Fact]
        public void Should_open_circuit_breaker_then_close()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(2, 500, 1000, null));

            this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn($"http://localhost:{port}", "Hello from Laura"))
                .Given(x => _steps.GivenThereIsAConfiguration(configuration))
                .Given(x => _steps.GivenOcelotIsRunningWithPolly())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min ExceptionsAllowedBeforeBreaking is 2
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => GivenIWaitMilliseconds(3000))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void Open_circuit_should_not_effect_different_route()
        {
            var port1 = PortFinder.GetRandomPort();
            var port2 = PortFinder.GetRandomPort();
            var qos1 = new QoSOptions(2, 500, 1000, null);

            var configuration = FileConfigurationFactory(port1, qos1);
            var route2 = configuration.Routes[0].Clone() as FileRoute;
            route2.DownstreamHostAndPorts[0].Port = port2;
            route2.UpstreamPathTemplate = "/working";
            route2.QoSOptions = new();
            configuration.Routes.Add(route2);

            this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn($"http://localhost:{port1}", "Hello from Laura"))
                .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port2}", 200, "Hello from Tom", 0))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithPolly())
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min ExceptionsAllowedBeforeBreaking is 2
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/working"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Tom"))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .And(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => GivenIWaitMilliseconds(3000))
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        [Trait("Bug", "1833")]
        public void Should_timeout_per_default_after_90_seconds()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(new FileQoSOptions()), HttpMethods.Get);
            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 201, string.Empty, 95000))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunningWithPolly())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .BDDfy();
        }

        private static void GivenIWaitMilliseconds(int ms) => Thread.Sleep(ms);

        private void GivenThereIsABrokenServiceRunningOn(string url)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("this is an exception");
            });
        }

        private void GivenThereIsAPossiblyBrokenServiceRunningOn(string url, string responseBody)
        {
            var requestCount = 0;
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                if (requestCount == 2)
                {
                    // in Polly v8 
                    // MinimumThroughput (ExceptionsAllowedBeforeBreaking) must be 2 or more
                    // BreakDuration (DurationOfBreak) must be 500 or more
                    // Timeout (TimeoutValue) must be 1000 or more
                    // so we wait for 2.1 seconds to make sure the circuit is open
                    // DurationOfBreak * ExceptionsAllowedBeforeBreaking + Timeout
                    // 500 * 2 + 1000 = 2000 minimum + 100 milliseconds to exceed the minimum
                    await Task.Delay(2100);
                }

                requestCount++;
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(responseBody);
            });
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody, int timeout)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                Thread.Sleep(timeout);
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            });
        }

        public void Dispose()
        {
            _serviceHandler?.Dispose();
            _steps.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
