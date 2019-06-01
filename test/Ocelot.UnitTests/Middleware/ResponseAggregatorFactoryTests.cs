namespace Ocelot.UnitTests.Middleware
{
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.Middleware.Multiplexer;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class ResponseAggregatorFactoryTests
    {
        private readonly InMemoryResponseAggregatorFactory _factory;
        private Mock<IDefinedAggregatorProvider> _provider;
        private ReRoute _reRoute;
        private IResponseAggregator _aggregator;

        public ResponseAggregatorFactoryTests()
        {
            _provider = new Mock<IDefinedAggregatorProvider>();
            _aggregator = new SimpleJsonResponseAggregator();
            _factory = new InMemoryResponseAggregatorFactory(_provider.Object, _aggregator);
        }

        [Fact]
        public void should_return_simple_json_aggregator()
        {
            var reRoute = new ReRouteBuilder()
                .Build();

            this.Given(_ => GivenReRoute(reRoute))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheAggregatorIs<SimpleJsonResponseAggregator>())
                .BDDfy();
        }

        [Fact]
        public void should_return_user_defined_aggregator()
        {
            var reRoute = new ReRouteBuilder()
                .WithAggregator("doesntmatter")
                .Build();

            this.Given(_ => GivenReRoute(reRoute))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheAggregatorIs<UserDefinedResponseAggregator>())
                .BDDfy();
        }

        private void GivenReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void WhenIGet()
        {
            _aggregator = _factory.Get(_reRoute);
        }

        private void ThenTheAggregatorIs<T>()
        {
            _aggregator.ShouldBeOfType<T>();
        }
    }
}
