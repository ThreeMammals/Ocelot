using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Infrastructure;

public class PlaceholdersTests
{
    private readonly Placeholders _placeholders;
    private readonly Mock<IBaseUrlFinder> _finder;
    private readonly Mock<IRequestScopedDataRepository> _repo;
    private readonly Mock<IHttpContextAccessor> _accessor;

    public PlaceholdersTests()
    {
        _accessor = new Mock<IHttpContextAccessor>();
        _repo = new Mock<IRequestScopedDataRepository>();
        _finder = new Mock<IBaseUrlFinder>();
        _placeholders = new Placeholders(_finder.Object, _repo.Object, _accessor.Object);
    }

    [Fact]
    public void Should_return_base_url()
    {
        // Arrange
        var baseUrl = "http://www.bbc.co.uk";
        _finder.Setup(x => x.Find()).Returns(baseUrl);

        // Act
        var result = _placeholders.Get("{BaseUrl}");

        // Assert
        result.Data.ShouldBe(baseUrl);
    }

    [Fact]
    public void Should_return_remote_ip_address()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Any } };
        _accessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _placeholders.Get("{RemoteIpAddress}");

        // Assert
        result.Data.ShouldBe(httpContext.Connection.RemoteIpAddress.ToString());
    }

    [Fact]
    public void Should_return_key_does_not_exist()
    {
        // Arrange, Act
        var result = _placeholders.Get("{Test}");

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].Message.ShouldBe("Unable to find placeholder called {Test}");
    }

    [Fact]
    public void Should_return_downstream_base_url_when_port_is_not_80_or_443()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage();
        httpRequest.RequestUri = new Uri("http://www.bbc.co.uk");
        var request = new DownstreamRequest(httpRequest);

        // Act
        var result = _placeholders.Get("{DownstreamBaseUrl}", request);

        // Assert
        result.Data.ShouldBe("http://www.bbc.co.uk/");
    }

    [Fact]
    public void Should_return_downstream_base_url_when_port_is_80_or_443()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage();
        httpRequest.RequestUri = new Uri("http://www.bbc.co.uk:123");
        var request = new DownstreamRequest(httpRequest);

        // Act
        var result = _placeholders.Get("{DownstreamBaseUrl}", request);

        // Assert
        result.Data.ShouldBe("http://www.bbc.co.uk:123/");
    }

    [Fact]
    public void Should_return_key_does_not_exist_for_http_request_message()
    {
        // Arrange
        var request = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://west.com"));

        // Act
        var result = _placeholders.Get("{Test}", request);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].Message.ShouldBe("Unable to find placeholder called {Test}");
    }

    [Fact]
    public void Should_return_trace_id()
    {
        // Arrange
        var traceId = "123";
        _repo.Setup(x => x.Get<string>("TraceId")).Returns(new OkResponse<string>(traceId));

        // Act
        var result = _placeholders.Get("{TraceId}");

        // Assert
        result.Data.ShouldBe(traceId);
    }

    [Fact]
    public void Should_return_ok_when_added()
    {
        // Arrange, Act
        var result = _placeholders.Add("{Test}", () => new OkResponse<string>("test"));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void Should_return_ok_when_removed()
    {
        // Arrange
        var result = _placeholders.Add("{Test}", () => new OkResponse<string>("test"));

        // Act
        result = _placeholders.Remove("{Test}");

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void Should_return_error_when_added()
    {
        // Arrange
        var result = _placeholders.Add("{Test}", () => new OkResponse<string>("test"));

        // Act
        result = _placeholders.Add("{Test}", () => new OkResponse<string>("test"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].Message.ShouldBe("Unable to add placeholder: {Test}, placeholder already exists");
    }

    [Fact]
    public void Should_return_error_when_removed()
    {
        // Arrange, Act
        var result = _placeholders.Remove("{Test}");

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors[0].Message.ShouldBe("Unable to remove placeholder: {Test}, placeholder does not exists");
    }

    [Fact]
    public void Should_return_upstreamHost()
    {
        // Arrange
        var upstreamHost = "UpstreamHostA";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Host", upstreamHost);
        _accessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _placeholders.Get("{UpstreamHost}");

        // Assert
        result.Data.ShouldBe(upstreamHost);
    }

    [Fact]
    public void Should_return_error_when_finding_upstbecause_Host_not_set()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _accessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _placeholders.Get("{UpstreamHost}");

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public void Should_return_error_when_finding_upstream_host_because_exception_thrown()
    {
        // Arrange
        _accessor.Setup(x => x.HttpContext).Throws(new Exception());

        // Act
        var result = _placeholders.Get("{UpstreamHost}");

        // Assert
        result.IsError.ShouldBeTrue();
    }
}
