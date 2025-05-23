using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Headers;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Headers;

public class HttpResponseHeaderReplacerTests : UnitTest
{
    private DownstreamResponse _response;
    private readonly Placeholders _placeholders;
    private readonly HttpResponseHeaderReplacer _replacer;
    private List<HeaderFindAndReplace> _headerFindAndReplaces;
    private Response _result;
    private DownstreamRequest _request;
    private readonly Mock<IBaseUrlFinder> _finder;
    private readonly Mock<IRequestScopedDataRepository> _repo;
    private readonly Mock<IHttpContextAccessor> _accessor;
    /*private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;*/

    public HttpResponseHeaderReplacerTests()
    {
        _repo = new Mock<IRequestScopedDataRepository>();
        _finder = new Mock<IBaseUrlFinder>();
        _accessor = new Mock<IHttpContextAccessor>();

        //_loggerFactory = new Mock<IOcelotLoggerFactory>();
        _placeholders = new Placeholders(_finder.Object, _repo.Object, _accessor.Object/*,_loggerFactory.Object*/);
        _replacer = new HttpResponseHeaderReplacer(_placeholders);
    }

    [Fact]
    public void Should_replace_headers()
    {
        // Arrange
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("test", new List<string> {"test"}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace> { new("test", "test", "chiken", 0) };

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeadersAreReplaced();
    }

    [Fact]
    public void Should_not_replace_headers()
    {
        // Arrange
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("test", new List<string> {"test"}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace>();

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeadersAreNotReplaced();
    }

    [Fact]
    public void Should_replace_downstream_base_url_with_ocelot_base_url()
    {
        // Arrange
        const string downstreamUrl = "http://downstream.com/";
        _request = new DownstreamRequest(new(HttpMethod.Get, "http://test.com") { RequestUri = new Uri(downstreamUrl) });
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("Location", new List<string> {downstreamUrl}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace>
        {
            new("Location", "{DownstreamBaseUrl}", "http://ocelot.com/", 0),
        };

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeaderShouldBe("Location", "http://ocelot.com/");
    }

    [Fact]
    public void Should_replace_downstream_base_url_with_ocelot_base_url_with_port()
    {
        // Arrange
        const string downstreamUrl = "http://downstream.com/";
        _request = new DownstreamRequest(new(HttpMethod.Get, "http://test.com") { RequestUri = new Uri(downstreamUrl) });
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("Location", new List<string> {downstreamUrl}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace>
        {
            new("Location", "{DownstreamBaseUrl}", "http://ocelot.com:123/", 0),
        };

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeaderShouldBe("Location", "http://ocelot.com:123/");
    }

    [Fact]
    public void Should_replace_downstream_base_url_with_ocelot_base_url_and_path()
    {
        // Arrange
        const string downstreamUrl = "http://downstream.com/test/product";
        _request = new DownstreamRequest(new(HttpMethod.Get, "http://test.com") { RequestUri = new Uri(downstreamUrl) });
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("Location", new List<string> {downstreamUrl}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace>
        {
            new("Location", "{DownstreamBaseUrl}", "http://ocelot.com/", 0),
        };

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeaderShouldBe("Location", "http://ocelot.com/test/product");
    }

    [Fact]
    public void Should_replace_downstream_base_url_with_ocelot_base_url_with_path_and_port()
    {
        // Arrange
        const string downstreamUrl = "http://downstream.com/test/product";
        _request = new DownstreamRequest(new(HttpMethod.Get, "http://test.com") { RequestUri = new Uri(downstreamUrl) });
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("Location", new List<string> {downstreamUrl}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace>
        {
            new("Location", "{DownstreamBaseUrl}", "http://ocelot.com:123/", 0),
        };

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeaderShouldBe("Location", "http://ocelot.com:123/test/product");
    }

    [Fact]
    public void Should_replace_downstream_base_url_and_port_with_ocelot_base_url()
    {
        // Arrange
        const string downstreamUrl = "http://downstream.com:123/test/product";
        _request = new DownstreamRequest(new(HttpMethod.Get, "http://test.com") { RequestUri = new Uri(downstreamUrl) });
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("Location", new List<string> {downstreamUrl}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace>
        {
            new("Location", "{DownstreamBaseUrl}", "http://ocelot.com/", 0),
        };

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeaderShouldBe("Location", "http://ocelot.com/test/product");
    }

    [Fact]
    public void Should_replace_downstream_base_url_and_port_with_ocelot_base_url_and_port()
    {
        // Arrange
        const string downstreamUrl = "http://downstream.com:123/test/product";
        _request = new DownstreamRequest(new(HttpMethod.Get, "http://test.com") { RequestUri = new Uri(downstreamUrl) });
        _response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.Accepted,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("Location", new List<string> {downstreamUrl}),
            }, string.Empty);
        _headerFindAndReplaces = new List<HeaderFindAndReplace>
        {
            new("Location", "{DownstreamBaseUrl}", "http://ocelot.com:321/", 0),
        };

        // Act
        WhenICallTheReplacer();

        // Assert
        ThenTheHeaderShouldBe("Location", "http://ocelot.com:321/test/product");    
    }

    private void ThenTheHeadersAreNotReplaced()
    {
        _result.ShouldBeOfType<OkResponse>();
        foreach (var f in _headerFindAndReplaces)
        {
            var values = _response.Headers.First(x => x.Key == f.Key);
            values.Values.ToList()[f.Index].ShouldBe("test");
        }
    }

    private void WhenICallTheReplacer()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items.UpsertDownstreamResponse(_response);
        httpContext.Items.UpsertDownstreamRequest(_request);

        _result = _replacer.Replace(httpContext, _headerFindAndReplaces);
    }

    private void ThenTheHeaderShouldBe(string key, string value)
    {
        var test = _response.Headers.First(x => x.Key == key);
        test.Values.First().ShouldBe(value);
    }

    private void ThenTheHeadersAreReplaced()
    {
        _result.ShouldBeOfType<OkResponse>();
        foreach (var f in _headerFindAndReplaces)
        {
            var values = _response.Headers.First(x => x.Key == f.Key);
            values.Values.ToList()[f.Index].ShouldBe(f.Replace);
        }
    }
}
