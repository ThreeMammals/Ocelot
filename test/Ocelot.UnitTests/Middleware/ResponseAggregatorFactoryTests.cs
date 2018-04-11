namespace Ocelot.UnitTests.Middleware
{
    using System;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.Middleware.Multiplexer;
    using Shouldly;
    using Xunit;

    public class ResponseAggregatorFactoryTests
    {
        private InMemoryResponseAggregatorFactory _factory;
        private Mock<IDefinedAggregatorProvider> _provider;

        public ResponseAggregatorFactoryTests()
        {
            _provider = new Mock<IDefinedAggregatorProvider>();
            _factory = new InMemoryResponseAggregatorFactory(_provider.Object);
        }
        
        // todo - bddfy these tests
        [Fact]
        public void should_return_simple_json_aggregator()
        {
            var reRoute = new ReRouteBuilder().Build();
            var aggregator = _factory.Get(reRoute);
            aggregator.ShouldBeOfType<SimpleJsonResponseAggregator>();
        }

        [Fact]
        public void should_return_user_defined_aggregator()
        {
            var reRoute = new ReRouteBuilder().WithAggregator("doesntmatter").Build();
            var aggregator = _factory.Get(reRoute);
            aggregator.ShouldBeOfType<UserDefinedResponseAggregator>();        
        }
    }
}