using Castle.Components.DictionaryAdapter;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.UnitTests.Responder;
using Ocelot.Values;
using System.Text;

namespace Ocelot.UnitTests.Multiplexing
{
    public class SimpleJsonResponseAggregatorTests : UnitTest
    {
        private readonly SimpleJsonResponseAggregator _aggregator;
        private List<HttpContext> _downstreamContexts;
        private HttpContext _upstreamContext;
        private Route _route;

        public SimpleJsonResponseAggregatorTests()
        {
            _aggregator = new SimpleJsonResponseAggregator();
        }

        [Fact]
        public void should_aggregate_n_responses_and_set_response_content_on_upstream_context_withConfig()
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

            var route = new RouteBuilder()
                .WithDownstreamRoutes(downstreamRoutes)
                .WithAggregateRouteConfig(new List<AggregateRouteConfig>
                {
                    new(){RouteKey = "UserDetails",JsonPath = "$[*].writerId",Parameter = "userId"},
                })
                .Build();

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

            this.Given(x => GivenTheUpstreamContext(new DefaultHttpContext()))
                .And(x => GivenTheRoute(route))
                .And(x => GivenTheDownstreamContext(downstreamContexts))
                .When(x => WhenIAggregate())
                .Then(x => ThenTheContentIs(expected))
                .And(x => ThenTheContentTypeIs("application/json"))
                .And(x => ThenTheReasonPhraseIs("cannot return from aggregate..which reason phrase would you use?"))
                .BDDfy();
        }

        [Fact]
        public void should_aggregate_n_responses_and_set_response_content_on_upstream_context()
        {
            var billDownstreamRoute = new DownstreamRouteBuilder().WithKey("Bill").Build();

            var georgeDownstreamRoute = new DownstreamRouteBuilder().WithKey("George").Build();

            var downstreamRoutes = new List<DownstreamRoute>
            {
                billDownstreamRoute,
                georgeDownstreamRoute,
            };

            var route = new RouteBuilder()
                .WithDownstreamRoutes(downstreamRoutes)
                .Build();

            var billDownstreamContext = new DefaultHttpContext();
            billDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new EditableList<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
            billDownstreamContext.Items.UpsertDownstreamRoute(billDownstreamRoute);

            var georgeDownstreamContext = new DefaultHttpContext();
            georgeDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("George says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
            georgeDownstreamContext.Items.UpsertDownstreamRoute(georgeDownstreamRoute);

            var downstreamContexts = new List<HttpContext> { billDownstreamContext, georgeDownstreamContext };

            var expected = "{\"Bill\":Bill says hi,\"George\":George says hi}";

            this.Given(x => GivenTheUpstreamContext(new DefaultHttpContext()))
                .And(x => GivenTheRoute(route))
                .And(x => GivenTheDownstreamContext(downstreamContexts))
                .When(x => WhenIAggregate())
                .Then(x => ThenTheContentIs(expected))
                .And(x => ThenTheContentTypeIs("application/json"))
                .And(x => ThenTheReasonPhraseIs("cannot return from aggregate..which reason phrase would you use?"))
                .BDDfy();
        }

        [Fact]
        public void should_return_error_if_any_downstreams_have_errored()
        {
            var billDownstreamRoute = new DownstreamRouteBuilder().WithKey("Bill").Build();

            var georgeDownstreamRoute = new DownstreamRouteBuilder().WithKey("George").Build();

            var downstreamRoutes = new List<DownstreamRoute>
            {
                billDownstreamRoute,
                georgeDownstreamRoute,
            };

            var route = new RouteBuilder()
                .WithDownstreamRoutes(downstreamRoutes)
                .Build();

            var billDownstreamContext = new DefaultHttpContext();
            billDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
            billDownstreamContext.Items.UpsertDownstreamRoute(billDownstreamRoute);

            var georgeDownstreamContext = new DefaultHttpContext();
            georgeDownstreamContext.Items.UpsertDownstreamResponse(new DownstreamResponse(new StringContent("Error"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"));
            georgeDownstreamContext.Items.UpsertDownstreamRoute(georgeDownstreamRoute);

            georgeDownstreamContext.Items.SetError(new AnyError());

            var downstreamContexts = new List<HttpContext> { billDownstreamContext, georgeDownstreamContext };

            var expected = "Error";

            this.Given(x => GivenTheUpstreamContext(new DefaultHttpContext()))
                .And(x => GivenTheRoute(route))
                .And(x => GivenTheDownstreamContext(downstreamContexts))
                .When(x => WhenIAggregate())
                .Then(x => ThenTheContentIs(expected))
                .And(x => ThenTheErrorIsMapped())
                .BDDfy();
        }

        private void ThenTheReasonPhraseIs(string expected)
        {
            _upstreamContext.Items.DownstreamResponse().ReasonPhrase.ShouldBe(expected);
        }

        private void ThenTheErrorIsMapped()
        {
            _upstreamContext.Items.Errors().ShouldBe(_downstreamContexts[1].Items.Errors());
            _upstreamContext.Items.DownstreamResponse().ShouldBe(_downstreamContexts[1].Items.DownstreamResponse());
        }

        private void GivenTheRoute(Route route)
        {
            _route = route;
        }

        private void GivenTheUpstreamContext(HttpContext upstreamContext)
        {
            _upstreamContext = upstreamContext;
        }

        private void GivenTheDownstreamContext(List<HttpContext> downstreamContexts)
        {
            _downstreamContexts = downstreamContexts;
        }

        private async Task WhenIAggregate()
        {
            await _aggregator.Aggregate(_route, _upstreamContext, _downstreamContexts);
        }

        private async Task ThenTheContentIs(string expected)
        {
            var content = await _upstreamContext.Items.DownstreamResponse().Content.ReadAsStringAsync();
            content.ShouldBe(expected);
        }

        private void ThenTheContentTypeIs(string expected)
        {
            _upstreamContext.Items.DownstreamResponse().Content.Headers.ContentType.MediaType.ShouldBe(expected);
        }

        private void ThenTheUpstreamContextIsMappedForNonAggregate()
        {
            _upstreamContext.Items.DownstreamRequest().ShouldBe(_downstreamContexts[0].Items.DownstreamRequest());
            _upstreamContext.Items.DownstreamRequest().ShouldBe(_downstreamContexts[0].Items.DownstreamRequest());
            _upstreamContext.Items.Errors().ShouldBe(_downstreamContexts[0].Items.Errors());
        }
    }
}
