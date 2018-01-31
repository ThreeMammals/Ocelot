using Xunit;
using Shouldly;
using TestStack.BDDfy;
using System.Net.Http;
using Ocelot.Headers;
using Ocelot.Configuration;
using System.Collections.Generic;
using Ocelot.Responses;
using System.Linq;

namespace Ocelot.UnitTests.Headers
{
    public class HttpResponseHeaderReplacerTests
    {
        private HttpResponseMessage _response;
        private HttpResponseHeaderReplacer _replacer;
        private List<HeaderFindAndReplace> _headerFindAndReplaces;
        private Response _result;
        private HttpRequestMessage _request;

        public HttpResponseHeaderReplacerTests()
        {
            _replacer = new HttpResponseHeaderReplacer();
        }
        [Fact]
        public void should_replace_headers()
        {
            var response = new HttpResponseMessage();
            response.Headers.Add("test", "test");

            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("test", "test", "chiken", 0));

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeadersAreReplaced())
                .BDDfy();
        }

        [Fact]
        public void should_not_replace_headers()
        {
            var response = new HttpResponseMessage();
            response.Headers.Add("test", "test");

            var fAndRs = new List<HeaderFindAndReplace>();

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeadersAreNotReplaced())
                .BDDfy();
        }

        [Fact]
        public void should_replace_downstream_base_url_with_ocelot_base_url()
        {
            var downstreamUrl = "http://downstream.com/";

            var request = new HttpRequestMessage();
            request.RequestUri = new System.Uri(downstreamUrl);

            var response = new HttpResponseMessage();
            response.Headers.Add("Location", downstreamUrl);

            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("Location", "{DownstreamBaseUrl}", "http://ocelot.com/", 0));

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheRequestIs(request))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeaderShouldBe("Location", "http://ocelot.com/"))
                .BDDfy();
        }

        [Fact]
        public void should_replace_downstream_base_url_with_ocelot_base_url_with_port()
        {
            var downstreamUrl = "http://downstream.com/";

            var request = new HttpRequestMessage();
            request.RequestUri = new System.Uri(downstreamUrl);

            var response = new HttpResponseMessage();
            response.Headers.Add("Location", downstreamUrl);

            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("Location", "{DownstreamBaseUrl}", "http://ocelot.com:123/", 0));

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheRequestIs(request))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeaderShouldBe("Location", "http://ocelot.com:123/"))
                .BDDfy();
        }


        [Fact]
        public void should_replace_downstream_base_url_with_ocelot_base_url_and_path()
        {
            var downstreamUrl = "http://downstream.com/test/product";

            var request = new HttpRequestMessage();
            request.RequestUri = new System.Uri(downstreamUrl);

            var response = new HttpResponseMessage();
            response.Headers.Add("Location", downstreamUrl);

            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("Location", "{DownstreamBaseUrl}", "http://ocelot.com/", 0));

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheRequestIs(request))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeaderShouldBe("Location", "http://ocelot.com/test/product"))
                .BDDfy();
        }

        [Fact]
        public void should_replace_downstream_base_url_with_ocelot_base_url_with_path_and_port()
        {
            var downstreamUrl = "http://downstream.com/test/product";

            var request = new HttpRequestMessage();
            request.RequestUri = new System.Uri(downstreamUrl);

            var response = new HttpResponseMessage();
            response.Headers.Add("Location", downstreamUrl);

            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("Location", "{DownstreamBaseUrl}", "http://ocelot.com:123/", 0));

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheRequestIs(request))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeaderShouldBe("Location", "http://ocelot.com:123/test/product"))
                .BDDfy();
        }

        [Fact]
        public void should_replace_downstream_base_url_and_port_with_ocelot_base_url()
        {
            var downstreamUrl = "http://downstream.com:123/test/product";

            var request = new HttpRequestMessage();
            request.RequestUri = new System.Uri(downstreamUrl);

            var response = new HttpResponseMessage();
            response.Headers.Add("Location", downstreamUrl);

            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("Location", "{DownstreamBaseUrl}", "http://ocelot.com/", 0));

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheRequestIs(request))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeaderShouldBe("Location", "http://ocelot.com/test/product"))
                .BDDfy();
        }

        [Fact]
        public void should_replace_downstream_base_url_and_port_with_ocelot_base_url_and_port()
        {
            var downstreamUrl = "http://downstream.com:123/test/product";

            var request = new HttpRequestMessage();
            request.RequestUri = new System.Uri(downstreamUrl);

            var response = new HttpResponseMessage();
            response.Headers.Add("Location", downstreamUrl);

            var fAndRs = new List<HeaderFindAndReplace>();
            fAndRs.Add(new HeaderFindAndReplace("Location", "{DownstreamBaseUrl}", "http://ocelot.com:321/", 0));

            this.Given(x => GivenTheHttpResponse(response))
                .And(x => GivenTheRequestIs(request))
                .And(x => GivenTheFollowingHeaderReplacements(fAndRs))
                .When(x => WhenICallTheReplacer())
                .Then(x => ThenTheHeaderShouldBe("Location", "http://ocelot.com:321/test/product"))
                .BDDfy();
        }

        private void GivenTheRequestIs(HttpRequestMessage request)
        {
            _request = request;
        }

        private void ThenTheHeadersAreNotReplaced()
        {
             _result.ShouldBeOfType<OkResponse>();
            foreach (var f in _headerFindAndReplaces)
            {
                _response.Headers.TryGetValues(f.Key, out var values);
                values.ToList()[f.Index].ShouldBe("test");
            }
        }

        private void GivenTheFollowingHeaderReplacements(List<HeaderFindAndReplace> fAndRs)
        {
            _headerFindAndReplaces = fAndRs;
        }

        private void GivenTheHttpResponse(HttpResponseMessage response)
        {
            _response = response;
        }

        private void WhenICallTheReplacer()
        {
            _result = _replacer.Replace(_response, _headerFindAndReplaces, _request);
        }

        private void ThenTheHeaderShouldBe(string key, string value)
        {
            var test = _response.Headers.GetValues(key);
            test.First().ShouldBe(value);
        }

         private void ThenTheHeadersAreReplaced()
        {
            _result.ShouldBeOfType<OkResponse>();
            foreach (var f in _headerFindAndReplaces)
            {
                _response.Headers.TryGetValues(f.Key, out var values);
                values.ToList()[f.Index].ShouldBe(f.Replace);
            }
        }
    }
}