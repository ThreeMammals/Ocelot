using Butterfly.Client.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Tracing.Butterfly;
using Ocelot.Tracing.OpenTracing;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using Xunit.Abstractions;

namespace Ocelot.AcceptanceTests;

public class OpenTracingTests : Steps
{
    private readonly ITestOutputHelper _output;

    public OpenTracingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Should_forward_tracing_information_from_ocelot_and_downstream_services()
    {
        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/api/values",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port1,
                        },
                    },
                    UpstreamPathTemplate = "/api001/values",
                    UpstreamHttpMethod = ["Get"],
                    HttpHandlerOptions = new FileHttpHandlerOptions
                    {
                        UseTracing = true,
                    },
                },
                new()
                {
                    DownstreamPathTemplate = "/api/values",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port2,
                        },
                    },
                    UpstreamPathTemplate = "/api002/values",
                    UpstreamHttpMethod = ["Get"],
                    HttpHandlerOptions = new FileHttpHandlerOptions
                    {
                        UseTracing = true,
                    },
                },
            },
        };

        var tracingPort = PortFinder.GetRandomPort();
        var fakeTracer = new FakeTracer();
        this.Given(_ => GivenFakeOpenTracing(tracingPort))
            .And(_ => GivenServiceIsRunning(port1, "/api/values", HttpStatusCode.OK, "Hello from Laura", tracingPort, "Service One"))
            .And(_ => GivenServiceIsRunning(port2, "/api/values", HttpStatusCode.OK, "Hello from Tom", tracingPort, "Service Two"))
            .And(_ => GivenThereIsAConfiguration(configuration))
            .And(_ => GivenOcelotIsRunningUsingOpenTracing(fakeTracer))
            .When(_ => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .When(_ => WhenIGetUrlOnTheApiGateway("/api002/values"))
            .Then(_ => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe("Hello from Tom"))
            .And(_ => ThenTheTracerIsCalled(fakeTracer))
            .BDDfy();
    }

    [Fact]
    public void Should_return_tracing_header()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/values",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        UpstreamPathTemplate = "/api001/values",
                        UpstreamHttpMethod = ["Get"],
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            UseTracing = true,
                        },
                        DownstreamHeaderTransform = new Dictionary<string, string>
                        {
                            {"Trace-Id", "{TraceId}"},
                            {"Tom", "Laura"},
                        },
                    },
                },
        };

        var butterflyPort = PortFinder.GetRandomPort();
        var fakeTracer = new FakeTracer();
        this.Given(x => GivenFakeOpenTracing(butterflyPort))
            .And(x => GivenServiceIsRunning(port, "/api/values", HttpStatusCode.OK, "Hello from Laura", butterflyPort, "Service One"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningUsingOpenTracing(fakeTracer))
            .When(x => WhenIGetUrlOnTheApiGateway("/api001/values"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .And(x => ThenTheResponseHeaderExists("Trace-Id"))
            .And(x => ThenTheResponseHeaderIs("Tom", "Laura"))
            .BDDfy();
    }

    private void GivenOcelotIsRunningUsingOpenTracing(OpenTracing.ITracer fakeTracer)
    {
        GivenOcelotIsRunning(s =>
        {
            s.AddOcelot().AddOpenTracing();
            s.AddSingleton(fakeTracer);
        });
    }

    private void ThenTheTracerIsCalled(FakeTracer fakeTracer)
    {
        var commandOnAllStateMachines = Wait.For(10_000).Until(() => fakeTracer.BuildSpanCalled >= 2);
        _output.WriteLine($"fakeTracer.BuildSpanCalled is {fakeTracer.BuildSpanCalled}");
        commandOnAllStateMachines.ShouldBeTrue();
    }

    private void GivenServiceIsRunning(int port, string basePath, HttpStatusCode statusCode, string responseBody, int butterflyPort, string serviceName)
    {
        void WithButterfly(IServiceCollection services) => services
            .AddButterfly(option =>
            {
                option.CollectorUrl = DownstreamUrl(butterflyPort);
                option.Service = serviceName;
                option.IgnoredRoutesRegexPatterns = Array.Empty<string>();
            });
        handler.GivenThereIsAServiceRunningOn(DownstreamUrl(port), basePath, WithButterfly, context =>
        {
            var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                ? context.Request.PathBase.Value : context.Request.Path.Value;
            bool oK = downstreamPath == basePath;
            context.Response.StatusCode = oK ? (int)statusCode : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(oK ? responseBody : "downstream path didn't match base path");
        });
    }

    private void GivenFakeOpenTracing(int port)
    {
        handler.GivenThereIsAServiceRunningOn(port, context => context.Response.WriteAsync("OK..."));
    }
}

internal class FakeTracer : OpenTracing.ITracer
{
    public IScopeManager ScopeManager => throw new NotImplementedException();
    public ISpan ActiveSpan => throw new NotImplementedException();

    public ISpanBuilder BuildSpan(string operationName)
    {
        BuildSpanCalled++;
        return new FakeSpanBuilder();
    }

    public int BuildSpanCalled { get; set; }

    public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier)
    {
        ExtractCalled++;
        return null;
    }

    public int ExtractCalled { get; set; }

    public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier)
    {
        InjectCalled++;
    }

    public int InjectCalled { get; set; }
}

internal class FakeSpanBuilder : ISpanBuilder
{
    public ISpanBuilder AddReference(string referenceType, ISpanContext referencedContext) => throw new NotImplementedException();
    public ISpanBuilder AsChildOf(ISpanContext parent) => throw new NotImplementedException();
    public ISpanBuilder AsChildOf(ISpan parent) => throw new NotImplementedException();
    public ISpanBuilder IgnoreActiveSpan() => throw new NotImplementedException();
    public ISpan Start() => throw new NotImplementedException();
    public IScope StartActive() => throw new NotImplementedException();
    public IScope StartActive(bool finishSpanOnDispose) => new FakeScope(finishSpanOnDispose);
    public ISpanBuilder WithStartTimestamp(DateTimeOffset timestamp) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, string value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, bool value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, int value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(string key, double value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(BooleanTag tag, bool value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(IntOrStringTag tag, string value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(IntTag tag, int value) => throw new NotImplementedException();
    public ISpanBuilder WithTag(StringTag tag, string value) => throw new NotImplementedException();
}

internal class FakeScope : IScope
{
    private readonly bool finishSpanOnDispose;

    public FakeScope(bool finishSpanOnDispose)
    {
        this.finishSpanOnDispose = finishSpanOnDispose;
    }

    public ISpan Span { get; } = new FakeSpan();

    public void Dispose()
    {
        if (finishSpanOnDispose)
        {
            Span.Finish();
        }
    }
}

internal class FakeSpan : ISpan
{
    public ISpanContext Context => new FakeSpanContext();
    public void Finish() { }
    public void Finish(DateTimeOffset finishTimestamp) => throw new NotImplementedException();
    public string GetBaggageItem(string key) => throw new NotImplementedException();
    public ISpan Log(IEnumerable<KeyValuePair<string, object>> fields) => this;
    public ISpan Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields) => throw new NotImplementedException();
    public ISpan Log(string @event) => throw new NotImplementedException();
    public ISpan Log(DateTimeOffset timestamp, string @event) => throw new NotImplementedException();
    public ISpan SetBaggageItem(string key, string value) => throw new NotImplementedException();
    public ISpan SetOperationName(string operationName) => throw new NotImplementedException();
    public ISpan SetTag(string key, string value) => this;
    public ISpan SetTag(string key, bool value) => this;
    public ISpan SetTag(string key, int value) => this;
    public ISpan SetTag(string key, double value) => this;
    public ISpan SetTag(BooleanTag tag, bool value) => this;
    public ISpan SetTag(IntOrStringTag tag, string value) => this;
    public ISpan SetTag(IntTag tag, int value) => this;
    public ISpan SetTag(StringTag tag, string value) => this;
}

internal class FakeSpanContext : ISpanContext
{
    public static string FakeTraceId = "FakeTraceId";
    public static string FakeSpanId = "FakeSpanId";
    public string TraceId => FakeTraceId;
    public string SpanId => FakeSpanId;
    public IEnumerable<KeyValuePair<string, string>> GetBaggageItems() => throw new NotImplementedException();
}
