using Microsoft.AspNetCore.Http;
using Moq.Protected;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace Ocelot.UnitTests.Multiplexing;

public class MultiplexingMiddlewareTests : UnitTest
{
    private MultiplexingMiddleware _middleware;
    private Ocelot.DownstreamRouteFinder.DownstreamRouteHolder _downstreamRoute;
    private int _count;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<IResponseAggregatorFactory> factory;
    private readonly Mock<IResponseAggregator> aggregator;
    private readonly Mock<IOcelotLoggerFactory> loggerFactory;
    private readonly Mock<IOcelotLogger> logger;

    public MultiplexingMiddlewareTests()
    {
        _httpContext = new DefaultHttpContext();
        factory = new Mock<IResponseAggregatorFactory>();
        aggregator = new Mock<IResponseAggregator>();
        factory.Setup(x => x.Get(It.IsAny<Route>())).Returns(aggregator.Object);
        loggerFactory = new Mock<IOcelotLoggerFactory>();
        logger = new Mock<IOcelotLogger>();
        loggerFactory.Setup(x => x.CreateLogger<MultiplexingMiddleware>()).Returns(logger.Object);
        _middleware = new MultiplexingMiddleware(Next, loggerFactory.Object, factory.Object);
    }

    private Task Next(HttpContext context) => Task.FromResult(_count++);

    [Fact]
    public async Task Should_multiplex()
    {
        var route = GivenDefaultRoute(2);
        GivenTheFollowing(route);

        // Act
        await _middleware.Invoke(_httpContext);
        _count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_not_multiplex()
    {
        var route = new Route(new DownstreamRouteBuilder().Build());
        GivenTheFollowing(route);

        // Act
        await _middleware.Invoke(_httpContext);
        _count.ShouldBe(1);
    }

    [Fact]
    [Trait("Bug", "1396")]
    public async Task CreateThreadContextAsync_CopyUser_ToTarget()
    {
        var route = new DownstreamRouteBuilder().Build();

        // Arrange
        GivenUser("test", "Copy", TestName());

        // Act
        var method = _middleware.GetType().GetMethod("CreateThreadContextAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var actual = await (Task<HttpContext>)method.Invoke(_middleware, new object[] { _httpContext, route });

        // Assert
        AssertUsers(actual);
    }

    [Fact]
    [Trait("Bug", "1396")]
    public async Task Invoke_ContextUser_ForwardedToDownstreamContext()
    {
        // Create
        HttpContext actualContext = null;
        _middleware = new MultiplexingMiddleware(NextMe, loggerFactory.Object, factory.Object);
        Task NextMe(HttpContext context)
        {
            actualContext = context;
            return Next(context);
        }

        // Arrange
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenDefaultRoute(2));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        _count.ShouldBe(2);
        AssertUsers(actualContext);
    }

    [Fact]
    [Trait("PR", "1826")]
    public async Task Should_Not_Copy_Context_If_One_Downstream_Route()
    {
        _middleware = new MultiplexingMiddleware(NextMe, loggerFactory.Object, factory.Object);
        Task NextMe(HttpContext context)
        {
            Assert.Equal(_httpContext, context);
            return Next(context);
        }

        // Arrange
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenDefaultRoute(1));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.Equal(1, _count);
    }

    [Fact]
    [Trait("PR", "1826")]
    public async Task Should_Call_ProcessSingleRoute_Once_If_One_Downstream_Route()
    {
        var mock = MockMiddlewareFactory(null, null);

        _middleware = mock.Object;

        // Arrange
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenDefaultRoute(1));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Once(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<DownstreamRoute>());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [Trait("PR", "1826")]
    public async Task Should_Not_Call_ProcessSingleRoute_If_More_Than_One_Downstream_Route(int routesCount)
    {
        var mock = MockMiddlewareFactory(null, null);

        // Arrange
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenDefaultRoute(routesCount));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Never(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<DownstreamRoute>());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [Trait("PR", "1826")]
    public async Task Should_Create_As_Many_Contexts_As_Routes_And_Map_Is_Called_Once(int routesCount)
    {
        var mock = MockMiddlewareFactory(routesCount, null);

        // Arrange
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenDefaultRoute(routesCount));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        mock.Protected().Verify<Task>(nameof(MapAsync), Times.Once(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<Route>(), ItExpr.Is<List<HttpContext>>(list => list.Count == routesCount));
    }

    [Fact]
    [Trait("PR", "1826")]
    public async Task Should_Not_Call_ProcessSingleRoute_Or_Map_If_No_Route()
    {
        var mock = MockMiddlewareFactory(null, null);

        // Arrange
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenDefaultRoute(0));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Never(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<DownstreamRoute>());

        mock.Protected().Verify<Task>(nameof(MapAsync), Times.Never(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<Route>(), ItExpr.IsAny<List<HttpContext>>());
    }

    [Theory]
    [Trait("Bug", "2039")]
    [InlineData(1)] // Times.Never()
    [InlineData(2)] // Times.Exactly(2)
    [InlineData(3)] // Times.Exactly(3)
    [InlineData(4)] // Times.Exactly(4)
    public async Task Should_Call_CloneRequestBodyAsync_Each_Time_Per_Requests(int numberOfRoutes)
    {
        // Arrange
        var mock = MockMiddlewareFactory(null, null);
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenDefaultRoute(numberOfRoutes));

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        mock.Protected().Verify<Task<Stream>>("CloneRequestBodyAsync",
            numberOfRoutes > 1 ? Times.Exactly(numberOfRoutes) : Times.Never(),
            ItExpr.IsAny<HttpRequest>(), ItExpr.IsAny<DownstreamRoute>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    [Trait("PR", "1826")]
    public async Task If_Using_3_Routes_WithAggregator_ProcessSingleRoute_Is_Never_Called_Map_Once_And_Pipeline_3_Times()
    {
        var mock = MockMiddlewareFactory(null, AggregateRequestDelegateFactory());

        // Arrange
        GivenUser("test", "Invoke", TestName());
        GivenTheFollowing(GivenRoutesWithAggregator());

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Never(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<DownstreamRoute>());
        mock.Protected().Verify<Task>(nameof(MapAsync), Times.Once(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<Route>(), ItExpr.IsAny<List<HttpContext>>());
        Assert.Equal(3, _count);
    }

    [Fact]
    [Trait("PR", "1826")]
    [Trait("PR", "2328")]
    public async Task Invoke_When_ProcessRoutesWithRouteKeysAsync_ReturnedEmptyList_ShouldEarlyExitWithoutMultiplexing()
    {
        // Arrange
        var mock = MockMiddlewareFactory(null, UsersResponder);
        var route = new Route
        {
            DownstreamRoute =
            [
                new DownstreamRouteBuilder().WithKey("comments").Build(),
                new DownstreamRouteBuilder().WithKey("user").Build()
            ],
            DownstreamRouteConfig =
            [
                new("invalid", "$[*].userId", "userId")
            ],
            Aggregator = "TestAggregator",
        };
        GivenTheFollowing(route);

        HttpContext[] empty = [];
        mock.Protected()
            .Setup<Task<HttpContext[]>>("ProcessRoutesWithRouteKeysAsync",
                ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<IEnumerable<DownstreamRoute>>(), ItExpr.IsAny<IReadOnlyCollection<AggregateRouteConfig>>(), ItExpr.IsAny<HttpContext>())
            .ReturnsAsync(empty);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.Equal(1, _count); // Next was called (no multiplexing)
        mock.Protected().Verify<Task>(nameof(MapAsync), Times.Never(), // MapResponsesAsync was not called
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<Route>(), ItExpr.IsAny<List<HttpContext>>());
    }

    [Fact]
    [Trait("PR", "1826")]
    [Trait("PR", "2328")]
    public async Task Invoke_When_Invalid_ComplexAggKey_ProcessRoutesWithRouteKeysAsync_Should_Call_Users_Twice_And_MapTheirResponses()
    {
        // Arrange
        var mock = MockMiddlewareFactory(null, UsersResponder);
        var route = new Route
        {
            DownstreamRoute =
            [
                new DownstreamRouteBuilder().WithKey("comments").Build(),
                new DownstreamRouteBuilder().WithKey("user").Build()
            ],
            DownstreamRouteConfig =
            [
                new("invalid", "$[*].userId", "userId")
            ],
            Aggregator = "TestAggregator",
        };
        GivenTheFollowing(route);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.Equal(2, _count); // 2 calls of UsersResponder
        mock.Protected().Verify<Task>(nameof(MapAsync), Times.Once(), // MapResponsesAsync was called
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<Route>(), ItExpr.IsAny<List<HttpContext>>());
    }

    private Task UsersResponder(HttpContext context)
    {
        var json = @"[{""userId"":1},{""userId"":2}]";
        context.Items.Add("DownstreamResponse",
            new DownstreamResponse(new StringContent(json, Encoding.UTF8, "application/json"),
                HttpStatusCode.OK, new List<Header>(), "test"));
        if (!context.Items.ContainsKey("TemplatePlaceholderNameAndValues"))
            context.Items.Add("TemplatePlaceholderNameAndValues", new List<PlaceholderNameAndValue>());
        _count++;
        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Bug", "2248")]
    [Trait("PR", "2328")]
    public async Task Should_expand_jsonpath_array_into_multiple_parameterized_calls()
    {
        // Arrange
        var mock = MockMiddlewareFactory(null, UsersResponder);
        var route = new Route
        {
            DownstreamRoute =
            [
                new DownstreamRouteBuilder().WithKey("comments").Build(),
                new DownstreamRouteBuilder().WithKey("user").Build()
            ],
            DownstreamRouteConfig =
            [
                new("user", "$[*].userId", "userId")
            ],
            Aggregator = "TestAggregator",
        };
        GivenTheFollowing(route);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.Equal(3, _count);
        mock.Protected().Verify<Task>(nameof(MapAsync), Times.Once(),
            ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<Route>(), ItExpr.Is<List<HttpContext>>(list => list.Count == 3));
    }
    
    [Fact]
    [Trait("Bug", "2248")]
    [Trait("PR", "2328")]
    public async Task Should_verify_each_context_has_aggregate_key()
    {
        // Arrange
        var route = GivenRoutesWithAggregator();
        GivenTheFollowing(route);
        _middleware = new MultiplexingMiddleware(AggregateRequestDelegateFactory(), loggerFactory.Object, factory.Object);

        // Act
        await _middleware.Invoke(_httpContext);

        // Assert
        Assert.Equal(3, _count);
        aggregator.Verify(
            a => a.Aggregate(It.IsAny<Route>(), It.IsAny<HttpContext>(), It.IsAny<List<HttpContext>>()),
            Times.Once());
    }

    private MethodInfo MapAsync() => _middleware.GetType().GetMethod(nameof(MapAsync), BindingFlags.NonPublic | BindingFlags.Instance);

    [Fact]
    [Trait("PR", "1826")]
    [Trait("PR", "2328")]
    public async Task MapAsync_Should_Return_CompletedTask_When_Only_One_DownstreamRoute()
    {
        // Arrange
        var route = GivenDefaultRoute(1); // 1 downstream route
        var contexts = new List<HttpContext> { new DefaultHttpContext() };

        // Act
        var actual = (Task)MapAsync().Invoke(_middleware, [_httpContext, route, contexts]);

        // Assert
        Assert.True(actual.IsCompleted);
        Assert.Same(Task.CompletedTask, actual);
    }

    [Theory]
    [Trait("PR", "2328")]
    [InlineData(0, 2)]   // contexts.Count == 0 → no loop iterations
    [InlineData(2, 0)]   // DownstreamRouteConfig.Count == 0 → skip if
    [InlineData(2, 3)]   // contexts < config → loop runs 2 times
    [InlineData(3, 2)]   // contexts > config → loop runs 2 times (short-circuit on i < config.Count)
    public async Task MapAsync_Should_Set_AggregateKeys_Correctly_For_Different_Counts(int contextsCount, int configCount)
    {
        // Arrange
        var from = GivenRoutesWithAggregator(); // base with config
        var route = new Route()
        {
            DownstreamRoute = [.. from.DownstreamRoute],
            DownstreamRouteConfig = [.. Enumerable.Range(0, configCount)
                .Select(i => new AggregateRouteConfig($"key{i}", "$[*].id", $"param{i}"))
            ],
            Aggregator = from.Aggregator,
        };
        var contexts = Enumerable.Range(0, contextsCount)
            .Select(_ => new DefaultHttpContext() as HttpContext)
            .ToList();

        // Act
        var actual = (Task)MapAsync().Invoke(_middleware, [_httpContext, route, contexts]);

        // Assert - verify keys were set where expected
        Assert.NotNull(actual);
        for (int i = 0; i < Math.Min(contextsCount, configCount); i++)
        {
            contexts[i].Items[MultiplexingMiddleware.CurrentAggregateRouteKeyItem].ShouldBe($"key{i}");
        }
    }

    private RequestDelegate AggregateRequestDelegateFactory()
    {
        return context =>
        {
            var responseContent = @"[{""id"":1,""writerId"":1,""postId"":2,""text"":""text1""},{""id"":2,""writerId"":1,""postId"":2,""text"":""text2""}]";
            context.Items.Add("DownstreamResponse", new DownstreamResponse(new StringContent(responseContent, Encoding.UTF8, "application/json"), HttpStatusCode.OK, new List<Header>(), "test"));

            if (!context.Items.ContainsKey("TemplatePlaceholderNameAndValues"))
            {
                context.Items.Add("TemplatePlaceholderNameAndValues", new List<PlaceholderNameAndValue>());
            }

            _count++;
            return Task.CompletedTask;
        };
    }

    private Mock<MultiplexingMiddleware> MockMiddlewareFactory(int? downstreamRoutesCount, RequestDelegate requestDelegate)
    {
        requestDelegate ??= Next;
        var mock = new Mock<MultiplexingMiddleware>(requestDelegate, loggerFactory.Object, factory.Object) { CallBase = true };
        mock.Protected().Setup<Task>(nameof(MapAsync), ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<Route>(),
            downstreamRoutesCount == null ? ItExpr.IsAny<List<HttpContext>>() : ItExpr.Is<List<HttpContext>>(list => list.Count == downstreamRoutesCount))
            .Returns(Task.CompletedTask).Verifiable();
        mock.Protected().Setup<Task>("ProcessSingleRouteAsync", ItExpr.IsAny<HttpContext>(), ItExpr.IsAny<DownstreamRoute>())
            .Returns(Task.CompletedTask).Verifiable();
        _middleware = mock.Object;
        return mock;
    }

    private void GivenUser(string authentication, string name, string role)
    {
        var user = new ClaimsPrincipal();
        user.AddIdentity(new(authentication, name, role));
        _httpContext.User = user;
    }

    private void AssertUsers(HttpContext actual)
    {
        Assert.NotNull(actual);
        Assert.Same(_httpContext.User, actual.User);
        Assert.NotNull(actual.User.Identity);
        var identity = _httpContext.User.Identity as ClaimsIdentity;
        var actualIdentity = actual.User.Identity as ClaimsIdentity;
        Assert.Equal(identity.AuthenticationType, actualIdentity.AuthenticationType);
        Assert.Equal(identity.NameClaimType, actualIdentity.NameClaimType);
        Assert.Equal(identity.RoleClaimType, actualIdentity.RoleClaimType);
    }

    private static Route GivenDefaultRoute(int count)
    {
        var r = new Route();
        for (var i = 0; i < count; i++)
        {
            r.DownstreamRoute.Add(new DownstreamRouteBuilder().Build());
        }

        return r;
    }

    private static Route GivenRoutesWithAggregator()
    {
        var route1 = new DownstreamRouteBuilder().WithKey("Comments").Build();
        var route2 = new DownstreamRouteBuilder().WithKey("UserDetails").Build();
        var route3 = new DownstreamRouteBuilder().WithKey("PostDetails").Build();

        return new Route()
        {
            DownstreamRoute = [route1, route2, route3],
            DownstreamRouteConfig = [
                new("UserDetails", "$[*].writerId", "userId"),
                new("PostDetails", "$[*].postId", "postId"),
            ],
            Aggregator = "TestAggregator",
        };
    }

    private void GivenTheFollowing(Route route)
    {
        _downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route);
        _httpContext.Items.UpsertDownstreamRoute(_downstreamRoute);
    }
}
