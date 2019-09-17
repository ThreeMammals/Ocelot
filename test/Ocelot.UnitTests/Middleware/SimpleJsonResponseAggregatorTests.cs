using Castle.Components.DictionaryAdapter;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.UnitTests.Responder;
using Ocelot.Values;
using Shouldly;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    public class SimpleJsonResponseAggregatorTests
    {
        private readonly SimpleJsonResponseAggregator _aggregator;
        private List<DownstreamContext> _downstreamContexts;
        private DownstreamContext _upstreamContext;
        private ReRoute _reRoute;

        public SimpleJsonResponseAggregatorTests()
        {
            _aggregator = new SimpleJsonResponseAggregator();
        }

        [Fact]
        public void should_aggregate_n_responses_and_set_response_content_on_upstream_context_withConfig()
        {
            var commentsDownstreamReRoute = new DownstreamReRouteBuilder().WithKey("Comments").Build();

            var userDetailsDownstreamReRoute = new DownstreamReRouteBuilder().WithKey("UserDetails")
                .WithUpstreamPathTemplate(new UpstreamPathTemplate("", 0, false, "/v1/users/{userId}"))
                .Build();

            var downstreamReRoutes = new List<DownstreamReRoute>
            {
                commentsDownstreamReRoute,
                userDetailsDownstreamReRoute
            };

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoutes(downstreamReRoutes)
                .WithAggregateReRouteConfig(new List<AggregateReRouteConfig>()
                {
                    new AggregateReRouteConfig(){ReRouteKey = "UserDetails",JsonPath = "$[*].writerId",Parameter = "userId"}
                })
                .Build();

            var commentsResponseContent = @"[{""id"":1,""writerId"":1,""postId"":1,""text"":""text1""},{""id"":2,""writerId"":2,""postId"":2,""text"":""text2""},{""id"":3,""writerId"":2,""postId"":1,""text"":""text21""}]";
            var commentsDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent(commentsResponseContent, Encoding.UTF8, "application/json"), HttpStatusCode.OK, new EditableList<KeyValuePair<string, IEnumerable<string>>>(), "some reason"),
                DownstreamReRoute = commentsDownstreamReRoute
            };

            var userDetailsResponseContent = @"[{""id"":1,""firstName"":""abolfazl"",""lastName"":""rajabpour""},{""id"":2,""firstName"":""reza"",""lastName"":""rezaei""}]";
            var userDetailsDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent(userDetailsResponseContent, Encoding.UTF8, "application/json"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"),
                DownstreamReRoute = userDetailsDownstreamReRoute
            };

            var downstreamContexts = new List<DownstreamContext> { commentsDownstreamContext, userDetailsDownstreamContext };

            var expected = "{\"Comments\":" + commentsResponseContent + ",\"UserDetails\":" + userDetailsResponseContent + "}";

            this.Given(x => GivenTheUpstreamContext(new DownstreamContext(new DefaultHttpContext())))
                .And(x => GivenTheReRoute(reRoute))
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
            var billDownstreamReRoute = new DownstreamReRouteBuilder().WithKey("Bill").Build();

            var georgeDownstreamReRoute = new DownstreamReRouteBuilder().WithKey("George").Build();

            var downstreamReRoutes = new List<DownstreamReRoute>
            {
                billDownstreamReRoute,
                georgeDownstreamReRoute
            };

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoutes(downstreamReRoutes)
                .Build();

            var billDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new EditableList<KeyValuePair<string, IEnumerable<string>>>(), "some reason"),
                DownstreamReRoute = billDownstreamReRoute
            };

            var georgeDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent("George says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"),
                DownstreamReRoute = georgeDownstreamReRoute
            };

            var downstreamContexts = new List<DownstreamContext> { billDownstreamContext, georgeDownstreamContext };

            var expected = "{\"Bill\":Bill says hi,\"George\":George says hi}";

            this.Given(x => GivenTheUpstreamContext(new DownstreamContext(new DefaultHttpContext())))
                .And(x => GivenTheReRoute(reRoute))
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
            var billDownstreamReRoute = new DownstreamReRouteBuilder().WithKey("Bill").Build();

            var georgeDownstreamReRoute = new DownstreamReRouteBuilder().WithKey("George").Build();

            var downstreamReRoutes = new List<DownstreamReRoute>
            {
                billDownstreamReRoute,
                georgeDownstreamReRoute
            };

            var reRoute = new ReRouteBuilder()
                .WithDownstreamReRoutes(downstreamReRoutes)
                .Build();

            var billDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"),
                DownstreamReRoute = billDownstreamReRoute
            };

            var georgeDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent("Error"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason"),
                DownstreamReRoute = georgeDownstreamReRoute,
            };

            georgeDownstreamContext.Errors.Add(new AnyError());

            var downstreamContexts = new List<DownstreamContext> { billDownstreamContext, georgeDownstreamContext };

            var expected = "Error";

            this.Given(x => GivenTheUpstreamContext(new DownstreamContext(new DefaultHttpContext())))
                .And(x => GivenTheReRoute(reRoute))
                .And(x => GivenTheDownstreamContext(downstreamContexts))
                .When(x => WhenIAggregate())
                .Then(x => ThenTheContentIs(expected))
                .And(x => ThenTheErrorIsMapped())
                .BDDfy();
        }

        private void ThenTheReasonPhraseIs(string expected)
        {
            _upstreamContext.DownstreamResponse.ReasonPhrase.ShouldBe(expected);
        }

        private void ThenTheErrorIsMapped()
        {
            _upstreamContext.Errors.ShouldBe(_downstreamContexts[1].Errors);
            _upstreamContext.DownstreamResponse.ShouldBe(_downstreamContexts[1].DownstreamResponse);
        }

        private void GivenTheReRoute(ReRoute reRoute)
        {
            _reRoute = reRoute;
        }

        private void GivenTheUpstreamContext(DownstreamContext upstreamContext)
        {
            _upstreamContext = upstreamContext;
        }

        private void GivenTheDownstreamContext(List<DownstreamContext> downstreamContexts)
        {
            _downstreamContexts = downstreamContexts;
        }

        private void WhenIAggregate()
        {
            _aggregator.Aggregate(_reRoute, _upstreamContext, _downstreamContexts).GetAwaiter().GetResult();
        }

        private void ThenTheContentIs(string expected)
        {
            var content = _upstreamContext.DownstreamResponse.Content.ReadAsStringAsync()
                .GetAwaiter()
                .GetResult();

            content.ShouldBe(expected);
        }

        private void ThenTheContentTypeIs(string expected)
        {
            _upstreamContext.DownstreamResponse.Content.Headers.ContentType.MediaType.ShouldBe(expected);
        }

        private void ThenTheUpstreamContextIsMappedForNonAggregate()
        {
            _upstreamContext.DownstreamRequest.ShouldBe(_downstreamContexts[0].DownstreamRequest);
            _upstreamContext.DownstreamResponse.ShouldBe(_downstreamContexts[0].DownstreamResponse);
            _upstreamContext.Errors.ShouldBe(_downstreamContexts[0].Errors);
        }
    }
}
