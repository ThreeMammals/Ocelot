using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Shouldly;
using System;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Logging;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class HttpHandlerOptionsCreatorTests
    {
        private IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private FileReRoute _fileReRoute;
        private HttpHandlerOptions _httpHandlerOptions;
        private IServiceProvider _serviceProvider;
        private IServiceCollection _serviceCollection;

        public HttpHandlerOptionsCreatorTests()
        {
            _serviceCollection = new ServiceCollection();
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _httpHandlerOptionsCreator = new HttpHandlerOptionsCreator(_serviceProvider);
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

            var expectedOptions = new HttpHandlerOptions(false, false, false, true);

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

            var expectedOptions = new HttpHandlerOptions(false, false, true, true);

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
            var expectedOptions = new HttpHandlerOptions(false, false, false, true);

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

            var expectedOptions = new HttpHandlerOptions(false, false, false, true);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_options_with_useproxy_true_as_default()
        {
            var fileReRoute = new FileReRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions()
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, true);

            this.Given(x => GivenTheFollowing(fileReRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void should_create_options_with_specified_useproxy()
        {
            var fileReRoute = new FileReRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    UseProxy = false
                }
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, false);

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
            _httpHandlerOptions.UseProxy.ShouldBe(expected.UseProxy);
        }

        private void GivenARealTracer()
        {
            var tracer = new FakeTracer();
            _serviceCollection.AddSingleton<ITracer, FakeTracer>();
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _httpHandlerOptionsCreator = new HttpHandlerOptionsCreator(_serviceProvider);
        }

        private class FakeTracer : ITracer
        {
            public void Event(HttpContext httpContext, string @event)
            {
                throw new NotImplementedException();
            }

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken, Action<string> addTraceIdToRepo,
                Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> baseSendAsync)
            {
                throw new NotImplementedException();
            }
        }
    }
}
