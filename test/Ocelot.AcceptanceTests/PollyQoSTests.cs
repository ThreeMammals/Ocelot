using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Requester;
using System.Reflection;

namespace Ocelot.AcceptanceTests;

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

    private static FileRoute GivenRoute(int port, QoSOptions options, string httpMethod = null, string upstream = null) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        DownstreamHostAndPorts = new()
        {
            new("localhost", port),
        },
        UpstreamPathTemplate = upstream ?? "/",
        UpstreamHttpMethod = new() { httpMethod ?? HttpMethods.Get },
        QoSOptions = new(options),
    };

    [Fact]
    public void Should_not_timeout()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(10, 500, 1000, null), HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, string.Empty, 10))
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
        var route = GivenRoute(port, new QoSOptions(0, 0, 1000, null), HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty, 2100))
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
        var route = GivenRoute(port, new QoSOptions(2, 1000, 100000, null));
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // Polly status
            .BDDfy();
    }

    [Fact]
    [Trait("Bug", "2085")]
    public void Should_open_circuit_breaker_for_DefaultBreakDuration()
    {
        int invalidDuration = QoSOptions.LowBreakDuration; // valid value must be >500ms, exact 500ms is invalid
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(2, invalidDuration, 100000, null));
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsABrokenServiceRunningOn(port, HttpStatusCode.InternalServerError))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .And(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError))
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // Polly status
            .Given(x => GivenIWaitMilliseconds(QoSOptions.DefaultBreakDuration - 500)) // BreakDuration is not elapsed
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // still opened
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable)) // still opened
            .Given(x => GivenThereIsABrokenServiceOnline(HttpStatusCode.NotFound))
            .Given(x => GivenIWaitMilliseconds(500)) // BreakDuration should elapse now
            .When(x => WhenIGetUrlOnTheApiGateway("/")) // closed, service online
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound)) // closed, service online
            .And(x => ThenTheResponseBodyShouldBe(nameof(HttpStatusCode.NotFound)))
            .BDDfy();
    }

    [Fact]
    public void Should_open_circuit_breaker_then_close()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(2, 500, 1000, null));
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn(port, "Hello from Laura"))
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
        var route = GivenRoute(port1, qos1);
        var route2 = GivenRoute(port2, new(new FileQoSOptions()), null, "/working");
        var configuration = GivenConfiguration(route, route2);

        this.Given(x => x.GivenThereIsAPossiblyBrokenServiceRunningOn(port1, "Hello from Laura"))
            .And(x => x.GivenThereIsAServiceRunningOn(port2, HttpStatusCode.OK, "Hello from Tom", 0))
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
        var defTimeoutMs = 1_000 * RoutesCreator.DefaultRequestTimeoutSeconds; // original value is 90 seconds
        defTimeoutMs = 1_000 * 3; // override value
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, new QoSOptions(new FileQoSOptions()), HttpMethods.Get);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty, defTimeoutMs + 500)) // 3.5s > 3s -> ServiceUnavailable
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithPolly())
            .And(x => GivenIHackDefaultTimeoutValue(defTimeoutMs)) // after 3 secs -> Timeout exception aka request cancellation
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable))
            .BDDfy();
    }

    private void GivenIHackDefaultTimeoutValue(int defaultTimeoutMs)
    {
        var field = typeof(MessageInvokerPool).GetField("_timeoutMilliseconds", BindingFlags.NonPublic | BindingFlags.Instance);
        var service = _ocelotServer.Services.GetService(typeof(IMessageInvokerPool));
        field.SetValue(service, defaultTimeoutMs); // hack the value of default 90 seconds
    }

    private static void GivenIWaitMilliseconds(int ms) => Thread.Sleep(ms);

    private HttpStatusCode _brokenServiceStatusCode;
    private void GivenThereIsABrokenServiceRunningOn(int port, HttpStatusCode brokenStatusCode)
    {
        string url = DownstreamUrl(port);
        _brokenServiceStatusCode = brokenStatusCode;
        _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            context.Response.StatusCode = (int)_brokenServiceStatusCode;
            await context.Response.WriteAsync(_brokenServiceStatusCode.ToString());
        });
    }

    private void GivenThereIsABrokenServiceOnline(HttpStatusCode onlineStatusCode)
    {
        _brokenServiceStatusCode = onlineStatusCode;
    }

    private void GivenThereIsAPossiblyBrokenServiceRunningOn(int port, string responseBody)
    {
        var requestCount = 0;
        string url = DownstreamUrl(port);
        _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            if (requestCount == 2)
            {
                // In Polly v8:
                //   MinimumThroughput (ExceptionsAllowedBeforeBreaking) must be 2 or more
                //   BreakDuration (DurationOfBreak) must be 500 or more
                //   Timeout (TimeoutValue) must be 1000 or more
                // So, we wait for 2.1 seconds to make sure the circuit is open
                // DurationOfBreak * ExceptionsAllowedBeforeBreaking + Timeout
                // 500 * 2 + 1000 = 2000 minimum + 100 milliseconds to exceed the minimum
                await Task.Delay(2100);
            }

            requestCount++;
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(responseBody);
        });
    }

    private void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, string responseBody, int timeout)
    {
        string url = DownstreamUrl(port);
        _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            Thread.Sleep(timeout);
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(responseBody);
        });
    }
}
