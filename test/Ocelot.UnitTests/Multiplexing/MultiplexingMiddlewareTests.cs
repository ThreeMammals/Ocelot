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

namespace Ocelot.UnitTests.Multiplexing
{
    public class MultiplexingMiddlewareTests : UnitTest
    {
        private MultiplexingMiddleware _middleware;
        private Ocelot.DownstreamRouteFinder.DownstreamRouteHolder _downstreamRoute;
        private int _count;
        private readonly HttpContext _httpContext;
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
        public void should_multiplex()
        {
            var route = GivenDefaultRoute(2);
            this.Given(x => GivenTheFollowing(route))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(2))
                .BDDfy();
        }

        [Fact]
        public void should_not_multiplex()
        {
            var route = new RouteBuilder().WithDownstreamRoute(new DownstreamRouteBuilder().Build()).Build();

            this.Given(x => GivenTheFollowing(route))
                .When(x => WhenIMultiplex())
                .Then(x => ThePipelineIsCalled(1))
                .BDDfy();
        }

        [Fact]
        [Trait("Bug", "1396")]
        public async Task CreateThreadContextAsync_CopyUser_ToTarget()
        {
            // Arrange
            GivenUser("test", "Copy", nameof(CreateThreadContextAsync_CopyUser_ToTarget));

            // Act
            var method = _middleware.GetType().GetMethod("CreateThreadContextAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var actual = await (Task<HttpContext>)method.Invoke(_middleware, new object[] { _httpContext });

            // Assert
            AssertUsers(actual);
        }

        [Fact]
        [Trait("Bug", "1396")]
        public async Task Invoke_ContextUser_ForwardedToDownstreamContext()
        {
            // Setup
            HttpContext actualContext = null;
            _middleware = new MultiplexingMiddleware(NextMe, loggerFactory.Object, factory.Object);
            Task NextMe(HttpContext context)
            {
                actualContext = context;
                return Next(context);
            }

            // Arrange
            GivenUser("test", "Invoke", nameof(Invoke_ContextUser_ForwardedToDownstreamContext));
            GivenTheFollowing(GivenDefaultRoute(2));

            // Act
            await WhenIMultiplex();

            // Assert
            ThePipelineIsCalled(2);
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
            GivenUser("test", "Invoke", nameof(Should_Not_Copy_Context_If_One_Downstream_Route));
            GivenTheFollowing(GivenDefaultRoute(1));

            // Act
            await WhenIMultiplex();

            // Assert
            ThePipelineIsCalled(1);
        }

        [Fact]
        [Trait("PR", "1826")]
        public async Task Should_Call_ProcessSingleRoute_Once_If_One_Downstream_Route()
        {
            var mock = MockMiddlewareFactory(null, null);

            _middleware = mock.Object;

            // Arrange
            GivenUser("test", "Invoke", nameof(Should_Call_ProcessSingleRoute_Once_If_One_Downstream_Route));
            GivenTheFollowing(GivenDefaultRoute(1));

            // Act
            await WhenIMultiplex();

            // Assert
            mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Once(),
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<DownstreamRoute>());
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
            GivenUser("test", "Invoke", nameof(Should_Not_Call_ProcessSingleRoute_If_More_Than_One_Downstream_Route));
            GivenTheFollowing(GivenDefaultRoute(routesCount));

            // Act
            await WhenIMultiplex();

            // Assert
            mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Never(),
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<DownstreamRoute>());
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
            GivenUser("test", "Invoke", nameof(Should_Create_As_Many_Contexts_As_Routes_And_Map_Is_Called_Once));
            GivenTheFollowing(GivenDefaultRoute(routesCount));

            // Act
            await WhenIMultiplex();

            // Assert
            mock.Protected().Verify<Task>("MapAsync", Times.Once(),
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<Route>(),
                ItExpr.Is<List<HttpContext>>(list => list.Count == routesCount)
            );
        }

        [Fact]
        [Trait("PR", "1826")]
        public async Task Should_Not_Call_ProcessSingleRoute_Or_Map_If_No_Route()
        {
            var mock = MockMiddlewareFactory(null, null);

            // Arrange
            GivenUser("test", "Invoke", nameof(Should_Not_Call_ProcessSingleRoute_Or_Map_If_No_Route));
            GivenTheFollowing(GivenDefaultRoute(0));

            // Act
            await WhenIMultiplex();

            // Assert
            mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Never(),
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<DownstreamRoute>());

            mock.Protected().Verify<Task>("MapAsync", Times.Never(),
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<Route>(),
                ItExpr.IsAny<List<HttpContext>>());
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
            GivenUser("test", "Invoke", nameof(Should_Call_CloneRequestBodyAsync_Each_Time_Per_Requests));
            GivenTheFollowing(GivenDefaultRoute(numberOfRoutes));

            // Act
            await WhenIMultiplex();

            // Assert
            mock.Protected().Verify<Task<Stream>>("CloneRequestBodyAsync",
                numberOfRoutes > 1 ? Times.Exactly(numberOfRoutes) : Times.Never(),
                ItExpr.IsAny<HttpRequest>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        [Trait("PR", "1826")]
        public async Task If_Using_3_Routes_WithAggregator_ProcessSingleRoute_Is_Never_Called_Map_Once_And_Pipeline_3_Times()
        {
            var mock = MockMiddlewareFactory(null, AggregateRequestDelegateFactory());

            // Arrange
            GivenUser("test", "Invoke", nameof(If_Using_3_Routes_WithAggregator_ProcessSingleRoute_Is_Never_Called_Map_Once_And_Pipeline_3_Times));
            GivenTheFollowing(GivenRoutesWithAggregator());

            // Act
            await WhenIMultiplex();

            mock.Protected().Verify<Task>("ProcessSingleRouteAsync", Times.Never(),
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<DownstreamRoute>());

            mock.Protected().Verify<Task>("MapAsync", Times.Once(),
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<Route>(),
                ItExpr.IsAny<List<HttpContext>>());

            ThePipelineIsCalled(3);
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

            mock.Protected().Setup<Task>("MapAsync",
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<Route>(),
                downstreamRoutesCount == null ? ItExpr.IsAny<List<HttpContext>>() : ItExpr.Is<List<HttpContext>>(list => list.Count == downstreamRoutesCount)
            ).Returns(Task.CompletedTask).Verifiable();

            mock.Protected().Setup<Task>("ProcessSingleRouteAsync",
                ItExpr.IsAny<HttpContext>(),
                ItExpr.IsAny<DownstreamRoute>()
            ).Returns(Task.CompletedTask).Verifiable();

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
            var b = new RouteBuilder();
            for (var i = 0; i < count; i++)
            {
                b.WithDownstreamRoute(new DownstreamRouteBuilder().Build());
            }

            return b.Build();
        }

        private static Route GivenRoutesWithAggregator()
        {
            var route1 = new DownstreamRouteBuilder().WithKey("Comments").Build();
            var route2 = new DownstreamRouteBuilder().WithKey("UserDetails").Build();
            var route3 = new DownstreamRouteBuilder().WithKey("PostDetails").Build();

            var b = new RouteBuilder();
            b.WithDownstreamRoute(route1);
            b.WithDownstreamRoute(route2);
            b.WithDownstreamRoute(route3);

            b.WithAggregateRouteConfig(new()
            {
                new AggregateRouteConfig { RouteKey = "UserDetails", JsonPath = "$[*].writerId", Parameter = "userId" },
                new AggregateRouteConfig { RouteKey = "PostDetails", JsonPath = "$[*].postId", Parameter = "postId" },
            });

            b.WithAggregator("TestAggregator");

            return b.Build();
        }

        private void GivenTheFollowing(Route route)
        {
            _downstreamRoute = new Ocelot.DownstreamRouteFinder.DownstreamRouteHolder(new List<PlaceholderNameAndValue>(), route);
            _httpContext.Items.UpsertDownstreamRoute(_downstreamRoute);
        }

        private async Task WhenIMultiplex()
        {
            await _middleware.Invoke(_httpContext);
        }

        private void ThePipelineIsCalled(int expected)
        {
            _count.ShouldBe(expected);
        }
    }
}
