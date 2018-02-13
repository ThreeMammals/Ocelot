namespace Ocelot.UnitTests.Request
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.Infrastructure.RequestData;
    using Ocelot.Logging;
    using Ocelot.Request.Builder;
    using Ocelot.Request.Middleware;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;
    using Ocelot.Requester.QoS;
    using Ocelot.Configuration;
    using Microsoft.AspNetCore.Builder;
    using Ocelot.Errors;

    public class HttpRequestBuilderMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<IRequestCreator> _requestBuilder;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly HttpRequestMessage _downstreamRequest;
        private OkResponse<Ocelot.Request.Request> _request;
        private OkResponse<string> _downstreamUrl;
        private OkResponse<DownstreamRoute> _downstreamRoute;

        public HttpRequestBuilderMiddlewareTests()
        {
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _requestBuilder = new Mock<IRequestCreator>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();

            _downstreamRequest = new HttpRequestMessage();

            _scopedRepository
                .Setup(sr => sr.Get<HttpRequestMessage>("DownstreamRequest"))
                .Returns(new OkResponse<HttpRequestMessage>(_downstreamRequest));

            GivenTheTestServerIsConfigured();
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

            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(new NoQoSProvider())))
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

            this.Given(x => x.GivenTheDownStreamUrlIs("any old string"))
                .And(x => x.GivenTheQosProviderHouseReturns(new ErrorResponse<IQoSProvider>(It.IsAny<Error>())))
                .And(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => x.GivenTheRequestBuilderReturns(new Ocelot.Request.Request(new HttpRequestMessage(), true, new NoQoSProvider(), false, false, "", false)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedDataRepositoryQosProviderError())
                .BDDfy();
        }


        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_qosProviderHouse.Object);
            services.AddSingleton(_requestBuilder.Object);
            services.AddSingleton(_scopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseHttpRequestBuilderMiddleware();
        }

        private void GivenTheDownStreamUrlIs(string downstreamUrl)
        {
            _downstreamUrl = new OkResponse<string>(downstreamUrl);
            _scopedRepository
                .Setup(x => x.Get<string>(It.IsAny<string>()))
                .Returns(_downstreamUrl);
        }

        private void GivenTheQosProviderHouseReturns(Response<IQoSProvider> qosProvider)
        {
            _qosProviderHouse
                .Setup(x => x.Get(It.IsAny<ReRoute>()))
                .Returns(qosProvider);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
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
            _scopedRepository
                .Verify(x => x.Add("Request", _request.Data), Times.Once());
        }

        private void ThenTheScopedDataRepositoryQosProviderError()
        {
            _scopedRepository
                .Verify(x => x.Add("OcelotMiddlewareError", true), Times.Once());
        }
    }
}
