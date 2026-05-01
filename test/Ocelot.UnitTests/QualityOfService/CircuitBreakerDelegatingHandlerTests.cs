using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Infrastructure.DesignPatterns;
using Ocelot.Logging;
using Ocelot.QualityOfService;

namespace Ocelot.UnitTests.QualityOfService;

[Trait("Feat", "2384")] // https://github.com/ThreeMammals/Ocelot/issues/2384
[Trait("PR", "2385")] // https://github.com/ThreeMammals/Ocelot/pull/2385
public class CircuitBreakerDelegatingHandlerTests : UnitTest
{
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;

    public CircuitBreakerDelegatingHandlerTests()
    {
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _loggerFactory.Setup(x => x.CreateLogger<CircuitBreakerDelegatingHandler>())
            .Returns(_logger.Object);
    }

    private static CancellationToken CancelMe { get => TestContext.Current.CancellationToken; } 

    private CircuitBreakerDelegatingHandler CreateHandler(QoSOptions opts, HttpMessageHandler innerHandler)
    {
        var route = new DownstreamRouteBuilder().WithQosOptions(opts).Build();
        var handler = new CircuitBreakerDelegatingHandler(route, _loggerFactory.Object)
        {
            InnerHandler = innerHandler,
        };
        return handler;
    }

    private static Task<HttpResponseMessage> SendAsync(DelegatingHandler handler, HttpRequestMessage request, CancellationToken ct = default)
    {
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        return invoker.SendAsync(request, ct);
    }

    // --------------------------------------------------------
    //  Constants
    // --------------------------------------------------------

    [Fact]
    public void LowBreakDuration_Is500()
        => Assert.Equal(500, CircuitBreakerDelegatingHandler.LowBreakDuration);

    [Fact]
    public void DefaultBreakDuration_Is5000()
        => Assert.Equal(5_000, CircuitBreakerDelegatingHandler.DefaultBreakDuration);

    [Fact]
    public void LowMinimumThroughput_Is2()
        => Assert.Equal(2, CircuitBreakerDelegatingHandler.LowMinimumThroughput);

    [Fact]
    public void DefaultMinimumThroughput_Is100()
        => Assert.Equal(100, CircuitBreakerDelegatingHandler.DefaultMinimumThroughput);

    [Fact]
    public void LowTimeout_Is10()
        => Assert.Equal(10, CircuitBreakerDelegatingHandler.LowTimeout);

    [Fact]
    public void DefaultTimeout_Is30000()
        => Assert.Equal(30_000, CircuitBreakerDelegatingHandler.DefaultTimeout);

    [Fact]
    public void HighTimeout_Is86400000()
        => Assert.Equal(86_400_000, CircuitBreakerDelegatingHandler.HighTimeout);

    // --------------------------------------------------------
    //  ServerErrorCodes
    // --------------------------------------------------------

    [Fact]
    public void DefaultServerErrorCodes_ContainsExpected9Codes()
    {
        var codes = CircuitBreakerDelegatingHandler.DefaultServerErrorCodes;
        Assert.Equal(9, codes.Count);
        Assert.Contains(HttpStatusCode.InternalServerError, codes);
        Assert.Contains(HttpStatusCode.NotImplemented, codes);
        Assert.Contains(HttpStatusCode.BadGateway, codes);
        Assert.Contains(HttpStatusCode.ServiceUnavailable, codes);
        Assert.Contains(HttpStatusCode.GatewayTimeout, codes);
        Assert.Contains(HttpStatusCode.HttpVersionNotSupported, codes);
        Assert.Contains(HttpStatusCode.VariantAlsoNegotiates, codes);
        Assert.Contains(HttpStatusCode.InsufficientStorage, codes);
        Assert.Contains(HttpStatusCode.LoopDetected, codes);
    }

    [Fact]
    public void DefaultServerErrorCodes_DoesNotContainSuccessCodes()
    {
        var codes = CircuitBreakerDelegatingHandler.DefaultServerErrorCodes;
        Assert.DoesNotContain(HttpStatusCode.OK, codes);
        Assert.DoesNotContain(HttpStatusCode.Created, codes);
        Assert.DoesNotContain(HttpStatusCode.Accepted, codes);
        Assert.DoesNotContain(HttpStatusCode.NotFound, codes);
    }

    // --------------------------------------------------------
    //  GetBreakDuration (tested via the internal CircuitBreaker property)
    // --------------------------------------------------------

    [Fact]
    public void GetBreakDuration_Null_UsesDefaultBreakDuration()
    {
        var opts = new QoSOptions(2, null); // BreakDuration is null
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(
            TimeSpan.FromMilliseconds(CircuitBreakerDelegatingHandler.DefaultBreakDuration),
            handler.CircuitBreaker.BreakDuration);
    }

    [Fact]
    public void GetBreakDuration_ExactLowBreakDuration_UsesDefaultBreakDuration()
    {
        // 500 == LowBreakDuration is invalid; must be strictly greater than 500
        var opts = new QoSOptions(2, CircuitBreakerDelegatingHandler.LowBreakDuration);
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(
            TimeSpan.FromMilliseconds(CircuitBreakerDelegatingHandler.DefaultBreakDuration),
            handler.CircuitBreaker.BreakDuration);
    }

    [Fact]
    public void GetBreakDuration_BelowLowBreakDuration_UsesDefaultBreakDuration()
    {
        var opts = new QoSOptions(2, CircuitBreakerDelegatingHandler.LowBreakDuration - 1); // 499
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(
            TimeSpan.FromMilliseconds(CircuitBreakerDelegatingHandler.DefaultBreakDuration),
            handler.CircuitBreaker.BreakDuration);
    }

    [Fact]
    public void GetBreakDuration_AboveLowBreakDuration_UsesConfiguredValue()
    {
        int customBreak = CircuitBreakerDelegatingHandler.LowBreakDuration + 1; // 501
        var opts = new QoSOptions(2, customBreak);
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(TimeSpan.FromMilliseconds(customBreak), handler.CircuitBreaker.BreakDuration);
    }

    // --------------------------------------------------------
    //  GetMinimumThroughput (tested via the internal CircuitBreaker property)
    // --------------------------------------------------------

    [Fact]
    public void GetMinimumThroughput_Null_UsesDefaultMinimumThroughput()
    {
        var opts = new QoSOptions(null, 1000); // MinimumThroughput is null
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(CircuitBreakerDelegatingHandler.DefaultMinimumThroughput, handler.CircuitBreaker.MinimumThroughput);
    }

    [Fact]
    public void GetMinimumThroughput_BelowLowMinimumThroughput_UsesDefaultMinimumThroughput()
    {
        // 1 < LowMinimumThroughput(2) is invalid
        var opts = new QoSOptions(CircuitBreakerDelegatingHandler.LowMinimumThroughput - 1, 1000); // 1
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(CircuitBreakerDelegatingHandler.DefaultMinimumThroughput, handler.CircuitBreaker.MinimumThroughput);
    }

    [Fact]
    public void GetMinimumThroughput_AtLowMinimumThroughput_UsesConfiguredValue()
    {
        int threshold = CircuitBreakerDelegatingHandler.LowMinimumThroughput; // 2
        var opts = new QoSOptions(threshold, 1000);
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(threshold, handler.CircuitBreaker.MinimumThroughput);
    }

    [Fact]
    public void GetMinimumThroughput_AboveLowMinimumThroughput_UsesConfiguredValue()
    {
        const int threshold = 5;
        var opts = new QoSOptions(threshold, 1000);
        var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        Assert.Equal(threshold, handler.CircuitBreaker.MinimumThroughput);
    }

    // --------------------------------------------------------
    //  Circuit state transitions via SendAsync
    // --------------------------------------------------------

    [Fact]
    public async Task SendAsync_SuccessResponse_RecordsSuccessAndReturnsResponse()
    {
        var opts = new QoSOptions(2, 1000);
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CircuitState.Closed, handler.CircuitBreaker.State);
        Assert.Equal(0, handler.CircuitBreaker.FailureCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task SendAsync_ServerErrorResponse_RecordsFailureAndReturnsResponse(HttpStatusCode statusCode)
    {
        var opts = new QoSOptions(10, 2000); // high threshold so circuit stays Closed after 1 failure
        using var handler = CreateHandler(opts, new FakeInnerHandler(statusCode));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(1, handler.CircuitBreaker.FailureCount);
        Assert.Equal(CircuitState.Closed, handler.CircuitBreaker.State);
    }

    [Fact]
    public async Task SendAsync_ReachesMinimumThroughput_OpensCircuit()
    {
        const int threshold = 2;
        var opts = new QoSOptions(threshold, 5000);
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.InternalServerError));
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test/");

        // Exhaust failure threshold
        for (int i = 0; i < threshold; i++)
        {
            request = new HttpRequestMessage(HttpMethod.Get, "http://test/");
            await SendAsync(handler, request, CancelMe);
        }

        Assert.Equal(CircuitState.Open, handler.CircuitBreaker.State);
    }

    [Fact]
    public async Task SendAsync_CircuitOpen_Returns503WithoutCallingInner()
    {
        const int threshold = 2;
        var opts = new QoSOptions(threshold, 5000);
        var innerHandler = new CountingInnerHandler(HttpStatusCode.InternalServerError);
        using var handler = CreateHandler(opts, innerHandler);

        // Open the circuit
        for (int i = 0; i < threshold; i++)
        {
            await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);
        }

        int callCountAfterOpen = innerHandler.CallCount;
        Assert.Equal(CircuitState.Open, handler.CircuitBreaker.State);

        // This call should be blocked
        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(callCountAfterOpen, innerHandler.CallCount); // inner was not called
    }

    [Fact]
    public async Task SendAsync_NonCanceledGeneralException_RecordsFailureAndRethrows()
    {
        var opts = new QoSOptions(2, 5000);
        using var handler = CreateHandler(opts, new ThrowingInnerHandler(new InvalidOperationException("test error")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe));

        Assert.Equal(1, handler.CircuitBreaker.FailureCount);
    }

    [Fact]
    public async Task SendAsync_OperationCanceledException_DoesNotRecordFailureAndRethrows()
    {
        var opts = new QoSOptions(2, 5000);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        using var handler = CreateHandler(opts, new ThrowingInnerHandler(new OperationCanceledException(cts.Token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), cts.Token));

        Assert.Equal(0, handler.CircuitBreaker.FailureCount);
    }

    // --------------------------------------------------------
    //  Timeout enforcement
    // --------------------------------------------------------

    [Fact]
    public async Task SendAsync_WithTimeout_CompletesBeforeTimeout_ReturnsResponse()
    {
        const int timeoutMs = 500, serviceDelayMs = 50;
        var opts = new QoSOptions(100, 5000) { Timeout = timeoutMs };
        using var handler = CreateHandler(opts, new DelayedInnerHandler(HttpStatusCode.OK, serviceDelayMs));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, handler.CircuitBreaker.FailureCount);
    }

    [Fact]
    public async Task SendAsync_WithTimeout_ExceedsTimeout_Returns503AndRecordsFailure()
    {
        const int timeoutMs = 100, serviceDelayMs = 500;
        var opts = new QoSOptions(100, 5000) { Timeout = timeoutMs };
        using var handler = CreateHandler(opts, new DelayedInnerHandler(HttpStatusCode.OK, serviceDelayMs));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, handler.CircuitBreaker.FailureCount);
    }

    [Fact]
    public async Task SendAsync_WithTimeout_OuterCancellationRequested_PropagatesCancellation()
    {
        const int timeoutMs = 5000, serviceDelayMs = 5000;
        var opts = new QoSOptions(100, 10000) { Timeout = timeoutMs };
        using var cts = new CancellationTokenSource(50); // outer cancel after 50ms
        using var handler = CreateHandler(opts, new DelayedInnerHandler(HttpStatusCode.OK, serviceDelayMs));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), cts.Token));
    }

    [Fact]
    public async Task SendAsync_NoTimeout_UsesNoTimeoutEnforcement()
    {
        // When Timeout is null, no per-request timeout is applied
        var opts = new QoSOptions(100, 5000) { Timeout = null };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.Created));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_TimeoutZero_TreatedAsNoTimeout()
    {
        // Timeout = 0 means no QoS timeout enforcement (UseQos would be false unless MinimumThroughput is set)
        var opts = new QoSOptions(2, 5000) { Timeout = 0 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.Accepted));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    // --------------------------------------------------------
    //  Ratio mode — constants
    // --------------------------------------------------------

    [Fact]
    public void LowFailureRatio_IsZero()
        => Assert.Equal(0.0, CircuitBreakerDelegatingHandler.LowFailureRatio);

    [Fact]
    public void DefaultFailureRatio_IsHalf()
        => Assert.Equal(0.5, CircuitBreakerDelegatingHandler.DefaultFailureRatio);

    [Fact]
    public void LowSamplingDuration_Is500()
        => Assert.Equal(500, CircuitBreakerDelegatingHandler.LowSamplingDuration);

    [Fact]
    public void DefaultSamplingDuration_Is10000()
        => Assert.Equal(10_000, CircuitBreakerDelegatingHandler.DefaultSamplingDuration);

    // --------------------------------------------------------
    //  Ratio mode — constructor selects correct CircuitBreaker mode
    // --------------------------------------------------------

    [Fact]
    public void Constructor_WithFailureRatio_CreatesRatioModeCircuitBreaker()
    {
        // FailureRatio is set → ratio mode
        var opts = new QoSOptions(5, 2000) { FailureRatio = 0.5, SamplingDuration = 10_000 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.NotNull(handler.CircuitBreaker.FailureRatio);
        Assert.Equal(0.5, handler.CircuitBreaker.FailureRatio);
        Assert.NotNull(handler.CircuitBreaker.SamplingDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(10_000), handler.CircuitBreaker.SamplingDuration);
    }

    [Fact]
    public void Constructor_WithoutFailureRatio_CreatesCountModeCircuitBreaker()
    {
        // FailureRatio is not set → count mode
        var opts = new QoSOptions(5, 2000);
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.Null(handler.CircuitBreaker.FailureRatio);
        Assert.Null(handler.CircuitBreaker.SamplingDuration);
    }

    [Fact]
    public void Constructor_WithZeroFailureRatio_CreatesCountModeCircuitBreaker()
    {
        // FailureRatio = 0 is treated as "not configured" → count mode
        var opts = new QoSOptions(5, 2000) { FailureRatio = 0.0 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.Null(handler.CircuitBreaker.FailureRatio);
    }

    // --------------------------------------------------------
    //  GetFailureRatio — clamps to DefaultFailureRatio when out of range
    // --------------------------------------------------------

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void GetFailureRatio_ValidValue_UsesConfiguredValue(double ratio)
    {
        var opts = new QoSOptions(5, 2000) { FailureRatio = ratio, SamplingDuration = 10_000 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.Equal(ratio, handler.CircuitBreaker.FailureRatio);
    }

    [Theory]
    [InlineData(1.1)]   // above 1 → default (still uses ratio mode with clamped value)
    [InlineData(2.0)]   // well above 1 → default
    public void GetFailureRatio_AboveOne_UsesDefaultFailureRatio(double ratio)
    {
        var opts = new QoSOptions(5, 2000) { FailureRatio = ratio, SamplingDuration = 10_000 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.Equal(CircuitBreakerDelegatingHandler.DefaultFailureRatio, handler.CircuitBreaker.FailureRatio);
    }

    [Theory]
    [InlineData(-0.1)]  // negative → count mode (treated as not configured)
    [InlineData(0.0)]   // zero → count mode (treated as not configured)
    public void GetFailureRatio_NonPositiveValue_FallsBackToCountMode(double ratio)
    {
        var opts = new QoSOptions(5, 2000) { FailureRatio = ratio, SamplingDuration = 10_000 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        // Non-positive FailureRatio → count mode → FailureRatio is null
        Assert.Null(handler.CircuitBreaker.FailureRatio);
    }

    // --------------------------------------------------------
    //  GetSamplingDuration — clamps to DefaultSamplingDuration when too low
    // --------------------------------------------------------

    [Fact]
    public void GetSamplingDuration_Null_UsesDefaultSamplingDuration()
    {
        var opts = new QoSOptions(5, 2000) { FailureRatio = 0.5, SamplingDuration = null };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.Equal(
            TimeSpan.FromMilliseconds(CircuitBreakerDelegatingHandler.DefaultSamplingDuration),
            handler.CircuitBreaker.SamplingDuration);
    }

    [Fact]
    public void GetSamplingDuration_ExactLowValue_UsesDefaultSamplingDuration()
    {
        // LowSamplingDuration is an exclusive lower bound
        var opts = new QoSOptions(5, 2000)
        {
            FailureRatio = 0.5,
            SamplingDuration = CircuitBreakerDelegatingHandler.LowSamplingDuration, // 500 — too low
        };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.Equal(
            TimeSpan.FromMilliseconds(CircuitBreakerDelegatingHandler.DefaultSamplingDuration),
            handler.CircuitBreaker.SamplingDuration);
    }

    [Fact]
    public void GetSamplingDuration_AboveLowValue_UsesConfiguredValue()
    {
        int custom = CircuitBreakerDelegatingHandler.LowSamplingDuration + 1; // 501
        var opts = new QoSOptions(5, 2000) { FailureRatio = 0.5, SamplingDuration = custom };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        Assert.Equal(TimeSpan.FromMilliseconds(custom), handler.CircuitBreaker.SamplingDuration);
    }

    // --------------------------------------------------------
    //  Ratio mode — SendAsync integration
    // --------------------------------------------------------

    [Fact]
    public async Task SendAsync_RatioMode_BelowMinimumThroughput_KeepsCircuitClosed()
    {
        // MinimumThroughput=4; only 3 requests → circuit stays closed regardless of ratio
        var opts = new QoSOptions(4, 5000) { FailureRatio = 0.5, SamplingDuration = 30_000 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.InternalServerError));

        for (int i = 0; i < 3; i++)
        {
            await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);
        }

        Assert.Equal(CircuitState.Closed, handler.CircuitBreaker.State);
    }

    [Fact]
    public async Task SendAsync_RatioMode_AtMinimumThroughput_RatioExceeded_OpensCircuit()
    {
        // MinimumThroughput=4, FailureRatio=0.5; 4 failures / 4 total = 100% → opens
        var opts = new QoSOptions(4, 5000) { FailureRatio = 0.5, SamplingDuration = 30_000 };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.InternalServerError));

        for (int i = 0; i < 4; i++)
        {
            await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);
        }

        Assert.Equal(CircuitState.Open, handler.CircuitBreaker.State);
    }

    [Fact]
    public async Task SendAsync_RatioMode_MixedResults_RatioBelowThreshold_KeepsCircuitClosed()
    {
        // MinimumThroughput=4, FailureRatio=0.5; 1 failure / 4 total = 25% → stays closed
        var opts = new QoSOptions(4, 5000) { FailureRatio = 0.5, SamplingDuration = 30_000 };
        using var handlerOk = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));
        using var handlerErr = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.InternalServerError));

        // Use two separate handlers to control which responses are returned
        // Simulate: 3 successes then 1 failure (ratio = 25%)
        var route = new DownstreamRouteBuilder().WithQosOptions(opts).Build();
        var innerHandler = new SequencedInnerHandler(
        [
            HttpStatusCode.OK,
            HttpStatusCode.OK,
            HttpStatusCode.OK,
            HttpStatusCode.InternalServerError,
        ]);
        using var handler = new CircuitBreakerDelegatingHandler(route, _loggerFactory.Object)
        {
            InnerHandler = innerHandler,
        };

        for (int i = 0; i < 4; i++)
        {
            await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);
        }

        Assert.Equal(CircuitState.Closed, handler.CircuitBreaker.State);
    }

    // --------------------------------------------------------
    //  GetTimeout (via SendAsync behaviour)
    // --------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SendAsync_DisabledTimeout_NoTimeoutEnforced(int? timeout)
    {
        var opts = new QoSOptions(100, 5000) { Timeout = timeout };
        // Use a delay longer than any timeout value to confirm no timeout fires
        using var handler = CreateHandler(opts, new DelayedInnerHandler(HttpStatusCode.OK, 50));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_TimeoutAtLowBoundary_UsesDefaultTimeout()
    {
        // Timeout = LowTimeout (10ms) is not strictly > 10, so GetTimeout returns DefaultTimeout (30s)
        // The request completes quickly so no timeout fires
        var opts = new QoSOptions(100, 5000) { Timeout = CircuitBreakerDelegatingHandler.LowTimeout };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_TimeoutAboveHigh_UsesDefaultTimeout()
    {
        // Timeout = HighTimeout is not strictly < HighTimeout, so GetTimeout returns DefaultTimeout (30s)
        var opts = new QoSOptions(100, 5000) { Timeout = CircuitBreakerDelegatingHandler.HighTimeout };
        using var handler = CreateHandler(opts, new FakeInnerHandler(HttpStatusCode.OK));

        var response = await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // --------------------------------------------------------
    //  Virtual ServerErrorCodes override
    // --------------------------------------------------------

    [Fact]
    public async Task SendAsync_CustomServerErrorCodes_TreatsNotFoundAsFailure()
    {
        // Subclass that also counts 404 as a failure
        var opts = new QoSOptions(2, 5000);
        var route = new DownstreamRouteBuilder().WithQosOptions(opts).Build();
        _loggerFactory.Setup(x => x.CreateLogger<CustomErrorCodesHandler>())
            .Returns(_logger.Object);
        using var handler = new CustomErrorCodesHandler(route, _loggerFactory.Object)
        {
            InnerHandler = new FakeInnerHandler(HttpStatusCode.NotFound),
        };

        // One call records a "failure" (404 is in the custom set)
        await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(1, handler.CircuitBreaker.FailureCount);
    }

    [Fact]
    public async Task SendAsync_CustomServerErrorCodes_DoesNotTreatInternalServerErrorAsFailure()
    {
        // Subclass that only counts 404 as a failure (not 500)
        var opts = new QoSOptions(2, 5000);
        var route = new DownstreamRouteBuilder().WithQosOptions(opts).Build();
        _loggerFactory.Setup(x => x.CreateLogger<CustomErrorCodesHandler>())
            .Returns(_logger.Object);
        using var handler = new CustomErrorCodesHandler(route, _loggerFactory.Object)
        {
            InnerHandler = new FakeInnerHandler(HttpStatusCode.InternalServerError),
        };

        // 500 is NOT in the custom set, so it is counted as a success
        await SendAsync(handler, new HttpRequestMessage(HttpMethod.Get, "http://test/"), CancelMe);

        Assert.Equal(0, handler.CircuitBreaker.FailureCount);
    }

    /// <summary>Test subclass that overrides <see cref="CircuitBreakerDelegatingHandler.ServerErrorCodes"/> to only treat 404 as a failure.</summary>
    private sealed class CustomErrorCodesHandler(DownstreamRoute route, IOcelotLoggerFactory loggerFactory)
        : CircuitBreakerDelegatingHandler(route, loggerFactory)
    {
        protected override HashSet<HttpStatusCode> ServerErrorCodes { get; } =
            [HttpStatusCode.NotFound];
    }

    // --------------------------------------------------------
    //  Helper inner handlers
    // --------------------------------------------------------

    private sealed class FakeInnerHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(statusCode));
    }

    private sealed class CountingInnerHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class DelayedInnerHandler(HttpStatusCode statusCode, int delayMs) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(delayMs, ct);
            return new HttpResponseMessage(statusCode);
        }
    }

    private sealed class ThrowingInnerHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class SequencedInnerHandler(IReadOnlyList<HttpStatusCode> sequence) : HttpMessageHandler
    {
        private int _index;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var code = _index < sequence.Count ? sequence[_index++] : sequence[^1];
            return Task.FromResult(new HttpResponseMessage(code));
        }
    }
}
