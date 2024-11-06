using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Values;

namespace Ocelot.UnitTests.Configuration;

public class AggregatesCreatorTests : UnitTest
{
    private readonly AggregatesCreator _creator;
    private readonly Mock<IUpstreamTemplatePatternCreator> _utpCreator;
    private readonly Mock<IUpstreamHeaderTemplatePatternCreator> _uhtpCreator;
    private FileConfiguration _fileConfiguration;
    private List<Route> _routes;
    private List<Route> _result;
    private UpstreamPathTemplate[] _aggregateUtp;
    private Dictionary<string, UpstreamHeaderTemplate>[] _headerTemplates;

    public AggregatesCreatorTests()
    {
        _utpCreator = new Mock<IUpstreamTemplatePatternCreator>();
        _uhtpCreator = new Mock<IUpstreamHeaderTemplatePatternCreator>();
        _creator = new AggregatesCreator(_utpCreator.Object, _uhtpCreator.Object);
    }

    [Fact]
    [Trait("Bug", "597")]
    [Trait("Feat", "600")]
    public void Create_NoRoutes_NoAggregates()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration
        {
            Aggregates = new List<FileAggregateRoute>
            {
                new()
                {
                    RouteKeys = new List<string>{"key1"},
                },
            },
        };
        _routes = new List<Route>();

        // Act
        _result = _creator.Create(_fileConfiguration, _routes);

        // Assert
        _utpCreator.Verify(x => x.Create(It.IsAny<FileAggregateRoute>()), Times.Never);
        _result.ShouldNotBeNull().Count.ShouldBe(0); // empty result
    }

    [Fact]
    [Trait("Bug", "597")]
    [Trait("Feat", "600")]
    public void Create_TwoAggregateRoutes_HappyPath()
    {
        // Arrange
        _fileConfiguration = new FileConfiguration
        {
            Aggregates = new List<FileAggregateRoute>
            {
                new()
                {
                    RouteKeys = new List<string>{"key1", "key2"},
                    UpstreamHost = "hosty",
                    UpstreamPathTemplate = "templatey",
                    Aggregator = "aggregatory",
                    RouteIsCaseSensitive = true,
                },
                new()
                {
                    RouteKeys = new List<string>{"key3", "key4"},
                    UpstreamHost = "hosty",
                    UpstreamPathTemplate = "templatey",
                    Aggregator = "aggregatory",
                    RouteIsCaseSensitive = true,
                },
            },
        };
        _routes = new List<Route>
        {
            new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().WithKey("key1").Build()).Build(),
            new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().WithKey("key2").Build()).Build(),
            new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().WithKey("key3").Build()).Build(),
            new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().WithKey("key4").Build()).Build(),
        };
        GivenTheUtpCreatorReturns();
        GivenTheUhtpCreatorReturns();

        // Act
        _result = _creator.Create(_fileConfiguration, _routes);

        // Assert
        ThenTheUtpCreatorIsCalledCorrectly();

        // Assert: then the aggregates are created
        _result.ShouldNotBeNull().Count.ShouldBe(2);
        AssertResultByIndex(0);
        AssertResultByIndex(1);
    }

    private void AssertResultByIndex(int i)
    {
        _result[i].UpstreamHttpMethod.ShouldContain(x => x == HttpMethod.Get);
        _result[i].UpstreamHost.ShouldBe(_fileConfiguration.Aggregates[i].UpstreamHost);
        _result[i].UpstreamTemplatePattern.ShouldBe(_aggregateUtp[i]);
        _result[i].UpstreamHeaderTemplates.ShouldBe(_headerTemplates[i]);
        _result[i].Aggregator.ShouldBe(_fileConfiguration.Aggregates[i].Aggregator);
        _result[i].DownstreamRoute.ShouldContain(x => x == _routes[2 * i].DownstreamRoute[0]);
        _result[i].DownstreamRoute.ShouldContain(x => x == _routes[(2 * i) + 1].DownstreamRoute[0]);
    }

    private void ThenTheUtpCreatorIsCalledCorrectly()
    {
        _utpCreator.Verify(x => x.Create(_fileConfiguration.Aggregates[0]), Times.Once);
        _utpCreator.Verify(x => x.Create(_fileConfiguration.Aggregates[1]), Times.Once);
    }

    private void GivenTheUtpCreatorReturns()
    {
        _aggregateUtp = new[]
        {
            new UpstreamPathTemplateBuilder().Build(),
            new UpstreamPathTemplateBuilder().Build(),
        };
        _utpCreator.SetupSequence(x => x.Create(It.IsAny<IRoute>()))
            .Returns(_aggregateUtp[0])
            .Returns(_aggregateUtp[1]);
    }

    private void GivenTheUhtpCreatorReturns()
    {
        _headerTemplates = new[]
        {
            new Dictionary<string, UpstreamHeaderTemplate>(),
            new Dictionary<string, UpstreamHeaderTemplate>(),
        };
        _uhtpCreator.SetupSequence(x => x.Create(It.IsAny<IRoute>()))
            .Returns(_headerTemplates[0])
            .Returns(_headerTemplates[1]);
    }
}
