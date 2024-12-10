using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Multiplexer;

namespace Ocelot.UnitTests.Multiplexing;

public class ResponseAggregatorFactoryTests : UnitTest
{
    private readonly InMemoryResponseAggregatorFactory _factory;
    private readonly Mock<IDefinedAggregatorProvider> _provider;
    private Route _route;
    private IResponseAggregator _aggregator;

    public ResponseAggregatorFactoryTests()
    {
        _provider = new Mock<IDefinedAggregatorProvider>();
        _aggregator = new SimpleJsonResponseAggregator();
        _factory = new InMemoryResponseAggregatorFactory(_provider.Object, _aggregator);
    }

    [Fact]
    public void Should_return_simple_json_aggregator()
    {
        var route = new RouteBuilder()
            .Build();

        GivenRoute(route);
        WhenIGet();
        ThenTheAggregatorIs<SimpleJsonResponseAggregator>();
    }

    [Fact]
    public void Should_return_user_defined_aggregator()
    {
        var route = new RouteBuilder()
            .WithAggregator("doesntmatter")
            .Build();

        GivenRoute(route);
        WhenIGet();
        ThenTheAggregatorIs<UserDefinedResponseAggregator>();
    }

    private void GivenRoute(Route route)
    {
        _route = route;
    }

    private void WhenIGet()
    {
        _aggregator = _factory.Get(_route);
    }

    private void ThenTheAggregatorIs<T>()
    {
        _aggregator.ShouldBeOfType<T>();
    }
}
