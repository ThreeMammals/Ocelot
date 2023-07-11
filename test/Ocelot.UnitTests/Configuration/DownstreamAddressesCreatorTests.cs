using System.Collections.Generic;

using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

using Shouldly;

using TestStack.BDDfy;

using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class DownstreamAddressesCreatorTests
    {
        public DownstreamAddressesCreator _creator;
        private FileRoute _route;
        private List<DownstreamHostAndPort> _result;

        public DownstreamAddressesCreatorTests()
        {
            _creator = new DownstreamAddressesCreator();
        }

        [Fact]
        public void should_do_nothing()
        {
            var route = new FileRoute();

            var expected = new List<DownstreamHostAndPort>();

            this.Given(x => GivenTheFollowingRoute(route))
                .When(x => WhenICreate())
                .Then(x => TheThenFollowingIsReturned(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_addresses_from_old_downstream_path_and_port()
        {
            var route = new FileRoute
            {
                DownstreamHostAndPorts = new List<FileHostAndPort>
                {
                    new()
                    {
                        Host = "test",
                        Port = 80,
                    },
                },
            };

            var expected = new List<DownstreamHostAndPort>
            {
                new("test", 80),
            };

            this.Given(x => GivenTheFollowingRoute(route))
                .When(x => WhenICreate())
                .Then(x => TheThenFollowingIsReturned(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_downstream_addresses_from_downstream_host_and_ports()
        {
            var route = new FileRoute
            {
                DownstreamHostAndPorts = new List<FileHostAndPort>
                {
                    new()
                    {
                        Host = "test",
                        Port = 80,
                    },
                    new()
                    {
                        Host = "west",
                        Port = 443,
                    },
                },
            };

            var expected = new List<DownstreamHostAndPort>
            {
                new("test", 80),
                new("west", 443),
            };

            this.Given(x => GivenTheFollowingRoute(route))
                .When(x => WhenICreate())
                .Then(x => TheThenFollowingIsReturned(expected))
                .BDDfy();
        }

        private void GivenTheFollowingRoute(FileRoute route)
        {
            _route = route;
        }

        private void WhenICreate()
        {
            _result = _creator.Create(_route);
        }

        private void TheThenFollowingIsReturned(List<DownstreamHostAndPort> expecteds)
        {
            _result.Count.ShouldBe(expecteds.Count);

            for (var i = 0; i < _result.Count; i++)
            {
                var result = _result[i];
                var expected = expecteds[i];

                result.Host.ShouldBe(expected.Host);
                result.Port.ShouldBe(expected.Port);
            }
        }
    }
}
