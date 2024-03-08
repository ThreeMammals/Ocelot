using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Ocelot.Provider.Polly.v7;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Polly.Wrap;

namespace Ocelot.UnitTests.Polly;

public class PollyQoSProviderTests
{
    [Fact]
    public void Should_build()
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(100)
            .WithExceptionsAllowedBeforeBreaking(1)
            .WithDurationOfBreak(200)
            .Build();
        var route = new DownstreamRouteBuilder().WithQosOptions(options)
            .Build();
        var factory = new Mock<IOcelotLoggerFactory>();
        var pollyQoSProvider = new PollyQoSProvider(factory.Object);
        var policy = pollyQoSProvider.GetPollyPolicyWrapper(route).ShouldNotBeNull()
            .AsyncPollyPolicy.ShouldNotBeNull();
        policy.ShouldNotBeNull();
    }

    [Fact]
    public void should_build_and_wrap_contains_two_policies()
    {
        var pollyQosProvider = PollyQoSProviderFactory();
        var pollyPolicyWrapper = PolicyWrapperFactory("/", pollyQosProvider);
        var policy = pollyPolicyWrapper.AsyncPollyPolicy;

        if (policy is AsyncPolicyWrap<HttpResponseMessage> policyWrap)
        {
            policyWrap.ShouldNotBeNull();
            var policies = policyWrap.GetPolicies().ToList();

            policies.Count.ShouldBe(2);
            var circuitBreakerFound = false;
            var timeoutPolicyFound = false;

            foreach(var currentPolicy in policies)
            {
                currentPolicy.ShouldNotBeNull();
                var convertedPolicy = (IAsyncPolicy<HttpResponseMessage>)currentPolicy;

                switch (convertedPolicy)
                {
                    case AsyncCircuitBreakerPolicy<HttpResponseMessage>:
                        circuitBreakerFound = true;
                        continue;
                    case AsyncTimeoutPolicy<HttpResponseMessage>:
                        timeoutPolicyFound = true;
                        break;
                }
            }

            Assert.True(circuitBreakerFound);
            Assert.True(timeoutPolicyFound);

            return;
        }

        Assert.Fail("policy is not AsyncPolicyWrap<HttpResponseMessage>");
    }

    [Fact]
    public void should_build_and_contains_one_policy_when_with_exceptions_allowed_before_breaking_is_zero()
    {
        var pollyQosProvider = PollyQoSProviderFactory();
        var pollyPolicyWrapper = PolicyWrapperFactory("/", pollyQosProvider, true);
        var policy = pollyPolicyWrapper.AsyncPollyPolicy;

        if (policy is AsyncTimeoutPolicy<HttpResponseMessage> convertedPolicy)
        {
            convertedPolicy.ShouldNotBeNull();
            return;
        }

        Assert.Fail("policy is not AsyncTimeoutPolicy<HttpResponseMessage>");
    }

    [Fact]
    public void should_return_same_circuit_breaker_for_given_route()
    {
        var pollyQosProvider = PollyQoSProviderFactory();
        var pollyPolicyWrapper = PolicyWrapperFactory("/", pollyQosProvider);
        var pollyPolicyWrapper2 = PolicyWrapperFactory("/", pollyQosProvider);
        pollyPolicyWrapper.ShouldBe(pollyPolicyWrapper2);
    }

    [Fact]
    public void should_return_different_circuit_breaker_for_two_different_routes()
    {
        var pollyQosProvider = PollyQoSProviderFactory();
        var pollyPolicyWrapper = PolicyWrapperFactory("/", pollyQosProvider);
        var pollyPolicyWrapper2 = PolicyWrapperFactory("/test", pollyQosProvider);
        pollyPolicyWrapper.ShouldNotBe(pollyPolicyWrapper2);
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
        var pollyPolicyWrapper = PolicyWrapperFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(errorCode);
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    [Fact]
    public async Task should_not_throw_broken_circuit_exception_if_status_code_ok()
    {
        var pollyPolicyWrapper = PolicyWrapperFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
    }

    [Fact(Skip = "TODO", DisplayName = "TODO " + nameof(should_throw_and_before_delay_should_not_allow_requests))]
    [Trait("TODO", "Fix after the release")]
    public async Task should_throw_and_before_delay_should_not_allow_requests()
    {
        var pollyPolicyWrapper = PolicyWrapperFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(200);

        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    [Fact]
    public async Task should_throw_but_after_delay_should_allow_one_more_internal_server_error()
    {
        var pollyPolicyWrapper = PolicyWrapperFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
                       await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(600);

        Assert.Equal(HttpStatusCode.InternalServerError, (await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
    }

    [Fact]
    public async Task should_throw_but_after_delay_should_allow_one_more_internal_server_error_and_throw()
    {
        var pollyPolicyWrapper = PolicyWrapperFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(600);

        Assert.Equal(HttpStatusCode.InternalServerError, (await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response))).StatusCode);
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    [Fact]
    public async Task should_throw_but_after_delay_should_allow_one_more_ok_request_and_put_counter_back_to_zero()
    {
        var pollyPolicyWrapper = PolicyWrapperFactory("/", PollyQoSProviderFactory());

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));

        await Task.Delay(600);

        var response2 = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response2))).StatusCode);
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(async () =>
            await pollyPolicyWrapper.AsyncPollyPolicy.ExecuteAsync(() => Task.FromResult(response)));
    }

    private PollyQoSProvider PollyQoSProviderFactory()
    {
        var factory = new Mock<IOcelotLoggerFactory>();
        factory.Setup(x => x.CreateLogger<PollyQoSProvider>())
            .Returns(new Mock<IOcelotLogger>().Object);

        var pollyQoSProvider = new PollyQoSProvider(factory.Object);
        return pollyQoSProvider;
    }

    private static PollyPolicyWrapper<HttpResponseMessage> PolicyWrapperFactory(string routeTemplate, PollyQoSProvider pollyQoSProvider, bool inactiveExceptionsAllowedBeforeBreaking = false)
    {
        var options = new QoSOptionsBuilder()
            .WithTimeoutValue(5000)
            .WithExceptionsAllowedBeforeBreaking(inactiveExceptionsAllowedBeforeBreaking ? 0 : 2)
            .WithDurationOfBreak(300)
            .Build();

        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate(routeTemplate)
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue(routeTemplate).Build();

        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath).Build();

        var pollyPolicyWrapper = pollyQoSProvider.GetPollyPolicyWrapper(route).ShouldNotBeNull();
        pollyPolicyWrapper.ShouldNotBeNull();
        pollyPolicyWrapper.AsyncPollyPolicy.ShouldNotBeNull();

        return pollyPolicyWrapper;
    }
}
