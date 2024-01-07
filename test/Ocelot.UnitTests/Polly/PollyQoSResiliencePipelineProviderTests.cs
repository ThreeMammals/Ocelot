using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;

using Polly.CircuitBreaker;
using Polly.Testing;
using Polly.Timeout;

namespace Ocelot.UnitTests.Polly
{
    public class PollyQoSResiliencePipelineProviderTests
    {
        [Fact]
        public void Should_build()
        {
            var options = new QoSOptionsBuilder()
                .WithTimeoutValue(1000) // 1s, minimum required by Polly
                .WithExceptionsAllowedBeforeBreaking(1)
                .WithDurationOfBreak(500) // 0.5s, minimum required by Polly
                .Build();

            var route = new DownstreamRouteBuilder()
                .WithQosOptions(options)
                .Build();

            var factory = new Mock<IOcelotLoggerFactory>();
            var pollyQoSProvider = new PollyQoSResiliencePipelineProvider(factory.Object);
            var policy = pollyQoSProvider.GetResiliencePipeline(route).ShouldNotBeNull();
            policy.ShouldNotBeNull();
        }

        [Fact]
        public void should_return_same_circuit_breaker_for_given_route()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();
            var route1 = DownstreamRouteFactory("/");
            var route2 = DownstreamRouteFactory("/");

            var pollyPolicy1 = pollyQosProvider.GetResiliencePipeline(route1);
            var pollyPolicy2 = pollyQosProvider.GetResiliencePipeline(route2);
            pollyPolicy1.ShouldBe(pollyPolicy2);
        }

        [Fact]
        public void should_return_different_circuit_breaker_for_two_different_routes()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();
            var route1 = DownstreamRouteFactory("/");
            var route2 = DownstreamRouteFactory("/test");

            var resiliencePipeline1 = pollyQosProvider.GetResiliencePipeline(route1);
            var resiliencePipeline2 = pollyQosProvider.GetResiliencePipeline(route2);

            resiliencePipeline1.ShouldNotBe(resiliencePipeline2);
        }

        [Fact]
        public void should_build_and_wrap_contains_two_policies()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();

            var route = DownstreamRouteFactory("/");
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);
            resiliencePipeline.ShouldNotBeNull();

            var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();
            resiliencePipelineDescriptor.ShouldNotBeNull();
            resiliencePipelineDescriptor.Strategies.Count.ShouldBe(2);
            resiliencePipelineDescriptor.Strategies[0].Options.ShouldBeOfType<TimeoutStrategyOptions>();
            resiliencePipelineDescriptor.Strategies[1].Options.ShouldBeOfType<CircuitBreakerStrategyOptions<HttpResponseMessage>>();
        }

        [Fact]
        public void should_build_and_contains_one_policy_when_with_exceptions_allowed_before_breaking_is_zero()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();
            // get route with 0 exceptions allowed before breaking
            var route = DownstreamRouteFactory("/", true);
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);
            resiliencePipeline.ShouldNotBeNull();

            var resiliencePipelineDescriptor = resiliencePipeline.GetPipelineDescriptor();
            resiliencePipelineDescriptor.ShouldNotBeNull();
            resiliencePipelineDescriptor.Strategies.Count.ShouldBe(1);
            resiliencePipelineDescriptor.Strategies.Single().Options.ShouldBeOfType<TimeoutStrategyOptions>();
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.NotImplemented)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        [InlineData(HttpStatusCode.HttpVersionNotSupported)]
        [InlineData(HttpStatusCode.VariantAlsoNegotiates)]
        [InlineData(HttpStatusCode.InsufficientStorage)]
        [InlineData(HttpStatusCode.LoopDetected)]
        public async Task should_throw_broken_circuit_exception_after_two_exceptions(HttpStatusCode errorCode)
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();

            var route = DownstreamRouteFactory("/");
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);

            var response = new HttpResponseMessage(errorCode);
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
        }

        [Fact]
        public async Task should_not_throw_broken_circuit_exception_if_status_code_ok()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();

            var route = DownstreamRouteFactory("/");
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        }

        [Fact]
        public async Task should_throw_and_before_delay_should_not_allow_requests()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();

            var route = DownstreamRouteFactory("/");
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);

            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

            await Task.Delay(200);

            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
        }

        [Fact]
        public async Task should_throw_but_after_delay_should_allow_one_more_internal_server_error()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();

            var route = DownstreamRouteFactory("/");
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);

            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

            await Task.Delay(600);

            Assert.Equal(HttpStatusCode.InternalServerError, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        }

        [Fact]
        public async Task should_throw_but_after_delay_should_allow_one_more_internal_server_error_and_throw()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();

            var route = DownstreamRouteFactory("/");
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);

            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

            await Task.Delay(600);

            Assert.Equal(HttpStatusCode.InternalServerError, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
        }

        [Fact]
        public async Task should_throw_but_after_delay_should_allow_one_more_ok_request_and_put_counter_back_to_zero()
        {
            var pollyQosProvider = PollyQoSResiliencePipelineProviderFactory();

            var route = DownstreamRouteFactory("/");
            var resiliencePipeline = pollyQosProvider.GetResiliencePipeline(route);

            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

            await Task.Delay(600);

            var response2 = new HttpResponseMessage(HttpStatusCode.OK);
            Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response2))).StatusCode);
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
                await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
        }

        private static PollyQoSResiliencePipelineProvider PollyQoSResiliencePipelineProviderFactory()
        {
            var factory = new Mock<IOcelotLoggerFactory>();
            factory.Setup(x => x.CreateLogger<PollyQoSResiliencePipelineProvider>())
                .Returns(new Mock<IOcelotLogger>().Object);

            var pollyQoSProvider = new PollyQoSResiliencePipelineProvider(factory.Object);
            return pollyQoSProvider;
        }

        private static DownstreamRoute DownstreamRouteFactory(string routeTemplate, bool inactiveExceptionsAllowedBeforeBreaking = false)
        {
            var options = new QoSOptionsBuilder()
                .WithTimeoutValue(5000)
                .WithExceptionsAllowedBeforeBreaking(inactiveExceptionsAllowedBeforeBreaking ? 0 : 2)
                .WithDurationOfBreak(500)
                .Build();

            var upstreamPath = new UpstreamPathTemplateBuilder()
                .WithTemplate(routeTemplate)
                .WithContainsQueryString(false)
                .WithPriority(1)
                .WithOriginalValue(routeTemplate).Build();

            var route = new DownstreamRouteBuilder()
                .WithQosOptions(options)
                .WithUpstreamPathTemplate(upstreamPath).Build();

            return route;
        }
    }
}
