using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using static Ocelot.UnitTests.Middleware.UserDefinedResponseAggregatorTests;

namespace Ocelot.UnitTests.Middleware
{
    public class DefinedAggregatorProviderTests
    {
        private ServiceLocatorDefinedAggregatorProvider _provider;
        private Response<IDefinedAggregator> _aggregator;
        private ReRoute _reRoute;

        [Fact]
        public void should_find_aggregator()
        {
            var reRoute = new ReRouteBuilder()
                .WithAggregator("TestDefinedAggregator")
                .Build();

            this.Given(_ => GivenDefinedAggregator())
                .And(_ => GivenReRoute(reRoute))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheAggregatorIsReturned())
                .BDDfy();
        }

        [Fact]
        public void should_not_find_aggregator()
        {
            var reRoute = new ReRouteBuilder()
                .WithAggregator("TestDefinedAggregator")
                .Build();

            this.Given(_ => GivenNoDefinedAggregator())
                .And(_ => GivenReRoute(reRoute))
                .When(_ => WhenIGet())
                .Then(_ => ThenAnErrorIsReturned())
                .BDDfy();
        }

        private void GivenDefinedAggregator()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IDefinedAggregator, TestDefinedAggregator>();
            var services = serviceCollection.BuildServiceProvider();
            _provider = new ServiceLocatorDefinedAggregatorProvider(services);
        }

        private void ThenTheAggregatorIsReturned()
        {
            _aggregator.Data.ShouldNotBeNull();
            _aggregator.Data.ShouldBeOfType<TestDefinedAggregator>();
            _aggregator.IsError.ShouldBeFalse();
        }

        private void GivenNoDefinedAggregator()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();
            _provider = new ServiceLocatorDefinedAggregatorProvider(services);
        }

        private void GivenReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGet()
        {
            _aggregator = _provider.Get(_reRoute);
        }

        private void ThenAnErrorIsReturned()
        {
            _aggregator.IsError.ShouldBeTrue();
            _aggregator.Errors[0].Message.ShouldBe("Could not find Aggregator: TestDefinedAggregator");
            _aggregator.Errors[0].ShouldBeOfType<CouldNotFindAggregatorError>();
        }
    }
}
