﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Configuration
{
    public class HttpHandlerOptionsCreatorTests : UnitTest
    {
        private IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private FileRoute _fileRoute;
        private HttpHandlerOptions _httpHandlerOptions;
        private IServiceProvider _serviceProvider;
        private readonly IServiceCollection _serviceCollection;

        public HttpHandlerOptionsCreatorTests()
        {
            _serviceCollection = new ServiceCollection();
            _serviceProvider = _serviceCollection.BuildServiceProvider(true);
            _httpHandlerOptionsCreator = new HttpHandlerOptionsCreator(_serviceProvider);
        }

        [Fact]
        public void Should_not_use_tracing_if_fake_tracer_registered()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    UseTracing = true,
                },
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_use_tracing_if_real_tracer_registered()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    UseTracing = true,
                },
            };

            var expectedOptions = new HttpHandlerOptions(false, false, true, true, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .And(x => GivenARealTracer())
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_create_options_with_useCookie_false_and_allowAutoRedirect_true_as_default()
        {
            var fileRoute = new FileRoute();
            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_create_options_with_specified_useCookie_and_allowAutoRedirect()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    AllowAutoRedirect = false,
                    UseCookieContainer = false,
                    UseTracing = false,
                },
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_create_options_with_useproxy_true_as_default()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions(),
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_create_options_with_specified_useproxy()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    UseProxy = false,
                },
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, false, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_create_options_with_specified_MaxConnectionsPerServer()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    MaxConnectionsPerServer = 10,
                },
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, true, 10, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_create_options_fixing_specified_MaxConnectionsPerServer_range()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    MaxConnectionsPerServer = -1,
                },
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        public void Should_create_options_fixing_specified_MaxConnectionsPerServer_range_when_zero()
        {
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new FileHttpHandlerOptions
                {
                    MaxConnectionsPerServer = 0,
                },
            };

            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime, false);

            this.Given(x => GivenTheFollowing(fileRoute))
                .When(x => WhenICreateHttpHandlerOptions())
                .Then(x => ThenTheFollowingOptionsReturned(expectedOptions))
                .BDDfy();
        }

        [Fact]
        [Trait("Feat", "657")]
        public void Should_create_options_with_useDefaultCredentials_false_as_default()
        {
            // Arrange
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new(),
            };
            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime,
                useDefaultCredentials: false);
            GivenTheFollowing(fileRoute);

            // Act
            WhenICreateHttpHandlerOptions();

            // Assert
            ThenTheFollowingOptionsReturned(expectedOptions);
        }

        [Fact]
        [Trait("Feat", "657")]
        public void Should_create_options_with_UseDefaultCredentials_true_if_set()
        {
            // Arrange
            var fileRoute = new FileRoute
            {
                HttpHandlerOptions = new()
                {
                    UseDefaultCredentials = true,
                },
            };
            var expectedOptions = new HttpHandlerOptions(false, false, false, true, int.MaxValue, DefaultPooledConnectionLifeTime,
                useDefaultCredentials: true);
            GivenTheFollowing(fileRoute);

            // Act
            WhenICreateHttpHandlerOptions();

            // Assert
            ThenTheFollowingOptionsReturned(expectedOptions);
        }

        private void GivenTheFollowing(FileRoute fileRoute)
        {
            _fileRoute = fileRoute;
        }

        private void WhenICreateHttpHandlerOptions()
        {
            _httpHandlerOptions = _httpHandlerOptionsCreator.Create(_fileRoute.HttpHandlerOptions);
        }

        private void ThenTheFollowingOptionsReturned(HttpHandlerOptions expected)
        {
            _httpHandlerOptions.ShouldNotBeNull();
            _httpHandlerOptions.AllowAutoRedirect.ShouldBe(expected.AllowAutoRedirect);
            _httpHandlerOptions.UseCookieContainer.ShouldBe(expected.UseCookieContainer);
            _httpHandlerOptions.UseTracing.ShouldBe(expected.UseTracing);
            _httpHandlerOptions.UseProxy.ShouldBe(expected.UseProxy);
            _httpHandlerOptions.MaxConnectionsPerServer.ShouldBe(expected.MaxConnectionsPerServer);
            _httpHandlerOptions.UseDefaultCredentials.ShouldBe(expected.UseDefaultCredentials);
        }

        private void GivenARealTracer()
        {
            _serviceCollection.AddSingleton<ITracer, FakeTracer>();
            _serviceProvider = _serviceCollection.BuildServiceProvider(true);
            _httpHandlerOptionsCreator = new HttpHandlerOptionsCreator(_serviceProvider);
        }

        /// <summary>
        /// 120 seconds.
        /// </summary>
        private static TimeSpan DefaultPooledConnectionLifeTime => TimeSpan.FromSeconds(HttpHandlerOptionsCreator.DefaultPooledConnectionLifetimeSeconds);

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
