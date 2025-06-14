using KubeClient;
using KubeClient.Http;
using KubeClient.Http.Formatters;
using KubeClient.Models;
using Microsoft.Extensions.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Kubernetes.Interfaces;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Kubernetes;

[Trait("Feat", "2168")]
[Trait("PR", "2174")] // https://github.com/ThreeMammals/Ocelot/pull/2174
public class EndpointClientV1Tests
{
    private readonly EndPointClientV1 _endpointClient;
    private readonly Mock<IKubeApiClient> _kubeApiClient = new();

    public EndpointClientV1Tests()
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        _kubeApiClient.Setup(x => x.LoggerFactory)
            .Returns(loggerFactory.Object);
        _endpointClient = new EndPointClientV1(_kubeApiClient.Object);
        _kubeApiClient.Setup(x => x.ResourceClient(It.IsAny<Func<IKubeApiClient, IEndPointClient>>()))
            .Returns(_endpointClient);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetAsync_WhenServiceIsNullOrEmpty_ThrowsArgumentException(string serviceName)
    {
        // Act
        var watchCall = () => _endpointClient.GetAsync(serviceName, null, CancellationToken.None);

        // Assert
        var e = await watchCall.ShouldThrowAsync<ArgumentException>();
        e.ParamName.ShouldBe(nameof(serviceName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("test-namespace")]
    public async Task GetAsync_KubeNamespaceChanges_HappyPath(string kubeNamespace)
    {
        // Arrange
        using var client = new FakeHttpClient()
        {
            BaseAddress = new UriBuilder(Uri.UriSchemeHttp, "localhost", 1234).Uri,
        };
        _kubeApiClient.SetupGet(x => x.Http).Returns(client);
        _kubeApiClient.SetupGet(x => x.DefaultNamespace).Returns(nameof(EndpointClientV1Tests));

        // Act
        var endpoints = await _endpointClient.GetAsync("service-XYZ", kubeNamespace, CancellationToken.None);

        // Assert
        Assert.NotNull(endpoints);
        Assert.Equal(nameof(GetAsync_KubeNamespaceChanges_HappyPath), endpoints.Kind);
        var url = client.Request.RequestUri.AbsoluteUri;
        Assert.Contains(kubeNamespace ?? nameof(EndpointClientV1Tests), url);
        Assert.Contains("service-XYZ", url);
        client.Request.Options.TryGetValue(new("KubeClient.Http.Request"), out HttpRequest request);
        Assert.NotNull(request?.TemplateParameters);
        Assert.True(request.TemplateParameters.ContainsKey("Namespace"));
        Assert.True(request.TemplateParameters.ContainsKey("ServiceName"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Watch_WhenServiceIsNullOrEmpty_ThrowsArgumentException(string serviceName)
    {
        // Act
        var watchCall = () => _endpointClient.Watch(serviceName, null, CancellationToken.None);
        
        // Assert
        watchCall.ShouldThrow<ArgumentException>().ParamName.ShouldBe(nameof(serviceName));
    }

    [Fact]
    public void Watch_ProvidesObservable()
    {
        // Act
        var observable = _endpointClient.Watch("some-service", null, CancellationToken.None);
        
        // Assert
        observable.ShouldNotBeNull();
    }
}

internal class FakeHttpClient : HttpClient, IDisposable
{
    private readonly Mock<IFormatterCollection> formatters = new();
    private readonly Mock<IInputFormatter> formatter = new();
    private readonly List<IDisposable> disposables = new();
    public FakeHttpClient([CallerMemberName] string testName = null)
    {
        formatter.Setup(x => x.ReadAsync(It.IsAny<InputFormatterContext>(), It.IsAny<Stream>()))
            .ReturnsAsync(() => new EndpointsV1() { Kind = testName });
        formatters.SetupGet(x => x.Count).Returns(1);
        formatters.Setup(x => x.FindInputFormatter(It.IsAny<InputFormatterContext>()))
            .Returns(formatter.Object);
    }

    public new void Dispose()
    {
        disposables.ForEach(d => d.Dispose());
        base.Dispose();
    }

    public HttpRequestMessage Request { get; private set; }
    public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        HttpResponseMessage response = new()
        {
            StatusCode = HttpStatusCode.OK,
            RequestMessage = new(),
        };
        response.RequestMessage.Properties.Add(MessageProperties.ContentFormatters, formatters.Object);
        response.Content = new StringContent("Hello from " + nameof(FakeHttpClient));
        disposables.Add(response);
        disposables.Add(response.Content);
        return Task.FromResult(response);
    }
}
