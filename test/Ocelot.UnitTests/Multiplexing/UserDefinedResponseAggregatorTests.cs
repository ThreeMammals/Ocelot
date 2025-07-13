using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Multiplexing;

public class UserDefinedResponseAggregatorTests : UnitTest
{
    private readonly UserDefinedResponseAggregator _aggregator;
    private readonly Mock<IDefinedAggregatorProvider> _provider;

    public UserDefinedResponseAggregatorTests()
    {
        _provider = new Mock<IDefinedAggregatorProvider>();
        _aggregator = new UserDefinedResponseAggregator(_provider.Object);
    }

    [Fact]
    public async Task Should_call_aggregator()
    {
        // Arrange
        var route = new RouteBuilder().Build();
        var context = new DefaultHttpContext();

        var contextA = new DefaultHttpContext();
        contextA.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Tom"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));

        var contextB = new DefaultHttpContext();
        contextB.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Laura"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));

        var contexts = new List<HttpContext>
        {
            contextA,
            contextB,
        };

        // Arrange: Given The Provider Returns Aggregator
        var aggregator = new TestDefinedAggregator();
        _provider.Setup(x => x.Get(It.IsAny<Route>())).Returns(new OkResponse<IDefinedAggregator>(aggregator));

        // Act
        await _aggregator.Aggregate(route, context, contexts);

        // Assert
        _provider.Verify(x => x.Get(route), Times.Once);
        var content = await context.Items.DownstreamResponse().Content.ReadAsStringAsync();
        content.ShouldBe("Tom, Laura");
    }

    [Fact]
    public async Task Should_not_find_aggregator()
    {
        // Arrange
        var route = new RouteBuilder().Build();
        var context = new DefaultHttpContext();

        var contextA = new DefaultHttpContext();
        contextA.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Tom"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));

        var contextB = new DefaultHttpContext();
        contextB.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Laura"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));

        var contexts = new List<HttpContext>
        {
            contextA,
            contextB,
        };

        // Arrange: Given The Provider Returns Error
        _provider.Setup(x => x.Get(It.IsAny<Route>())).Returns(new ErrorResponse<IDefinedAggregator>(new AnyError()));

        // Act
        await _aggregator.Aggregate(route, context, contexts);

        // Assert
        _provider.Verify(x => x.Get(route), Times.Once);
        context.Items.Errors().Count.ShouldBeGreaterThan(0);
        context.Items.Errors().Count.ShouldBe(1);
    }

    public class TestDefinedAggregator : IDefinedAggregator
    {
        public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
        {
            var tom = await responses[0].Items.DownstreamResponse().Content.ReadAsStringAsync();
            var laura = await responses[1].Items.DownstreamResponse().Content.ReadAsStringAsync();
            var content = $"{tom}, {laura}";
            var headers = responses.SelectMany(x => x.Items.DownstreamResponse().Headers).ToList();
            return new DownstreamResponse(new StringContent(content), HttpStatusCode.OK, headers, "some reason");
        }
    }
}
