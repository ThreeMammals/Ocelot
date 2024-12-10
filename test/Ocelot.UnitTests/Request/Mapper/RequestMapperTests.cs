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
        HttpContext httpContext = new DefaultHttpContext();
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
    public void Should_map_valid_request_uri(string scheme, string host, string path, string queryString,
        string expectedUri)
    {
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasScheme(scheme);
        GivenTheInputRequestHasHost(host);
        GivenTheInputRequestHasPath(path);
        GivenTheInputRequestHasQueryString(queryString);
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasUri(expectedUri);
    }

    [Theory]
    [InlineData("ftp", "google.com", "/abc/DEF", "?a=1&b=2")]
    public void Should_error_on_unsupported_request_uri(string scheme, string host, string path, string queryString)
    {
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasScheme(scheme);
        GivenTheInputRequestHasHost(host);
        GivenTheInputRequestHasPath(path);
        GivenTheInputRequestHasQueryString(queryString);
        ThenMapThrowsException();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("WHATEVER")]
    public void Should_map_method(string method)
    {
        GivenTheInputRequestHasMethod(method);
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasMethod(method);
    }

    [Theory]
    [InlineData("", "GET")]
    [InlineData(null, "GET")]
    [InlineData("POST", "POST")]
    public void Should_use_downstream_route_method_if_set(string input, string expected)
    {
        GivenTheInputRequestHasMethod("GET");
        GivenTheDownstreamRouteMethodIs(input);
        GivenTheInputRequestHasAValidUri();
        WhenMapped();
        ThenTheMappedRequestHasMethod(expected);
    }

    [Fact]
    public void Should_map_all_headers()
    {
        GivenTheInputRequestHasHeaders();
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasEachHeader();
    }

    [Fact]
    public void Should_handle_no_headers()
    {
        GivenTheInputRequestHasNoHeaders();
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasNoHeaders();
    }

    [Theory]
    [Trait("PR", "1972")]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Should_map_content(string method)
    {
        GivenTheInputRequestHasContent("This is my content");
        GivenTheInputRequestHasMethod(method);
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        await ThenTheMappedRequestHasContent("This is my content");
        ThenTheMappedRequestHasContentLength("This is my content".Length);
    }

    [Fact]
    [Trait("PR", "1972")]
    public async Task Should_map_chucked_content()
    {
        GivenTheInputRequestHasChunkedContent("This", " is my content");
        GivenTheInputRequestHasMethod("POST");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        await ThenTheMappedRequestHasContent("This is my content");
        ThenTheMappedRequestHasNoContentLength();
    }

    [Fact]
    [Trait("PR", "1972")]
    public async Task Should_map_empty_content()
    {
        GivenTheInputRequestHasContent("");
        GivenTheInputRequestHasMethod("POST");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        await ThenTheMappedRequestHasContent("");
        ThenTheMappedRequestHasContentLength(0);
    }

    [Fact]
    [Trait("PR", "1972")]
    public async Task Should_map_empty_chucked_content()
    {
        GivenTheInputRequestHasChunkedContent();
        GivenTheInputRequestHasMethod("POST");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        await ThenTheMappedRequestHasContent("");
        ThenTheMappedRequestHasNoContentLength();
    }

    [Fact]
    public void Should_handle_no_content()
    {
        GivenTheInputRequestHasNullContent();
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasNoContent();
    }

    [Fact]
    public void Should_handle_no_content_type()
    {
        GivenTheInputRequestHasNoContentType();
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasNoContent();
    }

    [Fact]
    public void Should_handle_no_content_length()
    {
        GivenTheInputRequestHasNoContentLength();
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasNoContent();
    }

    [Fact]
    public void Should_map_content_headers()
    {
        var bytes = Encoding.UTF8.GetBytes("some md5");
        var md5Bytes = MD5.HashData(bytes);

        GivenTheInputRequestHasContent("This is my content");
        GivenTheContentTypeIs("application/json");
        GivenTheContentEncodingIs("gzip, compress");
        GivenTheContentLanguageIs("english");
        GivenTheContentLocationIs("/my-receipts/38");
        GivenTheContentRangeIs("bytes 1-2/*");
        GivenTheContentDispositionIs("inline");
        GivenTheContentMD5Is(md5Bytes);
        GivenTheInputRequestHasMethod("GET");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasContentTypeHeader("application/json");
        ThenTheMappedRequestHasContentEncodingHeader("gzip", "compress");
        ThenTheMappedRequestHasContentLanguageHeader("english");
        ThenTheMappedRequestHasContentLocationHeader("/my-receipts/38");
        ThenTheMappedRequestHasContentMD5Header(md5Bytes);
        ThenTheMappedRequestHasContentRangeHeader();
        ThenTheMappedRequestHasContentDispositionHeader("inline");
        ThenTheContentHeadersAreNotAddedToNonContentHeaders();
    }

    [Fact]
    public void Should_not_add_content_headers()
    {
        GivenTheInputRequestHasContent("This is my content");
        GivenTheContentTypeIs("application/json");
        GivenTheInputRequestHasMethod("POST");
        GivenTheInputRequestHasAValidUri();
        GivenTheDownstreamRoute();
        WhenMapped();
        ThenTheMappedRequestHasContentTypeHeader("application/json");
        ThenTheOtherContentTypeHeadersAreNotMapped();
    }

    private void GivenTheDownstreamRouteMethodIs(string input)
    {
        _downstreamRoute = new DownstreamRouteBuilder()
            .WithDownStreamHttpMethod(input)
            .WithDownstreamHttpVersion(new Version("1.1")).Build();
    }

    private void GivenTheDownstreamRoute()
    {
        _downstreamRoute = new DownstreamRouteBuilder()
            .WithDownstreamHttpVersion(new Version("1.1")).Build();
    }

    private void GivenTheInputRequestHasNoContentLength()
    {
        _inputRequest.ContentLength = null;
    }

    private void GivenTheInputRequestHasNoContentType()
    {
        _inputRequest.ContentType = null;
    }

    private void ThenTheContentHeadersAreNotAddedToNonContentHeaders()
    {
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-Disposition");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentMD5");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentRange");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentLanguage");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentEncoding");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-ContentLocation");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-Length");
        _mappedRequest.Headers.ShouldNotContain(x => x.Key == "Content-Type");
    }

    private void ThenTheOtherContentTypeHeadersAreNotMapped()
    {
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentDisposition.ShouldBeNull();
        _mappedRequest.Content.Headers.ContentMD5.ShouldBeNull();
        _mappedRequest.Content.Headers.ContentRange.ShouldBeNull();
        _mappedRequest.Content.Headers.ContentLanguage.ShouldBeEmpty();
        _mappedRequest.Content.Headers.ContentEncoding.ShouldBeEmpty();
        _mappedRequest.Content.Headers.ContentLocation.ShouldBeNull();
    }

    private void ThenTheMappedRequestHasContentDispositionHeader(string expected)
    {
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentDisposition);
        _mappedRequest.Content.Headers.ContentDisposition.DispositionType.ShouldBe(expected);
    }

    private void GivenTheContentDispositionIs(string input)
    {
        _inputRequest.Headers.Append("Content-Disposition", input);
    }

    private void ThenTheMappedRequestHasContentMD5Header(byte[] expected)
    {
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentMD5.ShouldBe(expected);
    }

    private void GivenTheContentMD5Is(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        _inputRequest.Headers.Append("Content-MD5", base64);
    }

    private void ThenTheMappedRequestHasContentRangeHeader()
    {
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentRange);
        _mappedRequest.Content.Headers.ContentRange.From.ShouldBe(1);
        _mappedRequest.Content.Headers.ContentRange.To.ShouldBe(2);
    }

    private void GivenTheContentRangeIs(string input)
    {
        _inputRequest.Headers.Append("Content-Range", input);
    }

    private void ThenTheMappedRequestHasContentLocationHeader(string expected)
    {
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentLocation);
        _mappedRequest.Content.Headers.ContentLocation.OriginalString.ShouldBe(expected);
    }

    private void GivenTheContentLocationIs(string input)
    {
        _inputRequest.Headers.Append("Content-Location", input);
    }

    private void ThenTheMappedRequestHasContentLanguageHeader(string expected)
    {
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentLanguage.First().ShouldBe(expected);
    }

    private void GivenTheContentLanguageIs(string input)
    {
        _inputRequest.Headers.Append("Content-Language", input);
    }

    private void ThenTheMappedRequestHasContentEncodingHeader(string expected, string expectedTwo)
    {
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentEncoding.ToArray()[0].ShouldBe(expected);
        _mappedRequest.Content.Headers.ContentEncoding.ToArray()[1].ShouldBe(expectedTwo);
    }

    private void GivenTheContentEncodingIs(string input)
    {
        _inputRequest.Headers.Append("Content-Encoding", input);
    }

    private void GivenTheContentTypeIs(string contentType)
    {
        _inputRequest.ContentType = contentType;
    }

    private void ThenTheMappedRequestHasContentTypeHeader(string expected)
    {
        Assert.NotNull(_mappedRequest.Content);
        Assert.NotNull(_mappedRequest.Content.Headers.ContentType);
        _mappedRequest.Content.Headers.ContentType.MediaType.ShouldBe(expected);
    }

    private void ThenTheMappedRequestHasContentSize(long expected)
    {
        Assert.NotNull(_mappedRequest.Content);
        _mappedRequest.Content.Headers.ContentLength.ShouldBe(expected);
    }

    private void GivenTheInputRequestHasMethod(string method)
    {
        _inputRequest.Method = method;
    }

    private void GivenTheInputRequestHasScheme(string scheme)
    {
        _inputRequest.Scheme = scheme;
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
        GivenTheInputRequestHasScheme("http");
        GivenTheInputRequestHasHost("www.google.com");
    }

    private void GivenTheInputRequestHasHeaders()
    {
        _inputHeaders = new()
        {
            new("abc", new StringValues(new string[] { "123", "456" })),
            new("def", new StringValues(new string[] { "789", "012" })),
        };

        foreach (var inputHeader in _inputHeaders)
        {
            _inputRequest.Headers.Add(inputHeader);
        }
    }

    private void GivenTheInputRequestHasNoHeaders()
    {
        _inputRequest.Headers.Clear();
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

    private void GivenTheInputRequestHasNullContent()
    {
        _inputRequest.Body = null!;
    }

    private void WhenMapped()
    {
        _mappedRequest = _requestMapper.Map(_inputRequest, _downstreamRoute);
    }

    private void ThenMapThrowsException()
    {
        Assert.Throws<NullReferenceException>(() => _requestMapper.Map(_inputRequest, _downstreamRoute));
    }

    private void ThenTheMappedRequestHasUri(string expectedUri)
    {
        Assert.NotNull(_mappedRequest.RequestUri);
        _mappedRequest.RequestUri.OriginalString.ShouldBe(expectedUri);
    }

    private void ThenTheMappedRequestHasMethod(string expectedMethod)
    {
        _mappedRequest.Method.ToString().ShouldBe(expectedMethod);
    }

    private void ThenTheMappedRequestHasEachHeader()
    {
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

    private void ThenTheMappedRequestHasNoHeaders()
    {
        _mappedRequest.Headers.Count().ShouldBe(0);
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

    private void ThenTheMappedRequestHasNoContentLength()
    {
        _mappedRequest.Headers.TryGetValues(HeaderNames.ContentLength, out _).ShouldBeFalse();
    }

    private void ThenTheMappedRequestHasNoContent()
    {
        _mappedRequest.Content.ShouldBeNull();
    }

    private void ThenTheMappedRequestIsNull()
    {
        _mappedRequest.ShouldBeNull();
    }
}
