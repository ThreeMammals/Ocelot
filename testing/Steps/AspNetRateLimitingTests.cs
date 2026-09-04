using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using System.Threading.RateLimiting;

namespace Ocelot.AcceptanceTests.RateLimiting;

public class AspNetRateLimitingTests: RateLimitingSteps
{
    private const string FixedWindowPolicyName = "RateLimitPolicy";
    private int _rateLimitLimit;
    private int _rateLimitWindow;
    private string _quotaExceededMessage;
    
    [Fact]
    [Trait("Feat", "2138")]
    public async Task Should_RateLimit()
    {
        _rateLimitLimit = 3;
        _rateLimitWindow = 1;
        _quotaExceededMessage = "woah!";
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, FixedWindowPolicyName);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOnPath(port, "/");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithRateLimiter);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 1);
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/", _rateLimitLimit - 1);
        ThenTheStatusCodeShouldBeOK();
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 1);
        ThenTheStatusCodeShouldBe(HttpStatusCode.TooManyRequests);
        ThenTheResponseBodyShouldBe(_quotaExceededMessage);
        GivenIWait((1000 * _rateLimitWindow) + 100);
        await WhenIGetUrlOnTheApiGatewayMultipleTimes("/", 1);
        ThenTheStatusCodeShouldBeOK();
    }

    private FileRoute GivenRoute(int port, string rateLimitPolicyName)
    {
        var route = GivenRoute(port);
        route.RateLimitOptions = new()
        {
            Policy = rateLimitPolicyName,
        };
        return route;
    }

    private FileConfiguration GivenConfiguration(params FileRoute[] routes)
    {
        var config = base.GivenConfiguration(routes);
        config.GlobalConfiguration.RateLimitOptions = new()
        {
            QuotaExceededMessage = _quotaExceededMessage,
            HttpStatusCode = StatusCodes.Status429TooManyRequests,
        };
        return config;
    }

    private void WithRateLimiter(IServiceCollection services) => services
        .AddOcelot().Services
        .AddRateLimiter(op =>
        {
            op.AddFixedWindowLimiter(FixedWindowPolicyName, options =>
            {
                options.PermitLimit = _rateLimitLimit;
                options.Window = TimeSpan.FromSeconds(_rateLimitWindow);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });
        });
}
