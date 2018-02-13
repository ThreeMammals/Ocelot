namespace Ocelot.UnitTests.Responder
{
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.Errors;
    using Ocelot.Logging;
    using Ocelot.Requester;
    using Ocelot.Responder;
    using Ocelot.Responder.Middleware;
    using Ocelot.Responses;
    using TestStack.BDDfy;
    using Xunit;

    public class ResponderMiddlewareTests : ServerHostedMiddlewareTest
    {
        private readonly Mock<IHttpResponder> _responder;
        private readonly Mock<IErrorsToHttpStatusCodeMapper> _codeMapper;
        private OkResponse<HttpResponseMessage> _response;

        public ResponderMiddlewareTests()
        {
            _responder = new Mock<IHttpResponder>();
            _codeMapper = new Mock<IErrorsToHttpStatusCodeMapper>();

            GivenTheTestServerIsConfigured();
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

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(_codeMapper.Object);
            services.AddSingleton(_responder.Object);
            services.AddSingleton(ScopedRepository.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseResponderMiddleware();
        }

        private void GivenTheHttpResponseMessageIs(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            ScopedRepository
                .Setup(x => x.Get<HttpResponseMessage>(It.IsAny<string>()))
                .Returns(_response);
        }

        private void GivenThereAreNoPipelineErrors()
        {
            ScopedRepository
                .Setup(x => x.Get<bool>(It.IsAny<string>()))
                .Returns(new OkResponse<bool>(false));
        }

        private void ThenThereAreNoErrors()
        {
            //todo a better assert?
        }

        private void GivenThereArePipelineErrors(Error error)
        {
            ScopedRepository
                .Setup(x => x.Get<bool>("OcelotMiddlewareError"))
                .Returns(new OkResponse<bool>(true));
            ScopedRepository.Setup(x => x.Get<List<Error>>("OcelotMiddlewareErrors"))
                .Returns(new OkResponse<List<Error>>(new List<Error>() { error }));
        }  
    }
}
