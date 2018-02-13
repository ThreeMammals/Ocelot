namespace Ocelot.UnitTests.Requester
{
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Logging;
    using Ocelot.Requester;
    using Ocelot.Requester.Middleware;
    using Ocelot.Requester.QoS;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;

    public class HttpRequesterMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<IHttpRequester> _requester;
        private OkResponse<HttpResponseMessage> _response;
        private OkResponse<Ocelot.Request.Request> _request;

        public HttpRequesterMiddlewareTests()
        {
            _requester = new Mock<IHttpRequester>();

            GivenTheTestServerIsConfigured();
        }

        [Fact]
        public void should_call_scoped_data_repository_correctly()
        {
            this.Given(x => x.GivenTheRequestIs(new Ocelot.Request.Request(new HttpRequestMessage(),true, new NoQoSProvider(), false, false, "", false)))
                .And(x => x.GivenTheRequesterReturns(new HttpResponseMessage()))
                .And(x => x.GivenTheScopedRepoReturns())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheScopedRepoIsCalledCorrectly())
                .BDDfy();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_requester.Object);
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseHttpRequesterMiddleware();
        }

        private void GivenTheRequestIs(Ocelot.Request.Request request)
        {
            _request = new OkResponse<Ocelot.Request.Request>(request);
            ScopedRepository
                .Setup(x => x.Get<Ocelot.Request.Request>(It.IsAny<string>()))
                .Returns(_request);
        }

        private void GivenTheRequesterReturns(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            _requester
                .Setup(x => x.GetResponse(It.IsAny<Ocelot.Request.Request>()))
                .ReturnsAsync(_response);
        }

        private void GivenTheScopedRepoReturns()
        {
            ScopedRepository
                .Setup(x => x.Add(It.IsAny<string>(), _response.Data))
                .Returns(new OkResponse());
        }

        private void ThenTheScopedRepoIsCalledCorrectly()
        {
            ScopedRepository
                .Verify(x => x.Add("HttpResponseMessage", _response.Data), Times.Once());
        }
    }
}
