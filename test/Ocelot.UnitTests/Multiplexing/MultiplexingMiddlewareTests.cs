using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using System.Reflection;
using System.Security.Claims;

namespace Ocelot.UnitTests.Multiplexing
{
    public class MultiplexingMiddlewareTests
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
        public void Copy_User_ToTarget()
        {
            // Arrange
            GivenUser("test", "Copy", nameof(Copy_User_ToTarget));

            // Act
            var method = _middleware.GetType().GetMethod("Copy", BindingFlags.NonPublic | BindingFlags.Static);
            HttpContext actual = (HttpContext)method.Invoke(_middleware, [_httpContext]);

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
            for (int i = 0; i < count; i++)
            {
                b.WithDownstreamRoute(new DownstreamRouteBuilder().Build());
            }

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
