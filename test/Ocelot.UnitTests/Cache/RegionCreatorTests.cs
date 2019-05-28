using Ocelot.Cache;
using Ocelot.Configuration.File;
using Shouldly;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Cache
{
    public class RegionCreatorTests
    {
        private string _result;
        private FileReRoute _reRoute;

        [Fact]
        public void should_create_region()
        {
            var reRoute = new FileReRoute
            {
                UpstreamHttpMethod = new List<string> { "Get" },
                UpstreamPathTemplate = "/testdummy"
            };

            this.Given(_ => GivenTheReRoute(reRoute))
                .When(_ => WhenICreateTheRegion())
                .Then(_ => ThenTheRegionIs("Gettestdummy"))
                .BDDfy();
        }

        [Fact]
        public void should_use_region()
        {
            var reRoute = new FileReRoute
            {
                FileCacheOptions = new FileCacheOptions
                {
                    Region = "region"
                }
            };

            this.Given(_ => GivenTheReRoute(reRoute))
                .When(_ => WhenICreateTheRegion())
                .Then(_ => ThenTheRegionIs("region"))
                .BDDfy();
        }

        private void GivenTheReRoute(FileReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenICreateTheRegion()
        {
            RegionCreator regionCreator = new RegionCreator();
            _result = regionCreator.Create(_reRoute);
        }

        private void ThenTheRegionIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
