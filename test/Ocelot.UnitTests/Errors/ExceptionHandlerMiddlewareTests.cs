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

    public class ExceptionHandlerMiddlewareTests : ServerHostedMiddlewareTest
    {
        bool _shouldThrowAnException = false;

        public ExceptionHandlerMiddlewareTests()
        {
            GivenTheTestServerIsConfigured();
        }
        
        [Fact]
        public void NoDownstreamException()
        {
            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsOk())
                .BDDfy();
        }

        [Fact]
        public void DownstreamException()
        {
            this.Given(_ => GivenAnExceptionWillBeThrownDownstream())
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsError())
                .BDDfy();
        }

        protected override void GivenTheTestServerServicesAreConfigured(IServiceCollection services)
        {
            services.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
            services.AddLogging();
            services.AddSingleton(ScopedRepository.Object);
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