using System.Collections.Generic;
using System.Linq;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration;
using Xunit;
using TestStack.BDDfy;
using Shouldly;

namespace Ocelot.UnitTests.Configuration
{
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
            UpstreamHeaderRoutingOptions expected = new UpstreamHeaderRoutingOptions(
                headers: new Dictionary<string, HashSet<string>>()
                {
                    { "header1", new HashSet<string>() { "value1", "value2" }},
                    { "header2", new HashSet<string>() { "value3" }},
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
                Headers = new Dictionary<string, IList<string>>()
                {
                    { "Header1", new List<string>() { "Value1", "Value2" }},
                    { "Header2", new List<string>() { "Value3" }},
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
            foreach (KeyValuePair<string, HashSet<string>> pair in _upstreamHeaderRoutingOptions.Headers.Headers)
            {
                expected.Headers.Headers.TryGetValue(pair.Key, out var expectedValue).ShouldBe(true);
                expectedValue.SetEquals(pair.Value).ShouldBe(true);
            }

            _upstreamHeaderRoutingOptions.Mode.ShouldBe(expected.Mode);
        }
    }
}
