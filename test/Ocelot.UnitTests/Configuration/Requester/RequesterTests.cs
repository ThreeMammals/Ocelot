using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Ocelot.Library.Infrastructure.Requester;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class RequesterTests : IDisposable
    {
        private readonly IHttpRequester _httpRequester;
        private readonly HttpTest _httpTest;
        private string _httpMethod;
        private string _downstreamUrl;
        private HttpResponseMessage _result;
        private HttpContent _content;
        private IHeaderDictionary _headers;
        private IRequestCookieCollection _cookies;
        private IQueryCollection _query;
        private string _contentType;

        public RequesterTests()
        {
            _httpTest = new HttpTest();
            _httpRequester = new HttpClientHttpRequester();
        }

        [Fact]
        public void should_call_downstream_url_correctly()
        {
            this.Given(x => x.GivenIHaveHttpMethod("GET"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.OK))
                .When(x => x.WhenIMakeARequest())
                .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.OK))
                .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_obey_http_method()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.Created))
                .When(x => x.WhenIMakeARequest())
                .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.Created))
                .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
                .And(x => x.ThenTheCorrectHttpMethodIsUsed(HttpMethod.Post))
                .BDDfy();
        }

        [Fact]
        public void should_forward_http_content()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenIHaveTheHttpContent(new StringContent("Hi from Tom")))
                .And(x => x.GivenTheContentTypeIs("application/json"))
               .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.Created))
               .When(x => x.WhenIMakeARequest())
               .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.Created))
               .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
               .And(x => x.ThenTheCorrectHttpMethodIsUsed(HttpMethod.Post))
               .And(x => x.ThenTheCorrectContentIsUsed(new StringContent("Hi from Tom")))
               .BDDfy();
        }

        [Fact]
        public void should_forward_http_content_headers()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenIHaveTheHttpContent(new StringContent("Hi from Tom")))
                .And(x => x.GivenTheContentTypeIs("application/json"))
               .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.Created))
               .When(x => x.WhenIMakeARequest())
               .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.Created))
               .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
               .And(x => x.ThenTheCorrectHttpMethodIsUsed(HttpMethod.Post))
               .And(x => x.ThenTheCorrectContentIsUsed(new StringContent("Hi from Tom")))
               .And(x => x.ThenTheCorrectContentHeadersAreUsed(new HeaderDictionary
                {
                    {
                        "Content-Type", "application/json"
                    }
                }))
               .BDDfy();
        }

        [Fact]
        public void should_forward_headers()
        {
            this.Given(x => x.GivenIHaveHttpMethod("GET"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheHttpHeadersAre(new HeaderDictionary
                {
                    {"ChopSticks", "Bubbles" }
                }))
                .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.OK))
                .When(x => x.WhenIMakeARequest())
                .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.OK))
                .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
                .And(x => x.ThenTheCorrectHeadersAreUsed(new HeaderDictionary
                {
                    {"ChopSticks", "Bubbles" }
                }))
                .BDDfy();
        }

        [Fact]
        public void should_forward_cookies()
        {
            this.Given(x => x.GivenIHaveHttpMethod("GET"))
               .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
               .And(x => x.GivenTheCookiesAre(new RequestCookieCollection(new Dictionary<string, string>
               {
                   { "TheCookie","Monster" }
               })))
               .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.OK))
               .When(x => x.WhenIMakeARequest())
               .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.OK))
               .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
               .And(x => x.ThenTheCorrectCookiesAreUsed(new RequestCookieCollection(new Dictionary<string, string>
               {
                   { "TheCookie","Monster" }
               })))
               .BDDfy();
        }

        [Fact]
        public void should_forward_query_string()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheQueryStringIs(new QueryCollection(new Dictionary<string, StringValues>
                {
                    { "jeff", "1" },
                    { "geoff", "2" }
                })))
                .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.Created))
                .When(x => x.WhenIMakeARequest())
                .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.Created))
                .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
                .And(x => x.ThenTheCorrectQueryStringIsUsed("?jeff=1&geoff=2"))
                .BDDfy();
        }

        private void GivenTheContentTypeIs(string contentType)
        {
            _contentType = contentType;
        }

        private void ThenTheCorrectQueryStringIsUsed(string expected)
        {
            _httpTest.CallLog[0].Request.RequestUri.Query.ShouldBe(expected);
        }

        private void GivenTheQueryStringIs(IQueryCollection query)
        {
            _query = query;
        }

        private void ThenTheCorrectCookiesAreUsed(IRequestCookieCollection cookies)
        {
            var expectedCookies = cookies.Select(x => new KeyValuePair<string, string>(x.Key, x.Value));

            foreach (var expectedCookie in expectedCookies)
            {
                _httpTest
                    .CallLog[0]
                    .Request
                    .Headers
                    .ShouldContain(x => x.Key == "Cookie" && x.Value.First() == string.Format("{0}={1}", expectedCookie.Key, expectedCookie.Value));
            }
        }

        private void GivenTheCookiesAre(IRequestCookieCollection cookies)
        {
            _cookies = cookies;
        }

        private void ThenTheCorrectHeadersAreUsed(IHeaderDictionary headers)
        {
            var expectedHeaders = headers.Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value));

            foreach (var expectedHeader in expectedHeaders)
            {
                _httpTest.CallLog[0].Request.Headers.ShouldContain(x => x.Key == expectedHeader.Key && x.Value.First() == expectedHeader.Value[0]);
            }
        }

        private void ThenTheCorrectContentHeadersAreUsed(IHeaderDictionary headers)
        {
            var expectedHeaders = headers.Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value));

            foreach (var expectedHeader in expectedHeaders)
            {
                _httpTest.CallLog[0].Request.Content.Headers.ShouldContain(x => x.Key == expectedHeader.Key 
                && x.Value.First() == expectedHeader.Value[0]
                );
            }
        }

        private void GivenTheHttpHeadersAre(IHeaderDictionary headers)
        {
            _headers = headers;
        }

        private void GivenIHaveTheHttpContent(HttpContent content)
        {
            _content = content;
        }

        private void GivenIHaveHttpMethod(string httpMethod)
        {
            _httpMethod = httpMethod;
        }

        private void GivenIHaveDownstreamUrl(string downstreamUrl)
        {
            _downstreamUrl = downstreamUrl;
        }

        private void GivenTheDownstreamServerReturns(HttpStatusCode statusCode)
        {
            _httpTest.RespondWith(_content != null ? _content.ReadAsStringAsync().Result : string.Empty, (int)statusCode);
        }

        private void WhenIMakeARequest()
        {
            _result = _httpRequester
                .GetResponse(_httpMethod, _downstreamUrl, 
                _content != null ? _content.ReadAsStreamAsync().Result : Stream.Null, 
                _headers, _cookies, _query, _contentType).Result;
        }

        private void ThenTheFollowingIsReturned(HttpStatusCode expected)
        {
            _result.StatusCode.ShouldBe(expected);
        }

        private void ThenTheDownstreamServerIsCalledCorrectly()
        {
            _httpTest.ShouldHaveCalled(_downstreamUrl);
        }

        private void ThenTheCorrectHttpMethodIsUsed(HttpMethod expected)
        {
            _httpTest.CallLog[0].Request.Method.ShouldBe(expected);
        }

        private void ThenTheCorrectContentIsUsed(HttpContent content)
        {
            _httpTest.CallLog[0].Response.Content.ReadAsStringAsync().Result.ShouldBe(content.ReadAsStringAsync().Result);
        }

        public void Dispose()
        {
            _httpTest.Dispose();
        }
    }
}
