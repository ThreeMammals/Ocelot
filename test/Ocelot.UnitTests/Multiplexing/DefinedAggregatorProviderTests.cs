using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Builder;
using Ocelot.Multiplexer;
using static Ocelot.UnitTests.Multiplexing.UserDefinedResponseAggregatorTests;

namespace Ocelot.UnitTests.Multiplexing;

public class DefinedAggregatorProviderTests : UnitTest
{
    private ServiceLocatorDefinedAggregatorProvider _provider;

    [Fact]
    public void Should_find_aggregator()
    {
        // Arrange
        var route = new RouteBuilder()
            .WithAggregator("TestDefinedAggregator")
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IDefinedAggregator, TestDefinedAggregator>();
        var services = serviceCollection.BuildServiceProvider(true);
        _provider = new ServiceLocatorDefinedAggregatorProvider(services);

        // Act
        var aggregator = _provider.Get(route);

        // Assert
        aggregator.Data.ShouldNotBeNull();
        aggregator.Data.ShouldBeOfType<TestDefinedAggregator>();
        aggregator.IsError.ShouldBeFalse();
    }

    [Fact]
    public void Should_not_find_aggregator()
    {
        // Arrange
        var route = new RouteBuilder()
            .WithAggregator("TestDefinedAggregator")
            .Build();

        // Arrange: Given No Defined Aggregator
        var serviceCollection = new ServiceCollection();
        var services = serviceCollection.BuildServiceProvider(true);
        _provider = new ServiceLocatorDefinedAggregatorProvider(services);

        // Act
        var aggregator = _provider.Get(route);

        // Assert
        aggregator.IsError.ShouldBeTrue();
        aggregator.Errors[0].Message.ShouldBe("Could not find Aggregator: TestDefinedAggregator");
        aggregator.Errors[0].ShouldBeOfType<CouldNotFindAggregatorError>();
    }
}
