using System.Collections.Generic;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Cache
{
    public class RegionCreatorTests
    {
        private string _result;
        private ReRoute _reRoute;

        [Fact]
        public void should_create_region()
        {
            var reRoute = new ReRouteBuilder()
                                .WithUpstreamHttpMethod(new List<string>{"Get"})
                                .WithUpstreamPathTemplate("/test/dummy")
                                .Build();

            this.Given(_ => GivenTheReRoute(reRoute))
                .When(_ => WhenICreateTheRegion())
                .Then(_ => ThenTheRegionIs("Gettestdummy"))
                .BDDfy();
        }
        
        private void GivenTheReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenICreateTheRegion()
        {            
            RegionCreator regionCreator = new RegionCreator();
            _result = regionCreator.Region(_reRoute);
        }

        private void ThenTheRegionIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}