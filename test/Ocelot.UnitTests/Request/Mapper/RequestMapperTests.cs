namespace Ocelot.UnitTests.Request.Mapper
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Internal;
    using Microsoft.Extensions.Primitives;
    using Ocelot.Request.Mapper;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;
    using Shouldly;
    using System;
    using System.IO;
    using System.Text;

    public class RequestMapperTests
    {
        readonly HttpRequest _inputRequest;

        readonly RequestMapper _requestMapper;

        Response<HttpRequestMessage> _mappedRequest;

        List<KeyValuePair<string, StringValues>> _inputHeaders = null;

        public RequestMapperTests()
        {
            _inputRequest = new DefaultHttpRequest(new DefaultHttpContext());

            _requestMapper = new RequestMapper();
        }

        [Theory]
        [InlineData("https", "my.url:123", "/abc/DEF", "?a=1&b=2", "https://my.url:123/abc/DEF?a=1&b=2")]
        [InlineData("http", "blah.com", "/d ef", "?abc=123", "http://blah.com/d%20ef?abc=123")] // note! the input is encoded when building the input request
        [InlineData("http", "myusername:mypassword@abc.co.uk", null, null, "http://myusername:mypassword@abc.co.uk/")]
        [InlineData("http", "點看.com", null, null, "http://xn--c1yn36f.com/")]
        [InlineData("http", "xn--c1yn36f.com", null, null, "http://xn--c1yn36f.com/")]
        public void Should_map_valid_request_uri(string scheme, string host, string path, string queryString, string expectedUri)
        {
            this.Given(_ => GivenTheInputRequestHasMethod("GET"))
                .And(_ => GivenTheInputRequestHasScheme(scheme))
                .And(_ => GivenTheInputRequestHasHost(host))
                .And(_ => GivenTheInputRequestHasPath(path))
                .And(_ => GivenTheInputRequestHasQueryString(queryString))
                .When(_ => WhenMapped())
                .Then(_ => ThenNoErrorIsReturned())
                .And(_ => ThenTheMappedRequestHasUri(expectedUri))
                .BDDfy();
        }

        [Theory]
        [InlineData("ftp", "google.com", "/abc/DEF", "?a=1&b=2")]
        public void Should_error_on_unsupported_request_uri(string scheme, string host, string path, string queryString)
        {
            this.Given(_ => GivenTheInputRequestHasMethod("GET"))
                .And(_ => GivenTheInputRequestHasScheme(scheme))
                .And(_ => GivenTheInputRequestHasHost(host))
                .And(_ => GivenTheInputRequestHasPath(path))
                .And(_ => GivenTheInputRequestHasQueryString(queryString))
                .When(_ => WhenMapped())
                .Then(_ => ThenAnErrorIsReturned())
                .And(_ => ThenTheMappedRequestIsNull())
                .BDDfy();
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("WHATEVER")]
        public void Should_map_method(string method)
        {
            this.Given(_ => GivenTheInputRequestHasMethod(method))
                .And(_ => GivenTheInputRequestHasAValidUri())
                .When(_ => WhenMapped())
                .Then(_ => ThenNoErrorIsReturned())
                .And(_ => ThenTheMappedRequestHasMethod(method))
                .BDDfy();
        }

        [Fact]
        public void Should_map_all_headers()
        {
            this.Given(_ => GivenTheInputRequestHasHeaders())
                .And(_ => GivenTheInputRequestHasMethod("GET"))
                .And(_ => GivenTheInputRequestHasAValidUri())
                .When(_ => WhenMapped())
                .Then(_ => ThenNoErrorIsReturned())
                .And(_ => ThenTheMappedRequestHasEachHeader())
                .BDDfy();
        }

        [Fact]
        public void Should_handle_no_headers()
        {
            this.Given(_ => GivenTheInputRequestHasNoHeaders())
                .And(_ => GivenTheInputRequestHasMethod("GET"))
                .And(_ => GivenTheInputRequestHasAValidUri())
                .When(_ => WhenMapped())
                .Then(_ => ThenNoErrorIsReturned())
                .And(_ => ThenTheMappedRequestHasNoHeaders())
                .BDDfy();
        }

        [Fact]
        public void Should_map_content()
        {
            this.Given(_ => GivenTheInputRequestHasContent("This is my content"))
                .And(_ => GivenTheInputRequestHasMethod("GET"))
                .And(_ => GivenTheInputRequestHasAValidUri())
                .When(_ => WhenMapped())
                .Then(_ => ThenNoErrorIsReturned())
                .And(_ => ThenTheMappedRequestHasContent("This is my content"))
                .BDDfy();
        }

        [Fact]
        public void Should_map_content_type_header()
        {
            this.Given(_ => GivenTheInputRequestHasContent("This is my content"))
                .And(_ => GivenTheContentTypeIs("application/json"))
                .And(_ => GivenTheInputRequestHasMethod("GET"))
                .And(_ => GivenTheInputRequestHasAValidUri())
                .When(_ => WhenMapped())
                .Then(_ => ThenNoErrorIsReturned())
                .And(_ => ThenTheMappedRequestHasContentTypeHeader("application/json"))
                .And(_ => ThenTheMappedRequestHasContentSize("This is my content".Length))
                .BDDfy();
        }

        private void GivenTheContentTypeIs(string contentType)
        {
            _inputRequest.ContentType = contentType;
        }

        private void ThenTheMappedRequestHasContentTypeHeader(string expected)
        {
            _mappedRequest.Data.Content.Headers.ContentType.MediaType.ShouldBe(expected);
        }

        private void ThenTheMappedRequestHasContentSize(long expected)
        {
            _mappedRequest.Data.Content.Headers.ContentLength.ShouldBe(expected);
        }

        [Fact]
        public void Should_handle_no_content()
        {
            this.Given(_ => GivenTheInputRequestHasNoContent())
                .And(_ => GivenTheInputRequestHasMethod("GET"))
                .And(_ => GivenTheInputRequestHasAValidUri())
                .When(_ => WhenMapped())
                .Then(_ => ThenNoErrorIsReturned())
                .And(_ => ThenTheMappedRequestHasNoContent())
                .BDDfy();
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
            _inputHeaders = new List<KeyValuePair<string, StringValues>>()
            {
                new KeyValuePair<string, StringValues>("abc", new StringValues(new string[]{"123","456" })),
                new KeyValuePair<string, StringValues>("def", new StringValues(new string[]{"789","012" })),
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
            _inputRequest.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private void GivenTheInputRequestHasNoContent()
        {
            _inputRequest.Body = null;
        }

        private void WhenMapped()
        {
            _mappedRequest = _requestMapper.Map(_inputRequest).GetAwaiter().GetResult();
        }

        private void ThenNoErrorIsReturned()
        {
            _mappedRequest.IsError.ShouldBeFalse();
        }

        private void ThenAnErrorIsReturned()
        {
            _mappedRequest.IsError.ShouldBeTrue();
        }

        private void ThenTheMappedRequestHasUri(string expectedUri)
        {
            _mappedRequest.Data.RequestUri.OriginalString.ShouldBe(expectedUri);
        }

        private void ThenTheMappedRequestHasMethod(string expectedMethod)
        {
            _mappedRequest.Data.Method.ToString().ShouldBe(expectedMethod);
        }

        private void ThenTheMappedRequestHasEachHeader()
        {
            _mappedRequest.Data.Headers.Count().ShouldBe(_inputHeaders.Count);
            foreach(var header in _mappedRequest.Data.Headers)
            {
                var inputHeader = _inputHeaders.First(h => h.Key == header.Key);
                inputHeader.ShouldNotBeNull();
                inputHeader.Value.Count().ShouldBe(header.Value.Count());
                foreach(var inputHeaderValue in inputHeader.Value)
                {
                    header.Value.Any(v => v == inputHeaderValue);
                }
            }
        }

        private void ThenTheMappedRequestHasNoHeaders()
        {
            _mappedRequest.Data.Headers.Count().ShouldBe(0);
        }

        private void ThenTheMappedRequestHasContent(string expectedContent)
        {
            _mappedRequest.Data.Content.ReadAsStringAsync().GetAwaiter().GetResult().ShouldBe(expectedContent);
        }

        private void ThenTheMappedRequestHasNoContent()
        {
            _mappedRequest.Data.Content.ShouldBeNull();
        }

        private void ThenTheMappedRequestIsNull()
        {
            _mappedRequest.Data.ShouldBeNull();
        }
    }
}
