using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Requester;
using System.Reflection;

namespace Ocelot.AcceptanceTests
{
    public sealed class PollyQoSTests : Steps, IDisposable
    {
        private readonly ServiceHandler _serviceHandler;

        public PollyQoSTests()
        {
            _serviceHandler = new ServiceHandler();
        }

        public override void Dispose()
        {
            _serviceHandler.Dispose();
            base.Dispose();
        }

        private static FileConfiguration FileConfigurationFactory(int port, QoSOptions options, string httpMethod = nameof(HttpMethods.Get)) => new()
        {
            Routes = new()
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
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunningWithPolly())
                .And(x => GivenThePostHasContent("postContent"))
                .When(x => WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void Should_timeout()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(0, 0, 1000, null), HttpMethods.Post);

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 201, string.Empty, 2100))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunningWithPolly())
                .And(x => GivenThePostHasContent("postContent"))
                .When(x => WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .BDDfy();
        }

        [Fact]
        public void Should_open_circuit_breaker_after_two_exceptions()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(2, 5000, 100000, null));

            this.Given(x => x.GivenThereIsABrokenServiceRunningOn($"http://localhost:{port}"))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunningWithPolly())
                .And(x => WhenIGetUrlOnTheApiGateway("/"))
                .And(x => WhenIGetUrlOnTheApiGateway("/"))
                .And(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .BDDfy();
        }

        [Fact]
        public void Should_open_circuit_breaker_then_close()
        {
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(2, 500, 1000, null));

            this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn($"http://localhost:{port}", "Hello from Laura"))
                .Given(x => GivenThereIsAConfiguration(configuration))
                .Given(x => GivenOcelotIsRunningWithPolly())
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
                .When(x => WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min ExceptionsAllowedBeforeBreaking is 2
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
                .Given(x => WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => WhenIGetUrlOnTheApiGateway("/"))
                .Given(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .Given(x => GivenIWaitMilliseconds(3000))
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
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
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunningWithPolly())
                .And(x => WhenIGetUrlOnTheApiGateway("/"))
                .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => WhenIGetUrlOnTheApiGateway("/")) // repeat same request because min ExceptionsAllowedBeforeBreaking is 2
                .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
                .And(x => WhenIGetUrlOnTheApiGateway("/"))
                .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => WhenIGetUrlOnTheApiGateway("/working"))
                .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Tom"))
                .And(x => WhenIGetUrlOnTheApiGateway("/"))
                .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => WhenIGetUrlOnTheApiGateway("/"))
                .And(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
                .And(x => GivenIWaitMilliseconds(3000))
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        [Trait("Bug", "1833")]
        public void Should_timeout_per_default_after_90_seconds()
        {
            // Arrange
            var port = PortFinder.GetRandomPort();
            var configuration = FileConfigurationFactory(port, new QoSOptions(new FileQoSOptions()), HttpMethods.Get);
            GivenThereIsAServiceRunningOn(DownstreamUrl(port), (int)HttpStatusCode.Created, string.Empty, 3500); // 3.5s > 3s -> ServiceUnavailable
            GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunningWithPolly();
            GivenIHackDefaultTimeoutValue(3); // after 3 secs -> Timeout exception aka request cancellation

            // Act
            WhenIGetUrlOnTheApiGateway("/");

            // Assert
            ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        private void GivenIHackDefaultTimeoutValue(int defaultTimeoutSeconds)
        {
            var field = typeof(MessageInvokerPool).GetField("_requestTimeoutSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            var service = _ocelotServer.Services.GetService(typeof(IMessageInvokerPool));
            field.SetValue(service, defaultTimeoutSeconds); // hack the value of default 90 seconds
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
    }
}
