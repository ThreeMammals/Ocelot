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
    private UpstreamPathTemplate _aggregate1Utp;
    private UpstreamPathTemplate _aggregate2Utp;
    private Dictionary<string, UpstreamHeaderTemplate> _headerTemplates1;
    private Dictionary<string, UpstreamHeaderTemplate> _headerTemplates2;

    public AggregatesCreatorTests()
    {
        _utpCreator = new Mock<IUpstreamTemplatePatternCreator>();
        _uhtpCreator = new Mock<IUpstreamHeaderTemplatePatternCreator>();
        _creator = new AggregatesCreator(_utpCreator.Object, _uhtpCreator.Object);
    }

    [Fact]
    public void Should_return_no_aggregates()
    {
        // Arrange
        var fileConfig = new FileConfiguration
        {
            Aggregates = new List<FileAggregateRoute>
            {
                new()
                {
                    RouteKeys = new(["key1"]),
                },
            },
        };
        var routes = new List<Route>();
        GivenThe(fileConfig);
        GivenThe(routes);

        // Act
        WhenICreate();

        // Assert
        TheUtpCreatorIsNotCalled();
        ThenTheResultIsNotNull();
        ThenTheResultIsEmpty();
    }

    [Fact]
    public void Should_create_aggregates()
    {
        // Arrange
        var fileConfig = new FileConfiguration
        {
            Aggregates = new List<FileAggregateRoute>
            {
                new()
                {
                    RouteKeys = new(["key1", "key2"]),
                    UpstreamHost = "hosty",
                    UpstreamPathTemplate = "templatey",
                    Aggregator = "aggregatory",
                    RouteIsCaseSensitive = true,
                },
                new()
                {
                    RouteKeys = new(["key3", "key4"]),
                    UpstreamHost = "hosty",
                    UpstreamPathTemplate = "templatey",
                    Aggregator = "aggregatory",
                    RouteIsCaseSensitive = true,
                },
            },
        };
        var routes = new List<Route>
        {
            new(new DownstreamRouteBuilder().WithKey("key1").Build()),
            new(new DownstreamRouteBuilder().WithKey("key2").Build()),
            new(new DownstreamRouteBuilder().WithKey("key3").Build()),
            new(new DownstreamRouteBuilder().WithKey("key4").Build()),
        };

        GivenThe(fileConfig);
        GivenThe(routes);
        GivenTheUtpCreatorReturns();
        GivenTheUhtpCreatorReturns();

        // Act
        WhenICreate();

        // Assert
        ThenTheUtpCreatorIsCalledCorrectly();
        ThenTheAggregatesAreCreated();
    }

    private void ThenTheAggregatesAreCreated()
    {
        _result.ShouldNotBeNull();
        _result.Count.ShouldBe(2);

        _result[0].UpstreamHttpMethod.ShouldContain(x => x == HttpMethod.Get);
        _result[0].UpstreamHost.ShouldBe(_fileConfiguration.Aggregates[0].UpstreamHost);
        _result[0].UpstreamTemplatePattern.ShouldBe(_aggregate1Utp);
        _result[0].UpstreamHeaderTemplates.ShouldBe(_headerTemplates1);
        _result[0].Aggregator.ShouldBe(_fileConfiguration.Aggregates[0].Aggregator);
        _result[0].DownstreamRoute.ShouldContain(x => x == _routes[0].DownstreamRoute[0]);
        _result[0].DownstreamRoute.ShouldContain(x => x == _routes[1].DownstreamRoute[0]);

        _result[1].UpstreamHttpMethod.ShouldContain(x => x == HttpMethod.Get);
        _result[1].UpstreamHost.ShouldBe(_fileConfiguration.Aggregates[1].UpstreamHost);
        _result[1].UpstreamTemplatePattern.ShouldBe(_aggregate2Utp);
        _result[1].UpstreamHeaderTemplates.ShouldBe(_headerTemplates2);
        _result[1].Aggregator.ShouldBe(_fileConfiguration.Aggregates[1].Aggregator);
        _result[1].DownstreamRoute.ShouldContain(x => x == _routes[2].DownstreamRoute[0]);
        _result[1].DownstreamRoute.ShouldContain(x => x == _routes[3].DownstreamRoute[0]);
    }

    private void ThenTheUtpCreatorIsCalledCorrectly()
    {
        _utpCreator.Verify(x => x.Create(_fileConfiguration.Aggregates[0]), Times.Once);
        _utpCreator.Verify(x => x.Create(_fileConfiguration.Aggregates[1]), Times.Once);
    }

    private void GivenTheUtpCreatorReturns()
    {
        _aggregate1Utp = new UpstreamPathTemplateBuilder().Build();
        _aggregate2Utp = new UpstreamPathTemplateBuilder().Build();

        _utpCreator.SetupSequence(x => x.Create(It.IsAny<IRouteUpstream>()))
            .Returns(_aggregate1Utp)
            .Returns(_aggregate2Utp);
    }

    private void GivenTheUhtpCreatorReturns()
    {
        _headerTemplates1 = new Dictionary<string, UpstreamHeaderTemplate>();
        _headerTemplates2 = new Dictionary<string, UpstreamHeaderTemplate>();

        _uhtpCreator.SetupSequence(x => x.Create(It.IsAny<IRouteUpstream>()))
            .Returns(_headerTemplates1)
            .Returns(_headerTemplates2);
    }

    private void ThenTheResultIsEmpty()
    {
        _result.Count.ShouldBe(0);
    }

    private void ThenTheResultIsNotNull()
    {
        _result.ShouldNotBeNull();
    }

    private void TheUtpCreatorIsNotCalled()
    {
        _utpCreator.Verify(x => x.Create(It.IsAny<FileAggregateRoute>()), Times.Never);
    }

    private void GivenThe(FileConfiguration fileConfiguration)
    {
        _fileConfiguration = fileConfiguration;
    }

    private void GivenThe(List<Route> routes)
    {
        _routes = routes;
    }

    private void WhenICreate()
    {
        _result = _creator.Create(_fileConfiguration, _routes);
    }
}
