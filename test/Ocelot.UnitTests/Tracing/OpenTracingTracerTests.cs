using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Tracing.OpenTracing;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace Ocelot.UnitTests.Tracing;

public class OpenTracingTracerTests : UnitTest
{
    private readonly OpenTracingTracer sut;
    private readonly Mock<ITracer> _tracer = new();
    public OpenTracingTracerTests() => sut = new(_tracer.Object);

    [Fact]
    public void Ctor_ArgNullCheck()
    {
        // Arrange, Act
        var ex = Assert.Throws<ArgumentNullException>(() => new OpenTracingTracer(null));

        // Assert
        Assert.Equal("tracer", ex.ParamName);
    }

    [Fact]
    public void Event()
    {
        // Arrange
        DefaultHttpContext context = new();

        // Act, Assert
        sut.Event(context, TestName());
    }

    [Fact]
    public async Task SendAsync()
    {
        var spanContext = new Mock<ISpanContext>();
        spanContext.SetupGet(x => x.SpanId).Returns(TestID);
        var span = new Mock<ISpan>();
        span.Setup(x => x.SetTag(It.IsAny<StringTag>(), It.IsAny<string>())).Returns(span.Object);
        span.SetupGet(x => x.Context).Returns(spanContext.Object);
        var scope = new Mock<IScope>();
        scope.SetupGet(x => x.Span).Returns(span.Object);
        var spanBuilder = new Mock<ISpanBuilder>();
        spanBuilder.Setup(x => x.StartActive(It.IsAny<bool>())).Returns(scope.Object);
        _tracer.Setup(x => x.BuildSpan(It.IsAny<string>())).Returns(spanBuilder.Object);
        string actualTraceId = null;
        void AddTraceIdToRepo(string id) => actualTraceId = id;
        _tracer.Setup(x => x.Inject<ITextMap>(It.IsAny<ISpanContext>(), It.IsAny<IFormat<ITextMap>>(), It.IsAny<ITextMap>()));
        var request = new HttpRequestMessage(HttpMethod.Post, "https://ocelot.net:55555");
        request.Headers.Add(nameof(HttpStatusCode), HttpStatusCode.NoContent.ToString());

        // Act
        var actual = await sut.SendAsync(request, AddTraceIdToRepo, BaseSendAsync, CancellationToken.None);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(HttpStatusCode.NoContent, actual.StatusCode);
        var actualBody = await actual.Content.ReadAsStringAsync();
        Assert.Equal($"{TestID}->https://ocelot.net:55555/->HttpStatusCode(NoContent)", actualBody);

        // Scenario 2: catch (HttpRequestException ex)
        Task<HttpResponseMessage> HttpRequestExceptionAsync(HttpRequestMessage message, CancellationToken token)
            => throw new HttpRequestException("bla-bla error");
        Dictionary<string, object> logged = null;
        span.Setup(x => x.Log(It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
            .Callback<IEnumerable<KeyValuePair<string, object>>>(d => logged = new(d))
            .Returns(span.Object);
        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => sut.SendAsync(request, AddTraceIdToRepo, HttpRequestExceptionAsync, CancellationToken.None));
        Assert.NotNull(ex);
        Assert.Equal("bla-bla error", ex.Message);
        span.Verify(x => x.Log(It.IsAny<IEnumerable<KeyValuePair<string, object>>>()), Times.Once);
        Assert.Contains("event", logged);
        Assert.Equal("error", (string)logged["event"]);
        Assert.Contains("error.kind", logged);
        Assert.Equal(nameof(HttpRequestException), (string)logged["error.kind"]);
        Assert.Contains("error.object", logged);
        Assert.Equal(ex, logged["error.object"]);
    }

    private Task<HttpResponseMessage> BaseSendAsync(HttpRequestMessage message, CancellationToken token)
    {
        var headers = message.Headers.Select(h => $"{h.Key}({h.Value.Csv()})");
        var serializedHeaders = string.Join(';', headers);
        return Task.FromResult(new HttpResponseMessage()
        {
            StatusCode = Enum.Parse<HttpStatusCode>(message.Headers.GetValues(nameof(HttpStatusCode)).First()),
            Content = new StringContent($"{TestID}->{message.RequestUri}->{serializedHeaders}"),
        });
    }
}
