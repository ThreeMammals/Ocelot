using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware.Multiplexer;
using Shouldly;
using Xunit;
using static Ocelot.UnitTests.Middleware.UserDefinedResponseAggregatorTests;

namespace Ocelot.UnitTests.Middleware
{
    //todo - refactory BDDfy?
    public class DefinedAggregatorProviderTests
    {
        private ServiceLocatorDefinedAggregatorProvider _provider;
        
        [Fact]
        public void should_find_aggregator()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IDefinedAggregator, TestDefinedAggregator>();
            var services = serviceCollection.BuildServiceProvider();
            _provider = new ServiceLocatorDefinedAggregatorProvider(services);
            var reRoute = new ReRouteBuilder().WithAggregator("TestDefinedAggregator").Build();
            var aggregator = _provider.Get(reRoute);
            aggregator.Data.ShouldNotBeNull();
            aggregator.Data.ShouldBeOfType<TestDefinedAggregator>();
            aggregator.IsError.ShouldBeFalse();
        }

        [Fact]
        public void should_not_find_aggregator()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();
            _provider = new ServiceLocatorDefinedAggregatorProvider(services);
            var reRoute = new ReRouteBuilder().WithAggregator("TestDefinedAggregator").Build();
            var aggregator = _provider.Get(reRoute);
            aggregator.IsError.ShouldBeTrue();
            aggregator.Errors[0].Message.ShouldBe("Could not find Aggregator: TestDefinedAggregator");
            aggregator.Errors[0].ShouldBeOfType<CouldNotFindAggregatorError>();
        }
    }
}