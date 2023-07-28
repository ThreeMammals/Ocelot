using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;
using TestStack.BDDfy;
using Shouldly;
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
                .Then(_ => ThenAnyIs(false))
                .BDDfy();
        }

        [Fact]
        public void should_create_preset_headers()
        {
            this.Given(_ => GivenPresetHeaderDictionary())
                .When(_ => WhenICreate())
                .Then(_ => ThenAnyIs(true))
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
                { "testheader1", new HashSet<string>() { "testheader1value1", "testheader1value2" } },
                { "testheader2", new HashSet<string>() { "testheader1Value1", "testheader2value2" } },
            };
        }

        private void AndGivenMismatchingRequestHeaders()
        {
            _requestHeaders = new HeaderDictionary() {
                { "someHeader", new StringValues(new []{ "someHeaderValue" })},
            };
        }

        private void AndGivenOneMatchingHeaderWithMismatchingValue()
        {
            _requestHeaders = new HeaderDictionary() {
                { "testHeader1", new StringValues(new []{ "mismatchingValue" })},
            };
        }

        private void AndGivenOneMatchingHeaderWithMatchingValue()
        {
            _requestHeaders = new HeaderDictionary() {
                { "testHeader1", new StringValues(new []{ "testHeader1Value1" })},
            };
        }

        private void AndGivenTwoMatchingHeadersWithMatchingValues()
        {
            _requestHeaders = new HeaderDictionary() {
                { "testHeader1", new StringValues(new []{ "testHeader1Value1", "bogusValue" })},
                { "testHeader2", new StringValues(new []{ "bogusValue", "testHeader2Value2" })},
            };
        }

        private void WhenICreate()
        {
            _upstreamRoutingHeaders = new UpstreamRoutingHeaders(_headersDictionary);
        }

        private void ThenAnyIs(bool expected)
        {
            _upstreamRoutingHeaders.Any().ShouldBe(expected);
        }

        private void ThenHasAnyOfIs(bool expected)
        {
            _upstreamRoutingHeaders.HasAnyOf(_requestHeaders).ShouldBe(expected);
        }

        private void ThenHasAllOfIs(bool expected)
        {
            _upstreamRoutingHeaders.HasAllOf(_requestHeaders).ShouldBe(expected);
        }
    }
}
