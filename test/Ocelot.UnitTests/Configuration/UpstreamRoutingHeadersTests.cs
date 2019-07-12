using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;
using TestStack.BDDfy;
using Ocelot.Configuration;

namespace Ocelot.UnitTests.Configuration
{
    public class UpstreamRoutingHeadersTests
    {
        private Dictionary<string, HashSet<string>> _headersDictionary;

        private UpstreamRoutingHeaders _upstreamRoutingHeaders;

        private IHeaderDictionary _requestHeaders;

        [Fact]
        public void should_create_empty_headers()
        {
            this.Given(_ => GivenEmptyHeaderDictionary())
                .When(_ => WhenICreate())
                .Then(_ => ThenEmptyIs(true))
                .BDDfy();
        }

        [Fact]
        public void should_create_preset_headers()
        {
            this.Given(_ => GivenPresetHeaderDictionary())
                .When(_ => WhenICreate())
                .Then(_ => ThenEmptyIs(false))
                .BDDfy();
        }

        [Fact]
        public void should_not_match_mismatching_request_headers()
        {
            this.Given(_ => GivenPresetHeaderDictionary())
                .And(_ => AndGivenMismatchingRequestHeaders())
                .When(_ => WhenICreate())
                .Then(_ => ThenHasAnyOfIs(false))
                .And(_ => ThenHasAllOfIs(false))
                .BDDfy();
        }

        [Fact]
        public void should_not_match_matching_header_with_mismatching_value()
        {
            this.Given(_ => GivenPresetHeaderDictionary())
                .And(_ => AndGivenOneMatchingHeaderWithMismatchingValue())
                .When(_ => WhenICreate())
                .Then(_ => ThenHasAnyOfIs(false))
                .And(_ => ThenHasAllOfIs(false))
                .BDDfy();
        }

        [Fact]
        public void should_match_any_header_not_all()
        {
            this.Given(_ => GivenPresetHeaderDictionary())
                .And(_ => AndGivenOneMatchingHeaderWithMatchingValue())
                .When(_ => WhenICreate())
                .Then(_ => ThenHasAnyOfIs(true))
                .And(_ => ThenHasAllOfIs(false))
                .BDDfy();
        }

        [Fact]
        public void should_match_any_and_all_headers()
        {
            this.Given(_ => GivenPresetHeaderDictionary())
                .And(_ => AndGivenTwoMatchingHeadersWithMatchingValues())
                .When(_ => WhenICreate())
                .Then(_ => ThenHasAnyOfIs(true))
                .And(_ => ThenHasAllOfIs(true))
                .BDDfy();
        }

        private void GivenEmptyHeaderDictionary()
        {
            _headersDictionary = new Dictionary<string, HashSet<string>>();
        }

        private void GivenPresetHeaderDictionary()
        {
            _headersDictionary = new Dictionary<string, HashSet<string>>()
            {
                { "testHeader1", new HashSet<string>() { "testHeader1Value1", "testHeader1Value2" } },
                { "testHeader2", new HashSet<string>() { "testHeader1Value1", "testHeader2Value2" } },
            };
        }

        private void AndGivenMismatchingRequestHeaders()
        {
            _requestHeaders = new HeaderDictionary() {
                { "someHeader", new StringValues(new string[]{ "someHeaderValue" })},
            };
        }

        private void AndGivenOneMatchingHeaderWithMismatchingValue()
        {
            _requestHeaders = new HeaderDictionary() {
                { "testHeader1", new StringValues(new string[]{ "mismatchingValue" })},
            };
        }

        private void AndGivenOneMatchingHeaderWithMatchingValue()
        {
            _requestHeaders = new HeaderDictionary() {
                { "testHeader1", new StringValues(new string[]{ "testHeader1Value1" })},
            };
        }

        private void AndGivenTwoMatchingHeadersWithMatchingValues()
        {
            _requestHeaders = new HeaderDictionary() {
                { "testHeader1", new StringValues(new string[]{ "testHeader1Value1", "bogusValue" })},
                { "testHeader2", new StringValues(new string[]{ "bogusValue", "testHeader2Value2" })},
            };
        }

        private void WhenICreate()
        {
            _upstreamRoutingHeaders = new UpstreamRoutingHeaders(_headersDictionary);
        }

        private void ThenEmptyIs(bool expected)
        {
            Assert.True(_upstreamRoutingHeaders.Empty() == expected);
        }

        private void ThenHasAnyOfIs(bool expected)
        {
            Assert.True(_upstreamRoutingHeaders.HasAnyOf(_requestHeaders) == expected);
        }

        private void ThenHasAllOfIs(bool expected)
        {
            Assert.True(_upstreamRoutingHeaders.HasAllOf(_requestHeaders) == expected);
        }
    }
}
