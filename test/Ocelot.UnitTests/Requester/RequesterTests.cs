using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http.Testing;
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
               .And(x => x.GivenTheDownstreamServerReturns(HttpStatusCode.Created))
               .When(x => x.WhenIMakeARequest())
               .Then(x => x.ThenTheFollowingIsReturned(HttpStatusCode.Created))
               .And(x => x.ThenTheDownstreamServerIsCalledCorrectly())
               .And(x => x.ThenTheCorrectHttpMethodIsUsed(HttpMethod.Post))
               .And(x => x.ThenTheCorrectContentIsUsed(new StringContent("Hi from Tom")))
               .BDDfy();
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
            _result = _httpRequester.GetResponse(_httpMethod, _downstreamUrl, _content != null ? _content.ReadAsStreamAsync().Result : Stream.Null).Result;
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
