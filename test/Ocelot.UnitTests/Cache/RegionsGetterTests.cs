using Xunit;
using TestStack.BDDfy;
using Shouldly;
using Ocelot.Cache;
using Moq;
using Ocelot.Configuration.Provider;
using System.Collections.Generic;
using Ocelot.Responses;
using Ocelot.Configuration;
using System.Threading.Tasks;
using Ocelot.Configuration.Builder;
using System;
using Ocelot.Errors;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Cache
{
    public class RegionsGetterTests
    {
        private RegionsGetter _regionsGetter;
        private readonly Mock<IOcelotConfigurationProvider> _provider;
        private readonly Mock<IRegionCreator> _creator;
        private readonly Mock<IOcelotLoggerFactory> _factory;
        private List<string> _result;

        public RegionsGetterTests()
        {
            _provider = new Mock<IOcelotConfigurationProvider>();
            _creator = new Mock<IRegionCreator>();
            _factory = new  Mock<IOcelotLoggerFactory>();
            var logger = new Mock<IOcelotLogger>();
            _factory
                .Setup(x => x.CreateLogger<RegionsGetter>())
                .Returns(logger.Object);
            _regionsGetter = new RegionsGetter(_provider.Object, _creator.Object, _factory.Object);
        }
        
        [Fact]
        public void should_get_regions()
        {
            var reRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod(new List<string>{"Get"})
                .WithUpstreamPathTemplate("/")
                .Build();

            var reRoutes = new List<ReRoute>
            {
                reRoute
            };

            var config = new OcelotConfiguration(reRoutes, "whocares!");

            var expected = new List<string>
            {
                "balls"
            };

            this.Given(_ => GivenTheFollowingConfig(config))
                .And(_ => GivenTheProviderReturns("balls"))
                .When(_ => WhenIGetTheRegions())
                .Then(_ => ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

           [Fact]
        public void should_return_empty_regions()
        {
            var expected = new List<string>();

            this.Given(_ => GivenAnErrorGettingTheConfig())
                .When(_ => WhenIGetTheRegions())
                .Then(_ => ThenTheFollowingIsReturned(expected))
                .BDDfy();
        }

        private void GivenAnErrorGettingTheConfig()
        {
            var config = new OcelotConfiguration(new List<ReRoute>(), "whocares!");
            
            _provider
                .Setup(x => x.Get())
                .ReturnsAsync(new ErrorResponse<IOcelotConfiguration>(It.IsAny<Error>()));
        }

        private void GivenTheProviderReturns(string expected)
        {
            _creator
                .Setup(x => x.Region(It.IsAny<ReRoute>()))
                .Returns(expected);
        }

        private void GivenTheFollowingConfig(IOcelotConfiguration config)
        {
            _provider
                .Setup(x => x.Get())
                .ReturnsAsync(new OkResponse<IOcelotConfiguration>(config));
        }

        private void WhenIGetTheRegions()
        {
            _result = _regionsGetter.Regions().Result;
        }

        private void ThenTheFollowingIsReturned(List<string> expected)
        {
            _result.ShouldBe(expected);
        }
    }
}