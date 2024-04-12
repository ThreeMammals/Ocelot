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

namespace Ocelot.UnitTests.Polly;

public class PollyResiliencePipelineDelegatingHandlerTests
{
    private readonly Mock<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>> _pollyQoSResiliencePipelineProviderMock;
    private readonly Mock<IHttpContextAccessor> _contextAccessorMock;
    private readonly PollyResiliencePipelineDelegatingHandler _sut;

    public PollyResiliencePipelineDelegatingHandlerTests()
    {
        _pollyQoSResiliencePipelineProviderMock = new Mock<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>>();

        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var loggerMock = new Mock<IOcelotLogger>();
        _contextAccessorMock = new Mock<IHttpContextAccessor>();

        loggerFactoryMock.Setup(x => x.CreateLogger<PollyResiliencePipelineDelegatingHandler>())
            .Returns(loggerMock.Object);
        loggerMock.Setup(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>()));

        _sut = new PollyResiliencePipelineDelegatingHandler(DownstreamRouteFactory(), _contextAccessorMock.Object, loggerFactoryMock.Object);
    }

    [Fact]
    public async void SendAsync_OnePolicy()
    {
        // Arrange
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
        fakeResponse.Headers.Add("X-Xunit", nameof(SendAsync_OnePolicy));

        // setup the inner handler for PollyResiliencePipelineDelegatingHandler
        var innerHandler = new Mock<DelegatingHandler>();
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(fakeResponse);
        _sut.InnerHandler = innerHandler.Object;

        // setup the resilience pipeline eg: retry policy
        var resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<Exception>(),
            })
            .Build();

        _pollyQoSResiliencePipelineProviderMock
            .Setup(x => x.GetResiliencePipeline(It.IsAny<DownstreamRoute>()))
            .Returns(resiliencePipeline);

        var httpContext = new Mock<HttpContext>();
        httpContext
            .Setup(x => x.RequestServices.GetService(typeof(IPollyQoSResiliencePipelineProvider<HttpResponseMessage>)))
            .Returns(_pollyQoSResiliencePipelineProviderMock.Object);

        _contextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext.Object);

        // Act
        var actual = await InvokeAsync("SendAsync");

        // Assert
        ShouldHaveXunitHeaderWithNoContent(actual, nameof(SendAsync_OnePolicy));

        // TODO: do more checks
        // check that the pipeline provider was called only once
        _pollyQoSResiliencePipelineProviderMock
            .Verify(a => a.GetResiliencePipeline(It.IsAny<DownstreamRoute>()), times: Times.Once);
        _pollyQoSResiliencePipelineProviderMock
            .VerifyNoOtherCalls();

        // this check has no sense anymore
        //method.DeclaringType.Name.ShouldBe("IAsyncPolicy`1");
        //method.DeclaringType.ShouldNotBeOfType<AsyncPolicyWrap>();
    }

    private async Task<HttpResponseMessage> InvokeAsync(string methodName)
    {
        var m = typeof(PollyResiliencePipelineDelegatingHandler).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        var task = (Task<HttpResponseMessage>)m.Invoke(_sut, new object[] { new HttpRequestMessage(), CancellationToken.None });
        var actual = await task!;
        return actual;
    }

    private static void ShouldHaveXunitHeaderWithNoContent(HttpResponseMessage actual, string headerName)
    {
        actual.ShouldNotBeNull();
        actual.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        actual.Headers.GetValues("X-Xunit").ShouldContain(headerName);
    }

    private static DownstreamRoute DownstreamRouteFactory()
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(100)
            .WithExceptionsAllowedBeforeBreaking(2)
            .WithDurationOfBreak(200)
            .Build();

        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate("/")
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue("/").Build();

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath).Build();

        return route;
    }
}
