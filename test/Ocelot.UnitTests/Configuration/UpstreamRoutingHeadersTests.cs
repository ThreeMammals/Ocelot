using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;

namespace Ocelot.UnitTests.Configuration;

public class UpstreamRoutingHeadersTests
{
    private IReadOnlyDictionary<string, ICollection<string>> _headersDictionary;
    private UpstreamRoutingHeaders _upstreamRoutingHeaders;
    private IHeaderDictionary _requestHeaders;

    [Fact]
    public void Should_create_empty_headers()
    {
        GivenEmptyHeaderDictionary();
        WhenICreate();
        ThenAnyIs(false);
    }

    [Fact]
    public void Should_create_preset_headers()
    {
        GivenPresetHeaderDictionary();
        WhenICreate();
        ThenAnyIs(true);
    }

    [Fact]
    public void Should_not_match_mismatching_request_headers()
    {
        GivenPresetHeaderDictionary();
        AndGivenMismatchingRequestHeaders();
        WhenICreate();
        ThenHasAnyOfIs(false);
        ThenHasAllOfIs(false);
    }

    [Fact]
    public void Should_not_match_matching_header_with_mismatching_value()
    {
        GivenPresetHeaderDictionary();
        AndGivenOneMatchingHeaderWithMismatchingValue();
        WhenICreate();
        ThenHasAnyOfIs(false);
        ThenHasAllOfIs(false);
    }

    [Fact]
    public void Should_match_any_header_not_all()
    {
        GivenPresetHeaderDictionary();
        AndGivenOneMatchingHeaderWithMatchingValue();
        WhenICreate();
        ThenHasAnyOfIs(true);
        ThenHasAllOfIs(false);
    }

    [Fact]
    public void Should_match_any_and_all_headers()
    {
        GivenPresetHeaderDictionary();
        AndGivenTwoMatchingHeadersWithMatchingValues();
        WhenICreate();
        ThenHasAnyOfIs(true);
        ThenHasAllOfIs(true);
    }

    private void GivenEmptyHeaderDictionary()
    {
        _headersDictionary = new Dictionary<string, ICollection<string>>();
    }

    private void GivenPresetHeaderDictionary()
    {
        _headersDictionary = new Dictionary<string, ICollection<string>>()
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
