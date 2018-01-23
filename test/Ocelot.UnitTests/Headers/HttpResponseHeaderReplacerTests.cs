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
            _result = _replacer.Replace(_response, _headerFindAndReplaces);
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