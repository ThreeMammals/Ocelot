﻿using System.Collections.Generic;
using Ocelot.Middleware;

namespace Ocelot.UnitTests.Responder
{
    using Microsoft.AspNetCore.Http;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Moq;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.Errors;
    using Ocelot.Logging;
    using Ocelot.Responder;
    using Ocelot.Responder.Middleware;
    using TestStack.BDDfy;
    using Xunit;

    public class ResponderMiddlewareTests
    {
        private readonly Mock<IHttpResponder> _responder;
        private readonly Mock<IErrorsToHttpStatusCodeMapper> _codeMapper;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly ResponderMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public ResponderMiddlewareTests()
        {
            _responder = new Mock<IHttpResponder>();
            _codeMapper = new Mock<IErrorsToHttpStatusCodeMapper>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ResponderMiddleware>()).Returns(_logger.Object);
            _next = context => Task.CompletedTask;
            _middleware = new ResponderMiddleware(_next, _responder.Object, _loggerFactory.Object, _codeMapper.Object);
        }

        [Fact]
        public void should_not_return_any_errors()
        {
            this.Given(x => x.GivenTheHttpResponseMessageIs(new HttpResponseMessage()))
                .And(x => x.GivenThereAreNoPipelineErrors())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenThereAreNoErrors())
                .BDDfy();
        }

        [Fact]
        public void should_return_any_errors()
        {
            this.Given(x => x.GivenTheHttpResponseMessageIs(new HttpResponseMessage()))
                .And(x => x.GivenThereArePipelineErrors(new UnableToFindDownstreamRouteError()))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenThereAreNoErrors())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheHttpResponseMessageIs(HttpResponseMessage response)
        {
            _downstreamContext.DownstreamResponse = response;
        }

        private void GivenThereAreNoPipelineErrors()
        {
            _downstreamContext.Errors = new List<Error>();
        }

        private void ThenThereAreNoErrors()
        {
            //todo a better assert?
        }

        private void GivenThereArePipelineErrors(Error error)
        {
            _downstreamContext.Errors = new List<Error>(){error};
        }  
    }
}
