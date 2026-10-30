using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Values;
using System.Reflection;

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
                    RouteKeys = new(["key1"]),
                },
            },
        };
        _routes = [];

        // Act
        _result = _creator.Create(_fileConfiguration, _routes).ToList();

        // Assert
        _utpCreator.Verify(x => x.Create(It.IsAny<FileAggregateRoute>()), Times.Never);
        _result.ShouldNotBeNull().Count().ShouldBe(0); // empty result
    }

    [Fact]
    [Trait("Bug", "597")]
    [Trait("Feat", "600")]
    public void Create_TwoAggregateRoutes_HappyPath()
    {
        _fileConfiguration = new FileConfiguration
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
        _routes =
        [
            new(new DownstreamRouteBuilder().WithKey("key1").Build()),
            new(new DownstreamRouteBuilder().WithKey("key2").Build()),
            new(new DownstreamRouteBuilder().WithKey("key3").Build()),
            new(new DownstreamRouteBuilder().WithKey("key4").Build()),
        ];
        GivenTheUtpCreatorReturns();
        GivenTheUhtpCreatorReturns();

        // Act
        _result = _creator.Create(_fileConfiguration, _routes).ToList();

        // Assert
        ThenTheUtpCreatorIsCalledCorrectly();

        // Assert: then the aggregates are created
        _result.ShouldNotBeNull().Count().ShouldBe(2);
        AssertResultByIndex(0);
        AssertResultByIndex(1);
    }

    [Theory]
    [Trait("Feat", "1389")]
    [InlineData(nameof(HttpMethod.Get))]
    [InlineData(nameof(HttpMethod.Post))]
    public void SetUpAggregateRoute_NoUpstreamHttpMethod_DefaultVerbIsAssigned(string httpVerb)
    {
        // Arrange
        _fileConfiguration = new FileConfiguration
        {
            Aggregates =
            [
                new()
                {
                    RouteKeys = new(["key1", "key2"]),
                },
                new()
                {
                    RouteKeys = new(["key3", "key4"]),
                    UpstreamHttpMethod = [httpVerb], // wanted verb
                },
            ],
        };
        _routes =
        [
            new(new DownstreamRouteBuilder().WithKey("key1").Build()),
            new(new DownstreamRouteBuilder().WithKey("key2").Build()),
            new(new DownstreamRouteBuilder().WithKey("key3").Build()),
            new(new DownstreamRouteBuilder().WithKey("key4").Build()),
        ];
        GivenTheUtpCreatorReturns();
        GivenTheUhtpCreatorReturns();

        // Act
        _result = _creator.Create(_fileConfiguration, _routes).ToList();

        // Assert
        ThenTheUtpCreatorIsCalledCorrectly();
        _result.ShouldNotBeNull().Count.ShouldBe(2);
        AssertRoute(0, HttpMethod.Get); // default verb scenario
        AssertRoute(1, new HttpMethod(httpVerb)); // // wanted verb scenario
    }

    [Theory]
    [Trait("Feat", "1389")]
    [InlineData(new string[] { }, new string[] { }, new[] { "GET" })] // no route and no global options -> default GET
    [InlineData(new string[] { }, new[] { "POST" }, new[] { "POST" })] // no route option, global option used
    [InlineData(new[] { "PUT" }, new[] { "POST" }, new[] { "POST", "PUT" })] // both route and global options merged
    public void CreateUpstreamHttpMethod_ShouldUseDefaultGlobalOrMergedMethods(string[] routeMethods, string[] globalMethods, string[] expected)
    {
        // Arrange
        var route = new FileAggregateRoute { UpstreamHttpMethod = [.. routeMethods] };
        var global = new FileGlobalConfiguration { UpstreamHttpMethod = [.. globalMethods] };

        // Act
        var actual = CreateUpstreamHttpMethod(route, global);

        // Assert
        actual.Select(x => x.Method).OrderBy(x => x, StringComparer.Ordinal).ToArray()
            .ShouldBe(expected.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    private void AssertRoute(int i, HttpMethod expected)
    {
        _result[i].UpstreamHttpMethod.ShouldContain(x => x == expected);
        AssertResultByIndex(i, expected);
    }

    private void AssertResultByIndex(int i, HttpMethod method = null)
    {
        method ??= HttpMethod.Get; // DefaultHttpMethod
        _result[i].UpstreamHttpMethod.ShouldContain(x => x == method);
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
        _aggregateUtp =
        [
            new UpstreamPathTemplateBuilder().Build(),
            new UpstreamPathTemplateBuilder().Build(),
        ];
        _utpCreator.SetupSequence(x => x.Create(It.IsAny<IRouteUpstream>()))
            .Returns(_aggregateUtp[0])
            .Returns(_aggregateUtp[1]);
    }

    private void GivenTheUhtpCreatorReturns()
    {
        _headerTemplates =
        [
            [],
            [],
        ];
        _uhtpCreator.SetupSequence(x => x.Create(It.IsAny<IRouteUpstream>()))
            .Returns(_headerTemplates[0])
            .Returns(_headerTemplates[1]);
    }

    private MethodInfo CreateUpstreamHttpMethod() => _creator.GetType().GetMethod(nameof(CreateUpstreamHttpMethod), BindingFlags.NonPublic | BindingFlags.Instance);
    private HashSet<HttpMethod> CreateUpstreamHttpMethod(FileAggregateRoute route, FileGlobalConfiguration global)
        => (HashSet<HttpMethod>)CreateUpstreamHttpMethod().Invoke(_creator, [route, global]);
}
