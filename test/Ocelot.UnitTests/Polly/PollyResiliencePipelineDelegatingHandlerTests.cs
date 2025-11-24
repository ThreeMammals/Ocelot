using Microsoft.AspNetCore.Http;
using Moq.Protected;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Ocelot.Provider.Polly.Interfaces;
using Polly;
using Polly.Retry;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Polly;

public class PollyResiliencePipelineDelegatingHandlerTests
{
    private readonly Mock<DelegatingHandler> _innerHandler = new();
    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>> _pipelineProvider = new();
    private readonly Mock<IHttpContextAccessor> _contextAccessor = new();
    private readonly PollyResiliencePipelineDelegatingHandler _sut;
    private Func<string> _loggerMessage;

    public PollyResiliencePipelineDelegatingHandlerTests()
    {
        var loggerFactory = new Mock<IOcelotLoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger<PollyResiliencePipelineDelegatingHandler>())
            .Returns(_logger.Object);
        _logger.Setup(x => x.LogDebug(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => _loggerMessage = f);
        _logger.Setup(x => x.LogInformation(It.IsAny<Func<string>>()))
            .Callback<Func<string>>(f => _loggerMessage = f);
        _sut = new PollyResiliencePipelineDelegatingHandler(DownstreamRouteFactory(), _contextAccessor.Object, loggerFactory.Object);
    }

    [Fact]
    public async Task SendAsync_WithPipeline_ExecutedByPipeline()
    {
        // Arrange
        var fakeResponse = GivenHttpResponseMessage();
        SetupInnerHandler(fakeResponse);
        SetupResiliencePipelineProvider();

        // Act
        var actual = await InvokeAsync("SendAsync");

        // Assert
        ShouldHaveTestHeaderWithoutContent(actual);
        ShouldHaveCalledThePipelineProviderOnce();
        ShouldLogInformation("The Polly.ResiliencePipeline`1[System.Net.Http.HttpResponseMessage] pipeline has detected by QoS provider for the route with downstream URL ''. Going to execute request...");
        ShouldHaveCalledTheInnerHandlerOnce();
    }

    [Fact]
    public async Task SendAsync_NoPipeline_SentWithoutPipeline()
    {
        // Arrange
        const bool PipelineIsNull = true;
        var fakeResponse = GivenHttpResponseMessage();
        SetupInnerHandler(fakeResponse);
        SetupResiliencePipelineProvider(PipelineIsNull);

        // Act
        var actual = await InvokeAsync("SendAsync");

        // Assert
        ShouldHaveTestHeaderWithoutContent(actual);
        ShouldHaveCalledThePipelineProviderOnce();
        ShouldLogDebug("No pipeline was detected by QoS provider for the route with downstream URL ''.");
        ShouldHaveCalledTheInnerHandlerOnce();
    }

    private void SetupInnerHandler(HttpResponseMessage fakeResponse)
    {
        _innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(fakeResponse);
        _sut.InnerHandler = _innerHandler.Object;
    }

    private void SetupResiliencePipelineProvider(bool pipelineIsNull = false)
    {
        var resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<Exception>(),
            })
            .Build();
        _pipelineProvider.Setup(x => x.GetResiliencePipeline(It.IsAny<DownstreamRoute>()))
            .Returns(pipelineIsNull ? null : resiliencePipeline);
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.RequestServices.GetService(typeof(IPollyQoSResiliencePipelineProvider<HttpResponseMessage>)))
            .Returns(_pipelineProvider.Object);
        _contextAccessor.Setup(x => x.HttpContext)
            .Returns(httpContext.Object);
    }

    private async Task<HttpResponseMessage> InvokeAsync(string methodName)
    {
        var m = typeof(PollyResiliencePipelineDelegatingHandler).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        var task = (Task<HttpResponseMessage>)m.Invoke(_sut, new object[] { new HttpRequestMessage(), CancellationToken.None });
        var actual = await task!;
        return actual;
    }

    private static HttpResponseMessage GivenHttpResponseMessage([CallerMemberName] string headerValue = nameof(PollyResiliencePipelineDelegatingHandlerTests))
    {
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        fakeResponse.Headers.Add("X-Xunit", headerValue);
        return fakeResponse;
    }

    private static void ShouldHaveTestHeaderWithoutContent(HttpResponseMessage actual, [CallerMemberName] string headerValue = nameof(PollyResiliencePipelineDelegatingHandlerTests))
    {
        actual.ShouldNotBeNull();
        actual.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        actual.Headers.GetValues("X-Xunit").ShouldContain(headerValue);
    }

    private void ShouldHaveCalledThePipelineProviderOnce()
    {
        _pipelineProvider.Verify(a => a.GetResiliencePipeline(It.IsAny<DownstreamRoute>()),
            Times.Once);
        _pipelineProvider.VerifyNoOtherCalls();
    }

    private void ShouldHaveCalledTheInnerHandlerOnce()
    {
        _innerHandler.Protected().Verify<Task<HttpResponseMessage>>(
            "SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    private void ShouldLogDebug(string expected)
    {
        _logger.Verify(x => x.LogDebug(It.IsAny<Func<string>>()), Times.Once);
        var msg = _loggerMessage.ShouldNotBeNull().Invoke();
        msg.ShouldBe(expected);
    }

    private void ShouldLogInformation(string expected)
    {
        _logger.Verify(x => x.LogInformation(It.IsAny<Func<string>>()), Times.Once);
        var msg = _loggerMessage.ShouldNotBeNull().Invoke();
        msg.ShouldBe(expected);
    }

    private static DownstreamRoute DownstreamRouteFactory()
    {
        var options = new QoSOptions(2, 200)
        {
            TimeoutValue = 100,
        };
        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate("/")
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue("/").Build();
        return new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath)
            .Build();
    }
}
