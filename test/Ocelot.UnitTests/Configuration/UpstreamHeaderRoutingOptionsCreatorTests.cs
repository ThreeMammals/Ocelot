using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration;
using Xunit;
using TestStack.BDDfy;
using Shouldly;

namespace Ocelot.UnitTests.Configuration;

public class UpstreamHeaderRoutingOptionsCreatorTests
{
    private FileUpstreamHeaderRoutingOptions _fileUpstreamHeaderRoutingOptions;
    private IUpstreamHeaderRoutingOptionsCreator _creator;
    private UpstreamHeaderRoutingOptions _upstreamHeaderRoutingOptions;

    public UpstreamHeaderRoutingOptionsCreatorTests()
    {
        _creator = new UpstreamHeaderRoutingOptionsCreator();
    }

    [Fact]
    public void should_create_upstream_routing_header_options()
    {
        UpstreamHeaderRoutingOptions expected = new(
            headers: new Dictionary<string, ICollection<string>>()
            {
                { "HEADER1", new[] { "Value1", "Value2" }},
                { "HEADER2", new[] { "Value3" }},
            },
            mode: UpstreamHeaderRoutingTriggerMode.All
        );

        this.Given(_ => GivenTheseFileUpstreamHeaderRoutingOptions())
            .When(_ => WhenICreate())
            .Then(_ => ThenTheCreatedMatchesThis(expected))
            .BDDfy();
    }

    private void GivenTheseFileUpstreamHeaderRoutingOptions()
    {
        _fileUpstreamHeaderRoutingOptions = new FileUpstreamHeaderRoutingOptions()
        {
            Headers = new Dictionary<string, ICollection<string>>()
            {
                { "Header1", new[] { "Value1", "Value2" }},
                { "Header2", new[] { "Value3" }},
            },
            TriggerOn = "all",
        };
    }

    private void WhenICreate()
    {
        _upstreamHeaderRoutingOptions = _creator.Create(_fileUpstreamHeaderRoutingOptions);
    }

    private void ThenTheCreatedMatchesThis(UpstreamHeaderRoutingOptions expected)
    {
        _upstreamHeaderRoutingOptions.Headers.Headers.Count.ShouldBe(expected.Headers.Headers.Count);
        foreach (var pair in _upstreamHeaderRoutingOptions.Headers.Headers)
        {
            expected.Headers.Headers.TryGetValue(pair.Key, out var expectedValue).ShouldBe(true);
            expectedValue.ShouldBeEquivalentTo(pair.Value);
        }

        _upstreamHeaderRoutingOptions.Mode.ShouldBe(expected.Mode);
    }
}
