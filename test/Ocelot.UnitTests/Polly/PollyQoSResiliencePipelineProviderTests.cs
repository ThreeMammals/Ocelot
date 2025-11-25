using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Testing;
using Polly.Timeout;
using _TimeoutStrategy_ = Ocelot.Provider.Polly.TimeoutStrategy;

namespace Ocelot.UnitTests.Polly;

public class PollyQoSResiliencePipelineProviderTests
{
    #region Constructor
    [Theory]
    [Trait("PR", "2073")]
    [InlineData(0)]
    [InlineData(1)]
    public void Ctor_NoLoggerParam_ShouldThrowArgumentNullException(int branch)
    {
        // Arrange
        IOcelotLoggerFactory factory = null;
        if (branch >= 0)
            factory = null;
        if (branch >= 1)
            factory = Mock.Of<IOcelotLoggerFactory>();

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => new PollyQoSResiliencePipelineProvider(factory, null));

        // Assert
        Assert.Equal("loggerFactory", ex.ParamName);
    }

    [Fact]
    [Trait("PR", "2073")]
    public void Ctor_NoRegistryParam_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = new Mock<IOcelotLoggerFactory>();
        factory.Setup(x => x.CreateLogger<PollyQoSResiliencePipelineProvider>())
            .Returns(Mock.Of<IOcelotLogger>());
        ResiliencePipelineRegistry<OcelotResiliencePipelineKey> noRegistry = null; // !!!

        // Act
        var ex = Assert.Throws<ArgumentNullException>(
            () => new PollyQoSResiliencePipelineProvider(factory.Object, noRegistry));

        // Assert
        Assert.Equal("registry", ex.ParamName);
    }
    #endregion

    [Fact]
    public void ShouldBuild()
    {
        // Arrange
        var options = new QoSOptions()
        {
            DurationOfBreak = CircuitBreakerStrategy.LowBreakDuration + 1, // 0.5s, minimum required by Polly
            MinimumThroughput = 2, // 2 is the minimum required by Polly
            TimeoutValue = 1000, // 10ms, minimum required by Polly
        };
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();
        var provider = GivenProvider();

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        resiliencePipeline.ShouldBeOfType<ResiliencePipeline<HttpResponseMessage>>();
        resiliencePipeline.ShouldNotBe(ResiliencePipeline<HttpResponseMessage>.Empty);
    }

    [Fact]
    [Trait("Bug", "2085")]
    public void ShouldNotBuild_ReturnedEmpty()
    {
        // Arrange
        var options = new QoSOptions(); // empty options
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();
        var provider = GivenProvider();

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        resiliencePipeline.ShouldBeOfType<ResiliencePipeline<HttpResponseMessage>>();
        resiliencePipeline.ShouldBe(ResiliencePipeline<HttpResponseMessage>.Empty);
    }

    [Theory]
    [Trait("Bug", "2085")]
    [InlineData(CircuitBreakerStrategy.LowBreakDuration - 1, CircuitBreakerStrategy.DefaultBreakDuration)] // default
    [InlineData(CircuitBreakerStrategy.LowBreakDuration, CircuitBreakerStrategy.DefaultBreakDuration)] // default
    [InlineData(CircuitBreakerStrategy.LowBreakDuration + 1, CircuitBreakerStrategy.LowBreakDuration + 1)] // not default, exact
    public void ShouldBuild_WithDefaultBreakDuration(int durationOfBreak, int expectedMillisecons)
    {
        // Arrange
        var options = new QoSOptions()
        {
            DurationOfBreak = durationOfBreak, // 0.5s, minimum required by Polly
            MinimumThroughput = 2, // 2 is the minimum required by Polly
            TimeoutValue = 1000, // 10ms, minimum required by Polly
        };
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();
        var provider = GivenProvider();

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        resiliencePipeline.ShouldBeOfType<ResiliencePipeline<HttpResponseMessage>>();
        resiliencePipeline.ShouldNotBe(ResiliencePipeline<HttpResponseMessage>.Empty);
        var descriptor = resiliencePipeline.GetPipelineDescriptor();
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.Count.ShouldBe(2);
        descriptor.Strategies[0].Options.ShouldBeOfType<CircuitBreakerStrategyOptions<HttpResponseMessage>>();
        descriptor.Strategies[1].Options.ShouldBeOfType<TimeoutStrategyOptions>();
        var strategyOptions = descriptor.Strategies[0].Options as CircuitBreakerStrategyOptions<HttpResponseMessage>;
        strategyOptions.ShouldNotBeNull();
        strategyOptions.BreakDuration.ShouldBe(TimeSpan.FromMilliseconds(expectedMillisecons));
    }

    [Fact]
    public void Should_return_same_circuit_breaker_for_given_route()
    {
        // Arrange
        var provider = GivenProvider();
        var route1 = GivenDownstreamRoute("/");
        var route2 = GivenDownstreamRoute("/");

        // Act
        var resiliencePipeline1 = provider.GetResiliencePipeline(route1);
        var resiliencePipeline2 = provider.GetResiliencePipeline(route2);

        // Assert
        resiliencePipeline1.ShouldBe(resiliencePipeline2);

        // Act 2
        var resiliencePipeline3 = provider.GetResiliencePipeline(route1);

        // Assert 2
        resiliencePipeline3.ShouldBe(resiliencePipeline1);
        resiliencePipeline3.ShouldBe(resiliencePipeline2);
    }

    [Fact]
    public void Should_return_different_circuit_breaker_for_two_different_routes()
    {
        // Arrange
        var provider = GivenProvider();
        var route1 = GivenDownstreamRoute("/");
        var route2 = GivenDownstreamRoute("/test");

        // Act
        var resiliencePipeline1 = provider.GetResiliencePipeline(route1);
        var resiliencePipeline2 = provider.GetResiliencePipeline(route2);

        // Assert
        resiliencePipeline1.ShouldNotBe(resiliencePipeline2);
    }

    [Fact]
    [Trait("Bug", "2085")]
    public void ShouldBuild_ContainsTwoStrategies()
    {
        var pollyQoSResiliencePipelineProvider = GivenProvider();

        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = pollyQoSResiliencePipelineProvider.GetResiliencePipeline(route);
        resiliencePipeline.ShouldNotBeNull();

        var descriptor = resiliencePipeline.GetPipelineDescriptor();
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.Count.ShouldBe(2);
        descriptor.Strategies[0].Options.ShouldBeOfType<CircuitBreakerStrategyOptions<HttpResponseMessage>>();
        descriptor.Strategies[1].Options.ShouldBeOfType<TimeoutStrategyOptions>();
    }

    [Fact]
    public void Should_build_and_contains_one_policy_when_with_exceptions_allowed_before_breaking_is_zero()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/", 0); // get route with 0 exceptions allowed before breaking

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.GetPipelineDescriptor();

        // Assert
        resiliencePipeline.ShouldNotBeNull();
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.Count.ShouldBe(1);
        descriptor.Strategies.Single().Options.ShouldBeOfType<TimeoutStrategyOptions>();
    }

    [Fact]
    [Trait("Bug", "2085")]
    public async Task Should_throw_after_timeout()
    {
        // Arrange
        var provider = GivenProvider();
        const int OneSecond = 1000;
        var route = GivenDownstreamRoute("/", timeOut: OneSecond);
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var cancellationTokenSource = new CancellationTokenSource();

        // Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>

            // Act
            await resiliencePipeline.ExecuteAsync(async (cancellationToken) =>
            {
                await Task.Delay(OneSecond + 500, cancellationToken); // add 500ms to make sure it's timed out
                return response;
            },
            cancellationTokenSource.Token));
    }

    [Fact]
    [Trait("Bug", "2085")]
    public async Task Should_not_throw_before_timeout()
    {
        // Arrange
        var provider = GivenProvider();
        const int OneSecond = 1000;
        var route = GivenDownstreamRoute("/", timeOut: OneSecond);
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await resiliencePipeline.ExecuteAsync(async cancellationToken =>
        {
            await Task.Delay(OneSecond - 100, cancellationToken); // subtract 100ms to make sure it's not timed out
            return response;
        }, cancellationTokenSource.Token);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
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
    public async Task Should_throw_broken_circuit_exception_after_two_exceptions(HttpStatusCode errorCode)
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(errorCode);

        // Act
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_not_throw_broken_circuit_exception_if_status_code_ok()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act, Assert
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);
    }

    [Fact]
    public async Task Should_throw_and_before_delay_should_not_allow_requests()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(200);

        // Act, Assert 2
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_internal_server_error()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(6000);

        // Act 2
        var response2 = await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Assert 2
        Assert.Equal(HttpStatusCode.InternalServerError, response2.StatusCode);
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_internal_server_error_and_throw()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(6000);

        // Act, Assert 2
        Assert.Equal(HttpStatusCode.InternalServerError, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response))).StatusCode);

        // Act, Assert 3
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Fact]
    public async Task Should_throw_but_after_delay_should_allow_one_more_ok_request_and_put_counter_back_to_zero()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/");
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));

        // Act, Assert
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));

        await Task.Delay(10000);

        // Act, Assert 2
        var response2 = new HttpResponseMessage(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, (await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response2))).StatusCode);

        // Act, Assert 3
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response));
        await Assert.ThrowsAsync<BrokenCircuitException>(async () =>
            await resiliencePipeline.ExecuteAsync((_) => ValueTask.FromResult(response)));
    }

    [Theory]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(0)]
    public void ConfigureTimeout_NoQosTimeout_ShouldNotApplyTimeoutStrategy(int? timeout)
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/", timeOut: timeout);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Single().Options.ShouldNotBeOfType<TimeoutStrategyOptions>();
    }

    [Fact]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    public void ConfigureTimeout_HasInvalidTimeout_ShouldUseDefaultTimeout()
    {
        // Arrange
        int? invalidTimeout = _TimeoutStrategy_.LowTimeout - 1;
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/", 0, invalidTimeout);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Single().Options.ShouldBeOfType<TimeoutStrategyOptions>();
        var actual = descriptor.Strategies.Single().Options as TimeoutStrategyOptions;
        Assert.Equal(_TimeoutStrategy_.DefaultTimeout, (int)actual.Timeout.TotalMilliseconds);
    }

    [Theory]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    [InlineData(null)]
    [InlineData(_TimeoutStrategy_.LowTimeout - 1)]
    public void ConfigureTimeout_ValidationIsAlwaysTrue_ShouldUseDefaultTimeout(int? invalidTimeout)
    {
        // Arrange
        var provider = GivenProvider<FakeTimeoutProvider>();
        var route = GivenDownstreamRoute("/", timeOut: invalidTimeout);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Count.ShouldBe(2);
        var strategy = descriptor.Strategies.SingleOrDefault(x => x.Options.GetType() == typeof(TimeoutStrategyOptions)).ShouldNotBeNull();
        var actual = strategy.Options as TimeoutStrategyOptions;
        Assert.Equal(_TimeoutStrategy_.DefaultTimeout, (int)actual.Timeout.TotalMilliseconds);
    }

    [Theory]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    [InlineData(null, "Route '/' has invalid QoSOptions for Polly's Timeout strategy. Specifically, the timeout is disabled because the TimeoutValue (?) is either undefined, negative, or zero.")]
    [InlineData(-1, "Route '/' has invalid QoSOptions for Polly's Timeout strategy. Specifically, the timeout is disabled because the TimeoutValue (-1) is either undefined, negative, or zero.")]
    public void IsConfigurationValidForTimeout_InvalidValue_ShouldLogError(int? invalidTimeout, string expectedMessage)
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/", timeOut: invalidTimeout);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Single().Options.ShouldBeOfType<CircuitBreakerStrategyOptions<HttpResponseMessage>>();
        _logger.Verify(x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()), Times.Once());
        var message = _funcMessage?.Invoke() ?? string.Empty;
        message.ShouldBe(expectedMessage);
    }

    [Fact]
    [Trait("PR", "2073")]
    [Trait("Feat", "1314")]
    [Trait("Feat", "1869")]
    public void IsConfigurationValidForTimeout_ValidValueButIsNotValidTimeout_ShouldLogWarning()
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/", timeOut: _TimeoutStrategy_.LowTimeout - 1);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Count.ShouldBe(2);
        var strategy = descriptor.Strategies.SingleOrDefault(x => x.Options.GetType() == typeof(TimeoutStrategyOptions)).ShouldNotBeNull();
        var actual = strategy.Options as TimeoutStrategyOptions;
        Assert.Equal(_TimeoutStrategy_.DefaultTimeout, (int)actual.Timeout.TotalMilliseconds);
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()), Times.Once());
        var message = _funcMessage?.Invoke() ?? string.Empty;
        message.ShouldBe("Route '/' has invalid QoSOptions for Polly's Timeout strategy. Specifically, the Timeout value (9) is outside the valid range (10 to 86400000 milliseconds). Therefore, ensure the value falls within this range; otherwise, the default value (30000) will be substituted.");
    }

    [Fact]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    public void The_ReturnedWithMessagePosition()
    {
        // Arrange 1
        List<Func<string>> warnings = new();
        static string msg1() => "A";
        warnings.Add(msg1);

        // Act, Assert 1
        PollyQoSResiliencePipelineProvider.The(warnings, msg1).ShouldBe("the");

        // Arrange 2
        static string msg2() => "B";
        warnings.Add(msg2);

        // Act, Assert 2
        var nl = Environment.NewLine;
        PollyQoSResiliencePipelineProvider.The(warnings, msg1).ShouldBe($"{nl}  1. The");
        PollyQoSResiliencePipelineProvider.The(warnings, msg2).ShouldBe($"{nl}  2. The");
    }

    [Theory]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    [InlineData(null, "Route '/' has invalid QoSOptions for Polly's Circuit Breaker strategy. Specifically, the circuit breaker is disabled because the MinimumThroughput value (?) is either undefined, negative, or zero.")]
    [InlineData(-1, "Route '/' has invalid QoSOptions for Polly's Circuit Breaker strategy. Specifically, the circuit breaker is disabled because the MinimumThroughput value (-1) is either undefined, negative, or zero.")]
    public void IsConfigurationValidForCircuitBreaker_InvalidValue_ShouldLogError(int? exceptionsAllowedBeforeBreaking, string expectedMessage)
    {
        // Arrange
        var provider = GivenProvider();
        var route = GivenDownstreamRoute("/", exceptionsAllowedBeforeBreaking, 555);

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Single().Options.ShouldBeOfType<TimeoutStrategyOptions>();
        _logger.Verify(x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()), Times.Once());
        var message = _funcMessage?.Invoke() ?? string.Empty;
        message.ShouldBe(expectedMessage);
    }

    [Fact]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    public void IsConfigurationValidForCircuitBreaker_InvalidOptions_ShouldLogWarning()
    {
        // Arrange
        var provider = GivenProvider();
        var invalidOptions = new QoSOptions()
        {
            MinimumThroughput = 1, // invalid
            DurationOfBreak = 0,
            FailureRatio = 0.0D,
            SamplingDuration = 0,
            TimeoutValue = _TimeoutStrategy_.DefTimeout, // but timeout is valid
        };
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(invalidOptions)
            .WithUpstreamPathTemplate(new("/", 1, false, "/"))
            .Build();

        // Act
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Count.ShouldBe(2);
        descriptor.Strategies.Single(x => x.Options.GetType() == typeof(CircuitBreakerStrategyOptions<HttpResponseMessage>));
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()), Times.Once());
        var message = _funcMessage?.Invoke() ?? string.Empty;
        message.ShouldBe(@"Route '/' has invalid QoSOptions for Polly's Circuit Breaker strategy. Specifically, 
  1. The MinimumThroughput value (1) is less than the required LowMinimumThroughput threshold (2). Therefore, increase MinimumThroughput to at least 2 or higher. Until then, the default value (100) will be substituted.
  2. The BreakDuration value (0) is outside the valid range (500 to 86400000 milliseconds). Therefore, ensure the value falls within this range; otherwise, the default value (5000) will be substituted.
  3. The FailureRatio value (0) is outside the valid range (0 to 1). Therefore, ensure the ratio falls within this range; otherwise, the default value (0.1) will be substituted.
  4. The SamplingDuration value (0) is outside the valid range (500 to 86400000 milliseconds). Therefore, ensure the duration falls within this range; otherwise, the default value (30000) will be substituted.");
    }

    [Fact]
    [Trait("PR", "2081")]
    [Trait("Feat", "2080")]
    public void IsConfigurationValidForCircuitBreaker_NullOptions_ShouldLogWarning()
    {
        // Arrange
        var provider = GivenProvider();
        var nullOptions = new QoSOptions(1, null) // invalid
        {
            TimeoutValue = _TimeoutStrategy_.DefTimeout, // but timeout is valid
        };
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(nullOptions)
            .WithUpstreamPathTemplate(new("/", 1, false, "/"))
            .Build();

        // Act 2
        var resiliencePipeline = provider.GetResiliencePipeline(route);
        var descriptor = resiliencePipeline.ShouldNotBeNull().GetPipelineDescriptor();

        // Assert 2
        descriptor.ShouldNotBeNull();
        descriptor.Strategies.ShouldNotBeEmpty();
        descriptor.Strategies.Count.ShouldBe(2);
        descriptor.Strategies.ShouldContain(x => x.Options.GetType() == typeof(CircuitBreakerStrategyOptions<HttpResponseMessage>));
        _logger.Verify(x => x.LogWarning(It.IsAny<Func<string>>()), Times.Once());
        var message = _funcMessage?.Invoke() ?? string.Empty;
        message.ShouldBe("Route '/' has invalid QoSOptions for Polly's Circuit Breaker strategy. Specifically, the MinimumThroughput value (1) is less than the required LowMinimumThroughput threshold (2). Therefore, increase MinimumThroughput to at least 2 or higher. Until then, the default value (100) will be substituted.");
    }

    private Func<string> _funcMessage;
    private readonly Mock<IOcelotLogger> _logger = new();
    private PollyQoSResiliencePipelineProvider GivenProvider() => GivenProvider<PollyQoSResiliencePipelineProvider>();
    private PollyQoSResiliencePipelineProvider GivenProvider<T>()
        where T : PollyQoSResiliencePipelineProvider
    {
        _logger.Setup(x => x.LogError(It.IsAny<Func<string>>(), It.IsAny<Exception>()))
            .Callback<Func<string>, Exception>((f, _) => _funcMessage = f);
        _logger.Setup(x => x.LogWarning(It.IsAny<Func<string>>()))
            .Callback<Func<string>>((f) => _funcMessage = f);
        var loggerFactory = new Mock<IOcelotLoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger<PollyQoSResiliencePipelineProvider>())
            .Returns(_logger.Object);
        var registry = new ResiliencePipelineRegistry<OcelotResiliencePipelineKey>();
        return (T)Activator.CreateInstance(typeof(T), loggerFactory.Object, registry);
    }

    private static DownstreamRoute GivenDownstreamRoute(string routeTemplate, int? exceptionsAllowedBeforeBreaking = 2, int? timeOut = 10000)
    {
        var options = new QoSOptions(exceptionsAllowedBeforeBreaking, 5000)
        {
            TimeoutValue = timeOut,
        };
        var upstreamPath = new UpstreamPathTemplateBuilder()
            .WithTemplate(routeTemplate)
            .WithContainsQueryString(false)
            .WithPriority(1)
            .WithOriginalValue(routeTemplate)
            .Build();
        return new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .WithUpstreamPathTemplate(upstreamPath)
            .WithLoadBalancerKey($"{routeTemplate}|no-host|localhost:20005,localhost:20007|no-svc-ns|no-svc-name|LeastConnection|no-lb-key")
            .Build();
    }
}

internal class FakeTimeoutProvider : PollyQoSResiliencePipelineProvider
{
    public FakeTimeoutProvider(IOcelotLoggerFactory loggerFactory, ResiliencePipelineRegistry<OcelotResiliencePipelineKey> registry)
        : base(loggerFactory, registry) { }

    protected override bool IsConfigurationValidForTimeout(DownstreamRoute route) => true;
}
