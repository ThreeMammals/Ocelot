using Castle.Components.DictionaryAdapter;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.UnitTests.Responder;
using Ocelot.Values;
using System.Text;

namespace Ocelot.UnitTests.Multiplexing;

public class SimpleJsonResponseAggregatorTests : UnitTest
{
    private readonly SimpleJsonResponseAggregator _aggregator;

    public SimpleJsonResponseAggregatorTests()
    {
        _aggregator = new SimpleJsonResponseAggregator();
    }

    [Fact]
    public async Task Should_aggregate_n_responses_and_set_response_content_on_upstream_context_withConfig()
    {
        var commentsDownstreamRoute = new DownstreamRouteBuilder().WithKey("Comments").Build();
        var userDetailsDownstreamRoute = new DownstreamRouteBuilder().WithKey("UserDetails")
            .WithUpstreamPathTemplate(new UpstreamPathTemplate(string.Empty, 0, false, "/v1/users/{userId}"))
            .Build();
        var downstreamRoutes = new List<DownstreamRoute>
        {
            commentsDownstreamRoute,
            userDetailsDownstreamRoute,
        };
        var route = new Route()
        {
            DownstreamRoute = downstreamRoutes,
            DownstreamRouteConfig = [
                new(){RouteKey = "UserDetails",JsonPath = "$[*].writerId",Parameter = "userId"},
            ],
        };

        var commentsResponseContent = @"[{string.Emptyidstring.Empty:1,string.EmptywriterIdstring.Empty:1,string.EmptypostIdstring.Empty:1,string.Emptytextstring.Empty:string.Emptytext1string.Empty},{string.Emptyidstring.Empty:2,string.EmptywriterIdstring.Empty:2,string.EmptypostIdstring.Empty:2,string.Emptytextstring.Empty:string.Emptytext2string.Empty},{string.Emptyidstring.Empty:3,string.EmptywriterIdstring.Empty:2,string.EmptypostIdstring.Empty:1,string.Emptytextstring.Empty:string.Emptytext21string.Empty}]";

        var commentsDownstreamContext = new DefaultHttpContext();
        commentsDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent(commentsResponseContent, Encoding.UTF8, "application/json"), HttpStatusCode.OK, new EditableList<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
        commentsDownstreamContext.Items.UpsertDownstreamRoute(commentsDownstreamRoute);

        var userDetailsResponseContent = @"[{string.Emptyidstring.Empty:1,string.EmptyfirstNamestring.Empty:string.Emptyabolfazlstring.Empty,string.EmptylastNamestring.Empty:string.Emptyrajabpourstring.Empty},{string.Emptyidstring.Empty:2,string.EmptyfirstNamestring.Empty:string.Emptyrezastring.Empty,string.EmptylastNamestring.Empty:string.Emptyrezaeistring.Empty}]";
        var userDetailsDownstreamContext = new DefaultHttpContext();
        userDetailsDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent(userDetailsResponseContent, Encoding.UTF8, "application/json"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
        userDetailsDownstreamContext.Items.UpsertDownstreamRoute(userDetailsDownstreamRoute);

        var downstreamContexts = new List<HttpContext> { commentsDownstreamContext, userDetailsDownstreamContext };
        var expected = "{\"Comments\":" + commentsResponseContent + ",\"UserDetails\":" + userDetailsResponseContent + "}";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheContentTypeIs(upstreamContext, "application/json");
        ThenTheReasonPhraseIs(upstreamContext, "cannot return from aggregate..which reason phrase would you use?");
    }

    [Fact]
    public async Task Should_aggregate_n_responses_and_set_response_content_on_upstream_context()
    {
        var billDownstreamRoute = new DownstreamRouteBuilder().WithKey("Bill").Build();
        var georgeDownstreamRoute = new DownstreamRouteBuilder().WithKey("George").Build();
        var downstreamRoutes = new List<DownstreamRoute>
        {
            billDownstreamRoute,
            georgeDownstreamRoute,
        };
        var route = new Route()
        {
            DownstreamRoute = downstreamRoutes,
        };

        var billDownstreamContext = new DefaultHttpContext();
        billDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new EditableList<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
        billDownstreamContext.Items.UpsertDownstreamRoute(billDownstreamRoute);

        var georgeDownstreamContext = new DefaultHttpContext();
        georgeDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("George says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
        georgeDownstreamContext.Items.UpsertDownstreamRoute(georgeDownstreamRoute);

        var downstreamContexts = new List<HttpContext> { billDownstreamContext, georgeDownstreamContext };
        var expected = "{\"Bill\":Bill says hi,\"George\":George says hi}";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheContentTypeIs(upstreamContext, "application/json");
        ThenTheReasonPhraseIs(upstreamContext, "cannot return from aggregate..which reason phrase would you use?");
    }

    [Fact]
    public async Task Should_return_error_if_any_downstreams_have_errored()
    {
        var billDownstreamRoute = new DownstreamRouteBuilder().WithKey("Bill").Build();
        var georgeDownstreamRoute = new DownstreamRouteBuilder().WithKey("George").Build();
        var downstreamRoutes = new List<DownstreamRoute>
        {
            billDownstreamRoute,
            georgeDownstreamRoute,
        };
        var route = new Route()
        {
            DownstreamRoute = downstreamRoutes,
        };

        var billDownstreamContext = new DefaultHttpContext();
        billDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
        billDownstreamContext.Items.UpsertDownstreamRoute(billDownstreamRoute);

        var georgeDownstreamContext = new DefaultHttpContext();
        georgeDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Error"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
        georgeDownstreamContext.Items.UpsertDownstreamRoute(georgeDownstreamRoute);

        georgeDownstreamContext.Items.SetError(new AnyError());

        var downstreamContexts = new List<HttpContext> { billDownstreamContext, georgeDownstreamContext };
        var expected = "Error";
        var upstreamContext = new DefaultHttpContext();

        // Act
        await _aggregator.Aggregate(route, upstreamContext, downstreamContexts);

        // Assert
        await ThenTheContentIs(upstreamContext, expected);
        ThenTheErrorIsMapped(upstreamContext, downstreamContexts);
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
