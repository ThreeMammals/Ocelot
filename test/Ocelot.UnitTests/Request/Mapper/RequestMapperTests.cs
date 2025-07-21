using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Request.Mapper;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Net.Http.Headers;

namespace Ocelot.UnitTests.Request.Mapper;

public class RequestMapperTests : UnitTest
{
    private readonly HttpRequest _inputRequest;
    private readonly RequestMapper _requestMapper;
    private HttpRequestMessage _mappedRequest;
    private List<KeyValuePair<string, StringValues>> _inputHeaders;
    private DownstreamRoute _downstreamRoute;

    public RequestMapperTests()
    {
        var httpContext = new DefaultHttpContext();
        _inputRequest = httpContext.Request;
        _requestMapper = new RequestMapper();
    }

    [Theory]
    [InlineData("https", "my.url:123", "/abc/DEF", "?a=1&b=2", "https://my.url:123/abc/DEF?a=1&b=2")]
    [InlineData("http", "blah.com", "/d ef", "?abc=123",
        "http://blah.com/d%20ef?abc=123")] // note! the input is encoded when building the input request
    [InlineData("http", "myusername:mypassword@abc.co.uk", null, null, "http://myusername:mypassword@abc.co.uk/")]
    [InlineData("http", "點看.com", null, null, "http://xn--c1yn36f.com/")]
    [InlineData("http", "xn--c1yn36f.com", null, null, "http://xn--c1yn36f.com/")]
    public void Should_map_valid_request_uri(string scheme, string host, string path, string queryString, string expectedUri)
    {
        // Arrange
        _inputRequest.Method = "GET";
        _inputRequest.Scheme = scheme;
        GivenTheInputRequestHasHost(host);
        GivenTheInputRequestHasPath(path);
        GivenTheInputRequestHasQueryString(queryString);
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        Assert.NotNull(_mappedRequest.RequestUri);
        _mappedRequest.RequestUri.OriginalString.ShouldBe(expectedUri);
    }

    [Theory]
    [InlineData("ftp", "google.com", "/abc/DEF", "?a=1&b=2")]
    public void Should_error_on_unsupported_request_uri(string scheme, string host, string path, string queryString)
    {
        // Arrange
        _inputRequest.Method = "GET";
        _inputRequest.Scheme = scheme;
        GivenTheInputRequestHasHost(host);
        GivenTheInputRequestHasPath(path);
        GivenTheInputRequestHasQueryString(queryString);

        // Act, Assert
        Assert.Throws<NullReferenceException>(() => _requestMapper.Map(_inputRequest, _downstreamRoute));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("WHATEVER")]
    public void Should_map_method(string method)
    {
        // Arrange
        _inputRequest.Method = method;
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        _mappedRequest.Method.ToString().ShouldBe(method);
    }

    [Theory]
    [InlineData("", "GET")]
    [InlineData(null, "GET")]
    [InlineData("POST", "POST")]
    public void Should_use_downstream_route_method_if_set(string input, string expected)
    {
        // Arrange
        _inputRequest.Method = "GET";
        _downstreamRoute = new DownstreamRouteBuilder()
            .WithDownStreamHttpMethod(input)
            .WithDownstreamHttpVersion(new Version("1.1"))
            .Build();
        GivenTheInputRequestHasAValidUri();

        // Act
        WhenMapped();

        // Assert
        _mappedRequest.Method.ToString().ShouldBe(expected);
    }

    [Fact]
    public void Should_map_all_headers()
    {
        // Arrange: Given The Input Request Has Headers
        var abcVals = new[] { "123", "456" };
        var defVals = new[] { "789", "012" };
        _inputHeaders = new()
        {
            new("abc", new StringValues(abcVals)),
            new("def", new StringValues(defVals)),
        };

        foreach (var inputHeader in _inputHeaders)
        {
            _inputRequest.Headers.Add(inputHeader);
        }

        _inputRequest.Method = "GET";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert: Then The Mapped Request Has Each Header
        _mappedRequest.Headers.Count().ShouldBe(_inputHeaders.Count);
        foreach (var header in _mappedRequest.Headers)
        {
            var inputHeader = _inputHeaders.First(h => h.Key == header.Key);
            inputHeader.ShouldNotBe(default);
            inputHeader.Value.Count.ShouldBe(header.Value.Count());
            foreach (var inputHeaderValue in inputHeader.Value)
            {
                Assert.Contains(header.Value, v => v == inputHeaderValue);
            }
        }
    }

    [Fact]
    public void Should_handle_no_headers()
    {
        // Arrange
        _inputRequest.Headers.Clear();
        _inputRequest.Method = "GET";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        _mappedRequest.Headers.Count().ShouldBe(0);
    }

    [Theory]
    [Trait("PR", "1972")]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Should_map_content(string method)
    {
        // Arrange
        GivenTheInputRequestHasContent("This is my content");
        _inputRequest.Method = method;
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        await ThenTheMappedRequestHasContent("This is my content");
        ThenTheMappedRequestHasContentLength("This is my content".Length);
    }

    [Fact]
    [Trait("PR", "1972")]
    public async Task Should_map_chucked_content()
    {
        // Arrange
        GivenTheInputRequestHasChunkedContent("This", " is my content");
        _inputRequest.Method = "POST";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        await ThenTheMappedRequestHasContent("This is my content");
        _mappedRequest.Headers.TryGetValues(HeaderNames.ContentLength, out _).ShouldBeFalse(); // ThenTheMappedRequestHasNoContentLength
    }

    [Fact]
    [Trait("PR", "1972")]
    public async Task Should_map_empty_content()
    {
        // Arrange
        GivenTheInputRequestHasContent("");
        _inputRequest.Method = "POST";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        await ThenTheMappedRequestHasContent("");
        ThenTheMappedRequestHasContentLength(0);
    }

    [Fact]
    [Trait("PR", "1972")]
    public async Task Should_map_empty_chucked_content()
    {
        // Arrange
        GivenTheInputRequestHasChunkedContent();
        _inputRequest.Method = "POST";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        await ThenTheMappedRequestHasContent("");
        _mappedRequest.Headers.TryGetValues(HeaderNames.ContentLength, out _).ShouldBeFalse(); // ThenTheMappedRequestHasNoContentLength
    }

    [Fact]
    public void Should_handle_no_content()
    {
        // Arrange
        _inputRequest.Body = null!;
        _inputRequest.Method = "GET";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        _mappedRequest.Content.ShouldBeNull();
    }

    [Fact]
    public void Should_handle_no_content_type()
    {
        // Arrange
        _inputRequest.ContentType = null;
        _inputRequest.Method = "GET";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        _mappedRequest.Content.ShouldBeNull();
    }

    [Fact]
    public void Should_handle_no_content_length()
    {
        // Arrange
        _inputRequest.ContentLength = null;
        _inputRequest.Method = "GET";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        _mappedRequest.Content.ShouldBeNull();
    }

    [Fact]
    public void Should_map_content_headers()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("some md5");
        var md5Bytes = MD5.HashData(bytes);

        GivenTheInputRequestHasContent("This is my content");
        _inputRequest.ContentType = "application/json";
        _inputRequest.Headers.Append("Content-Encoding", "gzip, compress");
        _inputRequest.Headers.Append("Content-Language", "english");
        _inputRequest.Headers.Append("Content-Location", "/my-receipts/38");
        _inputRequest.Headers.Append("Content-Range", "bytes 1-2/*");
        _inputRequest.Headers.Append("Content-Disposition", "inline");
        var base64 = Convert.ToBase64String(md5Bytes);
        _inputRequest.Headers.Append("Content-MD5", base64);
        _inputRequest.Method = "GET";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        ThenTheMappedRequestHasContentTypeHeader("application/json");
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentEncoding.ToArray()[0].ShouldBe("gzip");
        _mappedRequest.Content.Headers.ContentEncoding.ToArray()[1].ShouldBe("compress");
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentLanguage.First().ShouldBe("english");
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentLocation);
        _mappedRequest.Content.Headers.ContentLocation.OriginalString.ShouldBe("/my-receipts/38");
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentMD5.ShouldBe(md5Bytes);
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentRange);
        _mappedRequest.Content.Headers.ContentRange.From.ShouldBe(1);
        _mappedRequest.Content.Headers.ContentRange.To.ShouldBe(2);
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentDisposition);
        _mappedRequest.Content.Headers.ContentDisposition.DispositionType.ShouldBe("inline");

        // Assert: Then The Content-* Headers Are Not Added To Non Content Headers
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-Disposition");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentMD5");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentRange");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentLanguage");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentEncoding");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentLocation");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-Length");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-Type");
    }

    [Fact]
    public void Should_not_add_content_headers()
    {
        // Arrange
        GivenTheInputRequestHasContent("This is my content");
        _inputRequest.ContentType = "application/json";
        _inputRequest.Method = "POST";
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();

        // Act
        WhenMapped();

        // Assert
        ThenTheMappedRequestHasContentTypeHeader("application/json");

        // Assert: Then The Other Content Type Headers Are Not Mapped
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentDisposition.ShouldBeNull();
        _mappedRequest.Content.Headers.ContentMD5.ShouldBeNull();
        _mappedRequest.Content.Headers.ContentRange.ShouldBeNull();
        _mappedRequest.Content.Headers.ContentLanguage.ShouldBeEmpty();
        _mappedRequest.Content.Headers.ContentEncoding.ShouldBeEmpty();
        _mappedRequest.Content.Headers.ContentLocation.ShouldBeNull();
    }

    private void GivenTheDownstreamRoute()
    {
        _downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamHttpVersion(new Version("1.1")).Build();
    }

    private void ThenTheMappedRequestHasContentTypeHeader(string expected)
    {
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentType);
        _mappedRequest.Content.Headers.ContentType.MediaType.ShouldBe(expected);
    }

    private void GivenTheInputRequestHasHost(string host)
    {
        _inputRequest.Host = new HostString(host);
    }

    private void GivenTheInputRequestHasPath(string path)
    {
        if (path != null)
        {
            _inputRequest.Path = path;
        }
    }

    private void GivenTheInputRequestHasQueryString(string querystring)
    {
        if (querystring != null)
        {
            _inputRequest.QueryString = new QueryString(querystring);
        }
    }

    private void GivenTheInputRequestHasAValidUri()
    {
        _inputRequest.Scheme = "http";
        GivenTheInputRequestHasHost("www.google.com");
    }

    private void GivenTheInputRequestHasContent(string content)
    {
        _inputRequest.ContentLength = content.Length;
        _inputRequest.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    private void GivenTheInputRequestHasChunkedContent(params string[] chunks)
    {
        // ASP.Net Core decodes chucked streams, so that the input request just sees the decoded data
        // Because of that, we just give a stream with the concatenated chunks to the test
        _inputRequest.Body = new MemoryStream(Encoding.UTF8.GetBytes(string.Join("", chunks)));
        _inputRequest.Headers.TransferEncoding = "chunked";
    }

    private void WhenMapped()
    {
        _mappedRequest = _requestMapper.Map(_inputRequest, _downstreamRoute);
    }

    private async Task ThenTheMappedRequestHasContent(string expectedContent)
    {
        Assert.NotNull(_mappedRequest.Content);
        var contentAsString = await _mappedRequest.Content.ReadAsStringAsync();
        contentAsString.ShouldBe(expectedContent);
    }

    private void ThenTheMappedRequestHasContentLength(long expectedLength)
    {
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentLength.ShouldBe(expectedLength);
    }
}
