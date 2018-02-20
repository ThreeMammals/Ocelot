using Ocelot.Middleware;

namespace Ocelot.UnitTests.Request
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Shouldly;
    using System.Collections.Generic;
    using System.Net.Http;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Logging;
    using Ocelot.Request.Builder;
    using Ocelot.Request.Middleware;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;
    using Ocelot.Requester.QoS;
    using Ocelot.Configuration;
    using Ocelot.Errors;

    public class HttpRequestBuilderMiddlewareTests
    {
        private readonly Mock<IRequestCreator> _requestBuilder;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly HttpRequestMessage _downstreamRequest;
        private OkResponse<Ocelot.Request.Request> _request;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly HttpRequestBuilderMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public HttpRequestBuilderMiddlewareTests()
        {
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _requestBuilder = new Mock<IRequestCreator>();
            _downstreamRequest = new HttpRequestMessage();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _downstreamContext.DownstreamRequest = _downstreamRequest;
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<HttpRequestBuilderMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                //do nothing
            };
            _middleware = new HttpRequestBuilderMiddleware(_next, _loggerFactory.Object, _requestBuilder.Object, _qosProviderHouse.Object);
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {

            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .WithHttpHandlerOptions(new HttpHandlerOptions(true, true,false))
                    .Build());

            this.Given(x => x.GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(new NoQoSProvider())))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheRequestBuilderReturns(new Ocelot.Request.Request(new HttpRequestMessage(), true, new NoQoSProvider(), false, false, "", false)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_call_scoped_data_repository_QosProviderError()
        {

            var downstreamRoute = new DownstreamRoute(new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithRequestIdKey("LSRequestId")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true))
                    .Build());

            this.Given(x => x.GivenTheQosProviderHouseReturns(new ErrorResponse<IQoSProvider>(It.IsAny<Error>())))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheRequestBuilderReturns(new Ocelot.Request.Request(new HttpRequestMessage(), true, new NoQoSProvider(), false, false, "", false)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryQosProviderError())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }



        private void GivenTheQosProviderHouseReturns(Response<IQoSProvider> qosProvider)
        {
            _qosProviderHouse
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(qosProvider);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamContext.DownstreamRoute = downstreamRoute;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void GivenTheRequestBuilderReturns(Ocelot.Request.Request request)
        {
            _request = new OkResponse<Ocelot.Request.Request>(request);

            _requestBuilder
                .Setup(x => x.Build(It.IsAny<HttpRequestMessage>(),
                                    It.IsAny<bool>(),
                                    It.IsAny<IQoSProvider>(),
                                    It.IsAny<bool>(),
                                    It.IsAny<bool>(),
                                    It.IsAny<string>(),
                                    It.IsAny<bool>()))
                .ReturnsAsync(_request);
        }

        private void ThenTheScopedDataRepositoryIsCalledCorrectly()
        {
            _downstreamContext.Request.ShouldBe(_request.Data);
        }

        private void ThenTheScopedDataRepositoryQosProviderError()
        {
            _downstreamContext.Response.IsError.ShouldBe(true);
        }
    }
}
