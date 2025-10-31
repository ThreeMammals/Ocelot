using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Tracing.Butterfly;
using System.Net.Http.Headers;

namespace Ocelot.UnitTests.Logging;

public class ButterflyTracerTests : UnitTest
{
    private ButterflyTracer sut;
    private readonly IServiceProvider _serviceProvider;
    private readonly FakeServiceTracer _serviceTracer;
    private readonly Mock<ITracer> _tracer = new();

    public ButterflyTracerTests()
    {
        IServiceCollection services = new ServiceCollection();
        _serviceTracer = new FakeServiceTracer(_tracer.Object, TestID);
        services.AddSingleton<IServiceTracer>(_serviceTracer);
        _serviceProvider = services.BuildServiceProvider(true);
        sut = new(_serviceProvider);
    }

    [Fact]
    public void Event()
    {
        // Arrange
        HttpContext context = new DefaultHttpContext();
        _tracer.Setup(x => x.Extract(It.IsAny<ICarrierReader>(), It.IsAny<ICarrier>()))
            .Returns(_serviceTracer.Span.Object.SpanContext);

        // Act
        sut.Event(context, TestName());

        // Assert
        Assert.Contains("_request_span_", context.Items.Keys);
        var actualSpan = context.Items["_request_span_"] as ISpan;
        Assert.NotNull(actualSpan);
    }

    [Fact]
    public void Event_TracerIsNull()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider(true);
        sut = new(serviceProvider);

        // Act
        DefaultHttpContext context = new();
        sut.Event(context, TestName());

        // Assert
        Assert.False(context.Items.ContainsKey("_request_span_"));
    }

    [Fact]
    public async Task SendAsync()
    {
        // Arrange
        ISpanContext spanContext;
        ICarrierWriter carrierWriter;
        ICarrier carrier = null;
        _tracer.Setup(x => x.Inject(It.IsAny<ISpanContext>(), It.IsAny<ICarrierWriter>(), It.IsAny<ICarrier>()))
            .Callback<ISpanContext, ICarrierWriter, ICarrier>((a, b, c) =>
            {
                spanContext = a;
                carrierWriter = b;
                carrier = c;
            });
        var request = new HttpRequestMessage(HttpMethod.Get, "https://ocelot.net:34567");
        request.Headers.Add(nameof(HttpStatusCode), HttpStatusCode.Created.ToString());
        request.Headers.Add(ButterflyTracer.PrefixSpanId, "bla-bla");

        // Act
        var actual = await sut.SendAsync(request,
            _serviceTracer.AddTraceIdToRepo,
            _serviceTracer.BaseSendAsync,
            CancellationToken.None);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(HttpStatusCode.Created, actual.StatusCode);
        var actualBody = await actual.Content.ReadAsStringAsync();
        Assert.Equal($"{TestID}->https://ocelot.net:34567/->HttpStatusCode(Created);ot-spanId({TestID})", actualBody);

        var actualCarrier = carrier as DelegatingCarrier<HttpRequestHeaders>;
        Assert.NotNull(actualCarrier);
        actualCarrier["testMe"] = "testMe";
        var testMe = request.Headers.GetValues("testMe").FirstOrDefault();
        Assert.NotNull(testMe);
        Assert.Equal("testMe", testMe);
    }

    internal class FakeServiceTracer : IServiceTracer
    {
        private readonly ITracer _tracer;
        private readonly string _testId;
        public readonly Mock<ISpan> Span = new();
        public readonly TagCollection TagCollection = new();
        public readonly LogCollection LogCollection = new();
        public FakeServiceTracer(ITracer tracer, string testId)
        {
            _tracer = tracer;
            _testId = testId;
            var spanContext = new Mock<ISpanContext>();
            spanContext.SetupGet(x => x.TraceId).Returns(_testId);
            spanContext.SetupGet(x => x.SpanId).Returns(_testId);
            Span.SetupGet(x => x.SpanContext).Returns(spanContext.Object);
            Span.SetupGet(x => x.Tags).Returns(TagCollection);
            Span.SetupGet(x => x.Logs).Returns(LogCollection);
        }

        public ITracer Tracer => _tracer;
        public string ServiceName => throw new NotImplementedException();
        public string Environment => throw new NotImplementedException();
        public string Identity => throw new NotImplementedException();
        public ISpan Start(ISpanBuilder spanBuilder) => Span.Object;

        public string ActualTraceId;
        public void AddTraceIdToRepo(string traceId) => ActualTraceId = traceId;
        public Task<HttpResponseMessage> BaseSendAsync(HttpRequestMessage message, CancellationToken token)
        {
            var headers = message.Headers.Select(h => $"{h.Key}({h.Value.Csv()})");
            var serializedHeaders = string.Join(';', headers);
            return Task.FromResult(new HttpResponseMessage()
            {
                StatusCode = Enum.Parse<HttpStatusCode>(message.Headers.GetValues(nameof(HttpStatusCode)).First()),
                Content = new StringContent($"{_testId}->{message.RequestUri}->{serializedHeaders}"),
            });
        }
    }
}
