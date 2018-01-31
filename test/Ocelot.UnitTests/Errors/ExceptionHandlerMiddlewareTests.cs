namespace Ocelot.UnitTests.Errors
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Errors.Middleware;
    using Ocelot.Logging;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Configuration.Provider;
    using Moq;
    using Ocelot.Configuration;
    using Rafty.Concensus;

    public class ExceptionHandlerMiddlewareTests : ServerHostedMiddlewareTest
    {
        bool _shouldThrowAnException = false;
        private Mock<IOcelotConfigurationProvider> _provider;

        public ExceptionHandlerMiddlewareTests()
        {
            _provider = new Mock<IOcelotConfigurationProvider>();
            GivenTheTestServerIsConfigured();
        }
        
        [Fact]
        public void NoDownstreamException()
        {
            var config = new OcelotConfiguration(null, null, null, null);

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheRequestIdIsNotSet())
                .BDDfy();
        }

        private void TheRequestIdIsNotSet()
        {
            ScopedRepository.Verify(x => x.Add<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void DownstreamException()
        {
            var config = new OcelotConfiguration(null, null, null, null);

            this.Given(_ => GivenAnExceptionWillBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsError())
                .BDDfy();
        }

        [Fact]
        public void ShouldSetRequestId()
        {
            var config = new OcelotConfiguration(null, null, null, "requestidkey");

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheRequestIdIsSet("RequestId", "1234"))
                .BDDfy();
        }

        [Fact]
        public void ShouldNotSetRequestId()
        {
            var config = new OcelotConfiguration(null, null, null, null);

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheRequestIdIsNotSet())
                .BDDfy();
        }

        private void TheRequestIdIsSet(string key, string value)
        {
            ScopedRepository.Verify(x => x.Add<string>(key, value), Times.Once);
        }

        private void GivenTheConfigurationIs(IOcelotConfiguration config)
        {
            var response = new Ocelot.Responses.OkResponse<IOcelotConfiguration>(config);
            _provider
                .Setup(x => x.Get()).ReturnsAsync(response);
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(ScopedRepository.Object);
            services.AddSingleton<IOcelotConfigurationProvider>(_provider.Object);
        }

        protected override void GivenTheTestServerPipelineIsConfigured(IApplicationBuilder app)
        {
            app.UseExceptionHandlerMiddleware();
            app.Run(DownstreamExceptionSimulator);
        }

        private async Task DownstreamExceptionSimulator(HttpContext context)
        {
            await Task.CompletedTask;

            if (_shouldThrowAnException)
            {
                throw new Exception("BOOM");
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        private void GivenAnExceptionWillNotBeThrownDownstream()
        {
            _shouldThrowAnException = false;
        }

        private void GivenAnExceptionWillBeThrownDownstream()
        {
            _shouldThrowAnException = true;
        }

        private void ThenTheResponseIsOk()
        {
            ResponseMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private void ThenTheResponseIsError()
        {
            ResponseMessage.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        }
    }
}