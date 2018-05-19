using System;
using Butterfly.Client.Tracing;
using Butterfly.OpenTracing;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Requester;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class HttpHandlerOptionsCreatorTests
    {
        private IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private FileReRoute _fileReRoute;
        private HttpHandlerOptions _httpHandlerOptions;
        private IServiceTracer _serviceTracer;

        public HttpHandlerOptionsCreatorTests()
        {
            _serviceTracer = new FakeServiceTracer();
            _httpHandlerOptionsCreator = new HttpHandlerOptionsCreator(_serviceTracer);
        }

        [Fact]
        public void should_not_use_tracing_if_fake_tracer_registered()
        {
            var fileReRoute = new FileReRoute 
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    UseTracing = true
                }
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void should_use_tracing_if_real_tracer_registered()
        {
            var fileReRoute = new FileReRoute 
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    UseTracing = true
                }
            };

            var expectedOptions = new HttpHandlerOptions(false, false, true);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .And(x => GivenARealTracer())
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_options_with_useCookie_false_and_allowAutoRedirect_true_as_default()
        {
            var fileReRoute = new FileReRoute();
            var expectedOptions = new HttpHandlerOptions(false, false, false);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_options_with_specified_useCookie_and_allowAutoRedirect()
        {
            var fileReRoute = new FileReRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    AllowAutoRedirect = false,
                    UseCookieContainer = false,
                    UseTracing = false
                }
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        private void GivenTheFollowing(FileReRoute fileReRoute)
        {
            _fileReRoute = fileReRoute;
        }

        private void WhenICreateHttpHandlerOptions()
        {
            _httpHandlerOptions = _httpHandlerOptionsCreator.Create(_fileReRoute.HttpHandlerOptions);
        }

        private void ThenTheFollowingOptionsReturned(HttpHandlerOptions expected)
        {
            _httpHandlerOptions.ShouldNotBeNull();
            _httpHandlerOptions.AllowAutoRedirect.ShouldBe(expected.AllowAutoRedirect);
            _httpHandlerOptions.UseCookieContainer.ShouldBe(expected.UseCookieContainer);
            _httpHandlerOptions.UseTracing.ShouldBe(expected.UseTracing);
        }

        private void GivenARealTracer()
        {
            var tracer = new RealTracer();
            _httpHandlerOptionsCreator = new HttpHandlerOptionsCreator(tracer);
        }

        class RealTracer : IServiceTracer
        {
            public ITracer Tracer => throw new NotImplementedException();

            public string ServiceName => throw new NotImplementedException();

            public string Environment => throw new NotImplementedException();

            public string Identity => throw new NotImplementedException();

            public ISpan Start(ISpanBuilder spanBuilder)
            {
                throw new NotImplementedException();
            }
        }
    }
}
