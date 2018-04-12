using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Castle.Components.DictionaryAdapter;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Errors;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Request.Middleware;
using Ocelot.UnitTests.Responder;
using Shouldly;
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
                DownstreamResponse = new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new EditableList<KeyValuePair<string, IEnumerable<string>>>()),
                DownstreamReRoute = billDownstreamReRoute
            };

            var georgeDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent("George says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>()),
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
                DownstreamResponse = new DownstreamResponse(new StringContent("Bill says hi"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>()),
                DownstreamReRoute = billDownstreamReRoute
            };

            var georgeDownstreamContext = new DownstreamContext(new DefaultHttpContext())
            {
                DownstreamResponse = new DownstreamResponse(new StringContent("Error"), HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>()),
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
