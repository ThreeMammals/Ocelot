using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Ocelot.Request.Builder;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Ocelot.Configuration;
using Ocelot.Requester.QoS;

namespace Ocelot.UnitTests.Request
{
    public class RequestBuilderTests
    {
        private string _httpMethod;
        private string _downstreamUrl;
        private HttpContent _content;
        private IHeaderDictionary _headers;
        private IRequestCookieCollection _cookies;
        private QueryString _query;
        private string _contentType;
        private readonly IRequestCreator _requestCreator;
        private Response<Ocelot.Request.Request> _result;
        private Ocelot.RequestId.RequestId _requestId;
        private bool _isQos;
        private IQoSProvider _qoSProvider;

        public RequestBuilderTests()
        {
            _content = new StringContent(string.Empty);
            _requestCreator = new HttpRequestCreator();
        }

        [Fact]
        public void should_user_downstream_url()
        {
            this.Given(x => x.GivenIHaveHttpMethod("GET"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x=> x.GivenTheQos(true, new NoQoSProvider()))
                .When(x => x.WhenICreateARequest())
                .And(x => x.ThenTheCorrectDownstreamUrlIsUsed("http://www.bbc.co.uk/"))
                .BDDfy();
        }

        [Fact]
        public void should_use_http_method()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheQos(true, new NoQoSProvider()))

                .When(x => x.WhenICreateARequest())
                .And(x => x.ThenTheCorrectHttpMethodIsUsed(HttpMethod.Post))
                .BDDfy();
        }

        [Fact]
        public void should_use_http_content()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenIHaveTheHttpContent(new StringContent("Hi from Tom")))
                .And(x => x.GivenTheContentTypeIs("application/json"))
                              .And(x => x.GivenTheQos(true, new NoQoSProvider()))

                              .When(x => x.WhenICreateARequest())
               .And(x => x.ThenTheCorrectContentIsUsed(new StringContent("Hi from Tom")))
               .BDDfy();
        }

        [Fact]
        public void should_use_http_content_headers()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenIHaveTheHttpContent(new StringContent("Hi from Tom")))
                .And(x => x.GivenTheContentTypeIs("application/json"))
                .And(x => x.GivenTheQos(true, new NoQoSProvider()))

               .When(x => x.WhenICreateARequest())
               .And(x => x.ThenTheCorrectContentHeadersAreUsed(new HeaderDictionary
                {
                    {
                        "Content-Type", "application/json"
                    }
                }))
               .BDDfy();
        }

        [Fact]
        public void should_use_unvalidated_http_content_headers()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenIHaveTheHttpContent(new StringContent("Hi from Tom")))
                .And(x => x.GivenTheContentTypeIs("application/json; charset=utf-8"))
                                .And(x => x.GivenTheQos(true, new NoQoSProvider()))

               .When(x => x.WhenICreateARequest())
               .And(x => x.ThenTheCorrectContentHeadersAreUsed(new HeaderDictionary
                {
                    {
                        "Content-Type", "application/json; charset=utf-8"
                    }
                }))
               .BDDfy();
        }

        [Fact]
        public void should_use_headers()
        {
            this.Given(x => x.GivenIHaveHttpMethod("GET"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheHttpHeadersAre(new HeaderDictionary
                {
                    {"ChopSticks", "Bubbles" }
                }))
                                .And(x => x.GivenTheQos(true, new NoQoSProvider()))

                .When(x => x.WhenICreateARequest())
                .And(x => x.ThenTheCorrectHeadersAreUsed(new HeaderDictionary
                {
                    {"ChopSticks", "Bubbles" }
                }))
                .BDDfy();
        }

        [Fact]
        public void should_use_request_id()
        {
            var requestId = Guid.NewGuid().ToString();

            this.Given(x => x.GivenIHaveHttpMethod("GET"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheHttpHeadersAre(new HeaderDictionary()))
                .And(x => x.GivenTheRequestIdIs(new Ocelot.RequestId.RequestId("RequestId", requestId)))
                              .And(x => x.GivenTheQos(true, new NoQoSProvider()))
  .When(x => x.WhenICreateARequest())
                .And(x => x.ThenTheCorrectHeadersAreUsed(new HeaderDictionary
                {
                    {"RequestId", requestId }
                }))
                .BDDfy();
        }

        [Fact]
        public void should_not_use_request_if_if_already_in_headers()
        {
            this.Given(x => x.GivenIHaveHttpMethod("GET"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheHttpHeadersAre(new HeaderDictionary
                {
                    {"RequestId", "534534gv54gv45g" }
                }))
                .And(x => x.GivenTheRequestIdIs(new Ocelot.RequestId.RequestId("RequestId", Guid.NewGuid().ToString())))
                               .And(x => x.GivenTheQos(true, new NoQoSProvider()))
 .When(x => x.WhenICreateARequest())
                .And(x => x.ThenTheCorrectHeadersAreUsed(new HeaderDictionary
                {
                    {"RequestId", "534534gv54gv45g" }
                }))
                .BDDfy();
        }

        [Theory]
        [InlineData(null, "blahh")]
        [InlineData("", "blahh")]
        [InlineData("RequestId", "")]
        [InlineData("RequestId", null)]
        public void should_not_use_request_id(string requestIdKey, string requestIdValue)
        {
            this.Given(x => x.GivenIHaveHttpMethod("GET"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheHttpHeadersAre(new HeaderDictionary()))
                .And(x => x.GivenTheRequestIdIs(new Ocelot.RequestId.RequestId(requestIdKey, requestIdValue)))
                              .And(x => x.GivenTheQos(true, new NoQoSProvider()))
  .When(x => x.WhenICreateARequest())
                .And(x => x.ThenTheRequestIdIsNotInTheHeaders())
                .BDDfy();
        }

        private void GivenTheRequestIdIs(Ocelot.RequestId.RequestId requestId)
        {
            _requestId = requestId;
        }

        private void GivenTheQos(bool isQos, IQoSProvider qoSProvider)
        {
            _isQos = isQos;
            _qoSProvider = qoSProvider;
        }

        [Fact]
        public void should_user_query_string()
        {
            this.Given(x => x.GivenIHaveHttpMethod("POST"))
                .And(x => x.GivenIHaveDownstreamUrl("http://www.bbc.co.uk"))
                .And(x => x.GivenTheQueryStringIs(new QueryString("?jeff=1&geoff=2")))
                .When(x => x.WhenICreateARequest())
                .And(x => x.ThenTheCorrectQueryStringIsUsed("?jeff=1&geoff=2"))
                .BDDfy();
        }

        private void GivenTheContentTypeIs(string contentType)
        {
            _contentType = contentType;
        }

        private void ThenTheCorrectQueryStringIsUsed(string expected)
        {
            _result.Data.HttpRequestMessage.RequestUri.Query.ShouldBe(expected);
        }

        private void GivenTheQueryStringIs(QueryString query)
        {
            _query = query;
        }

        private void ThenTheCorrectCookiesAreUsed(IRequestCookieCollection expected)
        {
           /* var resultCookies = _result.Data.CookieContainer.GetCookies(new Uri(_downstreamUrl + _query));
            var resultDictionary = resultCookies.Cast<Cookie>().ToDictionary(cook => cook.Name, cook => cook.Value);

            foreach (var expectedCookie in expected)
            {
                var resultCookie = resultDictionary[expectedCookie.Key];
                resultCookie.ShouldBe(expectedCookie.Value);
            }*/
        }

        private void GivenTheCookiesAre(IRequestCookieCollection cookies)
        {
            _cookies = cookies;
        }

        private void ThenTheRequestIdIsNotInTheHeaders()
        {
            _result.Data.HttpRequestMessage.Headers.ShouldNotContain(x => x.Key == "RequestId");
        }

        private void ThenTheCorrectHeadersAreUsed(IHeaderDictionary expected)
        {
            var expectedHeaders = expected.Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value));

            foreach (var expectedHeader in expectedHeaders)
            {
                _result.Data.HttpRequestMessage.Headers.ShouldContain(x => x.Key == expectedHeader.Key && x.Value.First() == expectedHeader.Value[0]);
            }
        }

        private void ThenTheCorrectContentHeadersAreUsed(IHeaderDictionary expected)
        {
            var expectedHeaders = expected.Select(x => new KeyValuePair<string, string[]>(x.Key, x.Value));

            foreach (var expectedHeader in expectedHeaders)
            {
                _result.Data.HttpRequestMessage.Content.Headers.ShouldContain(x => x.Key == expectedHeader.Key 
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

        private void WhenICreateARequest()
        {
            _result = _requestCreator.Build(_httpMethod, _downstreamUrl, _content?.ReadAsStreamAsync().Result, _headers,
                _query, _contentType, _requestId,_isQos,_qoSProvider).Result;
        }


        private void ThenTheCorrectDownstreamUrlIsUsed(string expected)
        {
            _result.Data.HttpRequestMessage.RequestUri.AbsoluteUri.ShouldBe(expected);
        }

        private void ThenTheCorrectHttpMethodIsUsed(HttpMethod expected)
        {
            _result.Data.HttpRequestMessage.Method.Method.ShouldBe(expected.Method);
        }

        private void ThenTheCorrectContentIsUsed(HttpContent expected)
        {
            _result.Data.HttpRequestMessage.Content.ReadAsStringAsync().Result.ShouldBe(expected.ReadAsStringAsync().Result);
        }
    }
}
