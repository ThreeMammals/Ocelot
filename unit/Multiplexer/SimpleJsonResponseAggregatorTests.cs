using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.UnitTests.Responder;
using Ocelot.Values;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ocelot.UnitTests.Multiplexer;

[Trait("Feat", "79")] // https://github.com/ThreeMammals/Ocelot/issues/79
public class SimpleJsonResponseAggregatorTests : UnitTest
{
    private readonly SimpleJsonResponseAggregator _aggregator = new();

    [Fact]
    [Trait("Feat", "661")] // https://github.com/ThreeMammals/Ocelot/issues/661
    [Trait("PR", "704")] // https://github.com/ThreeMammals/Ocelot/pull/704
    [Trait("Release", "13.2.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/13.2.0
    public async Task Should_aggregate_n_responses_and_set_response_content_on_upstream_context_withConfig()
    {
        var commentsDownstreamRoute = new DownstreamRouteBuilder().WithKey("Comments").Build();
        var userDetailsDownstreamRoute = new DownstreamRouteBuilder().WithKey("UserDetails")
            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 0, false, "/v1/users/{userId}"))
            .Build();
        var route = new Route()
        {
            DownstreamRoute = [commentsDownstreamRoute, userDetailsDownstreamRoute],
            DownstreamRouteConfig = [ new("UserDetails", "$[*].writerId", "userId") ],
        };

        var commentsResponseContent = @"[{string.Emptyidstring.Empty:1,string.EmptywriterIdstring.Empty:1,string.EmptypostIdstring.Empty:1,string.Emptytextstring.Empty:string.Emptytext1string.Empty},{string.Emptyidstring.Empty:2,string.EmptywriterIdstring.Empty:2,string.EmptypostIdstring.Empty:2,string.Emptytextstring.Empty:string.Emptytext2string.Empty},{string.Emptyidstring.Empty:3,string.EmptywriterIdstring.Empty:2,string.EmptypostIdstring.Empty:1,string.Emptytextstring.Empty:string.Emptytext21string.Empty}]";
        var commentsDownstreamContext = GivenContext(commentsDownstreamRoute, GivenJsonContent(commentsResponseContent));
        var userDetailsResponseContent = @"[{string.Emptyidstring.Empty:1,string.EmptyfirstNamestring.Empty:string.Emptyabolfazlstring.Empty,string.EmptylastNamestring.Empty:string.Emptyrajabpourstring.Empty},{string.Emptyidstring.Empty:2,string.EmptyfirstNamestring.Empty:string.Emptyrezastring.Empty,string.EmptylastNamestring.Empty:string.Emptyrezaeistring.Empty}]";
        var userDetailsDownstreamContext = GivenContext(userDetailsDownstreamRoute, GivenJsonContent(userDetailsResponseContent));

        var downstreamContexts = new List<HttpContext> { commentsDownstreamContext, userDetailsDownstreamContext };
        var expected = "{\"Comments\":" + commentsResponseContent + ",\"UserDetails\":" + userDetailsResponseContent + "}";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheContentTypeIs(upstreamContext, MediaTypeNames.Application.Json);
        ThenTheReasonPhraseIs(upstreamContext, "cannot return from aggregate..which reason phrase would you use?");
    }

    [Fact]
    [Trait("PR", "248")] // https://github.com/ThreeMammals/Ocelot/pull/248
    [Trait("Release", "3.1.7")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.7
    public async Task Should_aggregate_n_responses_and_set_response_content_on_upstream_context()
    {
        var billDownstreamRoute = new DownstreamRouteBuilder().WithKey("Bill").Build();
        var georgeDownstreamRoute = new DownstreamRouteBuilder().WithKey("George").Build();
        var route = new Route()
        {
            DownstreamRoute = [billDownstreamRoute, georgeDownstreamRoute]
        };

        var billDownstreamContext = GivenContext(billDownstreamRoute, GivenJsonContent("Bill says hi"));
        var georgeDownstreamContext = GivenContext(georgeDownstreamRoute, GivenJsonContent("George says hi"));

        var downstreamContexts = new List<HttpContext> { billDownstreamContext, georgeDownstreamContext };
        var expected = "{\"Bill\":Bill says hi,\"George\":George says hi}";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheContentTypeIs(upstreamContext, MediaTypeNames.Application.Json);
        ThenTheReasonPhraseIs(upstreamContext, "cannot return from aggregate..which reason phrase would you use?");
    }

    [Fact]
    [Trait("PR", "248")] // https://github.com/ThreeMammals/Ocelot/pull/248
    [Trait("Release", "3.1.7")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.7
    public async Task Should_return_error_if_any_downstreams_have_errored()
    {
        var billDownstreamRoute = new DownstreamRouteBuilder().WithKey("Bill").Build();
        var georgeDownstreamRoute = new DownstreamRouteBuilder().WithKey("George").Build();
        var route = new Route()
        {
            DownstreamRoute = [billDownstreamRoute, georgeDownstreamRoute]
        };

        var billDownstreamContext = GivenContext(billDownstreamRoute, GivenJsonContent("Bill says hi"));
        var georgeDownstreamContext = GivenContext(billDownstreamRoute, GivenJsonContent("Error"), HttpStatusCode.InternalServerError);
        georgeDownstreamContext.Items.SetError(new AnyError());

        var downstreamContexts = new List<HttpContext> { billDownstreamContext, georgeDownstreamContext };
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, "Error");
        ThenTheErrorIsMapped(upstreamContext, downstreamContexts);
    }

    [Fact]
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_aggregate_two_contexts_with_same_key()
    {
        var downstreamRoute = new DownstreamRouteBuilder().WithKey("Products").Build();
        var route = new Route(downstreamRoute);

        var context1 = GivenContext(downstreamRoute, GivenJsonContent("{\"id\":1,\"name\":\"Product1\"}"));
        var context2 = GivenContext(downstreamRoute, GivenJsonContent("{\"id\":2,\"name\":\"Product2\"}"));

        var downstreamContexts = new List<HttpContext> { context1, context2 };
        var expected = "{\"Products\":[{\"id\":1,\"name\":\"Product1\"},{\"id\":2,\"name\":\"Product2\"}]}";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheContentTypeIs(upstreamContext, MediaTypeNames.Application.Json);
    }

    [Fact]
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_aggregate_multiple_responses_in_array_with_trailing_comma_handling()
    {
        var downstreamRoute = new DownstreamRouteBuilder().WithKey("Orders").Build();
        var route = new Route(downstreamRoute);

        var context1 = GivenContext(downstreamRoute, GivenJsonContent("{\"orderId\":101}"));
        var context2 = GivenContext(downstreamRoute, GivenJsonContent("{\"orderId\":102}"));
        var context3 = GivenContext(downstreamRoute, GivenJsonContent("{\"orderId\":103}"));

        var downstreamContexts = new List<HttpContext> { context1, context2, context3 };
        var expected = "{\"Orders\":[{\"orderId\":101},{\"orderId\":102},{\"orderId\":103}]}";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheContentTypeIs(upstreamContext, MediaTypeNames.Application.Json);
    }

    [Fact]
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_return_error_if_error_occurs_in_first_of_multiple_contexts()
    {
        var downstreamRoute = new DownstreamRouteBuilder().WithKey("Products").Build();
        var route = new Route(downstreamRoute);

        var context1 = GivenContext(downstreamRoute, GivenJsonContent("Error occurred"), HttpStatusCode.InternalServerError, "error reason");
        context1.Items.SetError(new AnyError());
        var context2 = GivenContext(downstreamRoute, GivenJsonContent("{\"id\":2}"));

        var downstreamContexts = new List<HttpContext> { context1, context2 };
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, "Error occurred");
        ThenTheErrorIsMapped(upstreamContext, context1);
    }

    [Fact]
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_return_error_if_error_occurs_in_last_of_multiple_contexts()
    {
        var downstreamRoute = new DownstreamRouteBuilder().WithKey("Products").Build();
        var route = new Route(downstreamRoute);

        var context1 = GivenContext(downstreamRoute, GivenJsonContent("{\"id\":1}"));
        var context2 = GivenContext(downstreamRoute, GivenJsonContent("Error occurred"), HttpStatusCode.InternalServerError, "error reason");
        context2.Items.SetError(new AnyError());

        var downstreamContexts = new List<HttpContext> { context1, context2 };
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, "Error occurred");
        ThenTheErrorIsMapped(upstreamContext, context2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_skip_empty_content_in_multiple_context_array_aggregation(string emptyContent)
    {
        var downstreamRoute = new DownstreamRouteBuilder().WithKey("Data").Build();
        var route = new Route(downstreamRoute);

        var context1 = GivenContext(downstreamRoute, GivenJsonContent("{\"id\":1,\"value\":\"first\"}"));
        var context2 = GivenContext(downstreamRoute, GivenJsonContent(emptyContent ?? string.Empty)); // !!!
        var context3 = GivenContext(downstreamRoute, GivenJsonContent("{\"id\":3,\"value\":\"third\"}"));

        var downstreamContexts = new List<HttpContext> { context1, context2, context3 };
        var expected = "{\"Data\":[{\"id\":1,\"value\":\"first\"},{\"id\":3,\"value\":\"third\"}]}";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheContentTypeIs(upstreamContext, MediaTypeNames.Application.Json);
    }

    [Fact]
    [Trait("PR", "1389")] // https://github.com/ThreeMammals/Ocelot/pull/1389
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_return_error_if_single_downstream_context_has_error()
    {
        var downstreamRoute = new DownstreamRouteBuilder().WithKey("Product").Build();
        var route = new Route(downstreamRoute);

        var errorContext = GivenContext(downstreamRoute, GivenJsonContent("Error occurred"), HttpStatusCode.InternalServerError, "single context error");
        errorContext.Items.SetError(new AnyError());

        var downstreamContexts = new List<HttpContext> { errorContext };
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, "Error occurred");
        ThenTheErrorIsMapped(upstreamContext, errorContext);
    }

    private static StringContent GivenJsonContent(string json) => new(json, Encoding.UTF8, MediaTypeNames.Application.Json);

    private static HttpContext GivenContext(DownstreamRoute route, HttpContent content,
        HttpStatusCode status = HttpStatusCode.OK, [CallerMemberName] string testNameAsReason = null)
    {
        var dr = new DownstreamResponse(content, status, new List<Header>(), testNameAsReason ?? "some reason");
        return GivenContext(route, dr);
    }

    private static HttpContext GivenContext(DownstreamRoute route, DownstreamResponse response)
    {
        var context = new DefaultHttpContext();
        context.Items.UpsertDownstreamResponse(response);
        context.Items.UpsertDownstreamRoute(route);
        return context;
    }

    private static void ThenTheReasonPhraseIs(DefaultHttpContext upstreamContext, string expected)
    {
        upstreamContext.Items.DownstreamResponse().ReasonPhrase.ShouldBe(expected);
    }

    private static void ThenTheErrorIsMapped(DefaultHttpContext upstreamContext, List<HttpContext> downstreamContexts)
    {
        upstreamContext.Items.Errors().ShouldBe(downstreamContexts[1].Items.Errors());
        upstreamContext.Items.DownstreamResponse().ShouldBe(downstreamContexts[1].Items.DownstreamResponse());
    }

    private static void ThenTheErrorIsMapped(DefaultHttpContext upstreamContext, HttpContext downstreamContext)
    {
        upstreamContext.Items.Errors().ShouldBe(downstreamContext.Items.Errors());
        upstreamContext.Items.DownstreamResponse().ShouldBe(downstreamContext.Items.DownstreamResponse());
    }

    private static async Task ThenTheContentIs(DefaultHttpContext upstreamContext, string expected)
    {
        var content = await upstreamContext.Items.DownstreamResponse().Content.ReadAsStringAsync();
        content.ShouldBe(expected);
    }

    private static void ThenTheContentTypeIs(DefaultHttpContext upstreamContext, string expected)
    {
        upstreamContext.Items.DownstreamResponse().Content.Headers.ContentType.MediaType.ShouldBe(expected);
    }
}
