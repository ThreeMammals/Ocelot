using Ocelot.Configuration.Creator;
using Ocelot.DependencyInjection;
using Ocelot.Multiplexer;
using Ocelot.UnitTests.DependencyInjection;

namespace Ocelot.UnitTests.Multiplexer;

public class ServiceCollectionExtensionsTests : DepInjectionUnitTest
{
    [Fact]
    public void AddOcelotAggregation_Default_RegistersDefaultImplementations()
    {
        // Arrange, Act
        Services.AddOcelotAggregation();

        // Assert
        AssertSingleton<IAggregatesCreator, AggregatesCreator>();
        AssertSingleton<IResponseAggregator, SimpleJsonResponseAggregator>();
        AssertSingleton<IResponseAggregatorFactory, InMemoryResponseAggregatorFactory>();
        AssertSingleton<IDefinedAggregatorProvider, ServiceLocatorDefinedAggregatorProvider>();
    }

    [Fact]
    public void AddOcelotAggregation_GenericCreator_RegistersCustomCreator()
    {
        // Arrange, Act
        Services.AddOcelotAggregation<FakeAggregatesCreator>();

        // Assert
        AssertSingleton<IResponseAggregator, SimpleJsonResponseAggregator>();
        AssertSingleton<IResponseAggregatorFactory, InMemoryResponseAggregatorFactory>();
        AssertSingleton<IDefinedAggregatorProvider, ServiceLocatorDefinedAggregatorProvider>();
        AssertSingleton<IAggregatesCreator, FakeAggregatesCreator>(); // !!!
    }

    public class FakeAggregatesCreator : AggregatesCreator
    {
        public FakeAggregatesCreator(IUpstreamTemplatePatternCreator creator, IUpstreamHeaderTemplatePatternCreator headerCreator)
            : base(creator, headerCreator) { }
    }
}
