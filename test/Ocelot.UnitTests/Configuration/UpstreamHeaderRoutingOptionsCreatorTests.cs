using System.Collections.Generic;
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
                mode: UpstreamHeaderRoutingCombinationMode.All
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
                Headers = new Dictionary<string, List<string>>()
                {
                    { "header1", new List<string>() { "value1", "value2" }},
                    { "header2", new List<string>() { "value3" }},
                },
                CombinationMode = "all",
            };
        }

        private void WhenICreate()
        {
            _upstreamHeaderRoutingOptions = _creator.Create(_fileUpstreamHeaderRoutingOptions);
        }

        private void ThenTheCreatedMatchesThis(UpstreamHeaderRoutingOptions expected)
        {
            _upstreamHeaderRoutingOptions.Headers.ShouldBe(expected.Headers);
            _upstreamHeaderRoutingOptions.Mode.ShouldBe(expected.Mode);
        }
    }
}
