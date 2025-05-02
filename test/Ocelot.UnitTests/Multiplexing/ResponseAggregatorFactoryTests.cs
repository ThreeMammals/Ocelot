using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Multiplexer;

namespace Ocelot.UnitTests.Multiplexing;

public class ResponseAggregatorFactoryTests : UnitTest
{
    private readonly InMemoryResponseAggregatorFactory _factory;
    private readonly Mock<IDefinedAggregatorProvider> _provider;
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
        // Arrange
        var route = new RouteBuilder().Build();

        // Act
        _aggregator = _factory.Get(route);

        // Assert
        _aggregator.ShouldBeOfType<SimpleJsonResponseAggregator>();
    }

    [Fact]
    public void Should_return_user_defined_aggregator()
    {
        // Arrange
        var route = new RouteBuilder()
            .WithAggregator("doesntmatter")
            .Build();

        // Act
        _aggregator = _factory.Get(route);

        // Assert
        _aggregator.ShouldBeOfType<UserDefinedResponseAggregator>();
    }
}
