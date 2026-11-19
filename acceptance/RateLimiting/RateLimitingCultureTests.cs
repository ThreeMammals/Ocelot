using Ocelot.Configuration.File;
using Ocelot.Testing.Steps;
using System.Globalization;

namespace Ocelot.Acceptance.RateLimiting;

/// <summary>
/// The time spans of a rate limiting rule must not depend on the culture of the server.
/// These tests change the default culture of the process, so they must not run in parallel with other tests.
/// </summary>
[Collection(nameof(SequentialTests))]
public sealed class RateLimitingCultureTests : RateLimitingSteps
{
    const int TooManyRequests = (int)HttpStatusCode.TooManyRequests;

    [Theory]
    [Trait("Bug", "2440")] // https://github.com/ThreeMammals/Ocelot/issues/2440
    [InlineData("tr-TR", "0.5s", null)] // '.' is the group separator, so "0.5" was read as 5
    [InlineData("de-DE", "0.5s", null)]
    [InlineData("fr-FR", "0.5s", null)] // "0.5" was not a number at all
    [InlineData("tr-TR", "1s", 0.5D)] // the legacy PeriodTimespan was formatted as "0,5s" before being parsed
    public async Task Should_apply_fractional_time_spans_whatever_the_culture_is(string culture, string period, double? periodTimespan)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, period, periodTimespan); // 1/0.5s/w0ms or 1/1s/w0.5s
        var configuration = GivenConfiguration(route);

        var currentCulture = CultureInfo.CurrentCulture;
        var defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(culture);

            GivenThereIsAServiceRunningOnPath(port, "/api/ClientRateLimit");
            GivenThereIsAConfiguration(configuration);
            GivenOcelotIsRunning();

            await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
            ThenTheStatusCodeShouldBeOk();

            await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
            ThenTheStatusCodeShouldBe(TooManyRequests);

            await GivenIWaitAsync(1300); // both the period and the wait window have elapsed, unless they were read 10 times longer
            await WhenIGetUrlOnTheApiGatewayMultipleTimes("/ClientRateLimit", 1);
            ThenTheStatusCodeShouldBeOk();
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
        }
    }

    private FileRoute GivenRoute(int port, string period, double? periodTimespan)
    {
        var route = base.GivenRoute(port, "/ClientRateLimit", "/api/ClientRateLimit");
        route.RateLimitOptions = new()
        {
            Limit = 1,
            Period = period,
            PeriodTimespan = periodTimespan,
        };
        return route;
    }

    public override FileConfiguration GivenConfiguration(params FileRoute[] routes)
    {
        var config = base.GivenConfiguration(routes);
        config.GlobalConfiguration.RateLimitOptions = new()
        {
            ClientIdHeader = "ClientId",
            HttpStatusCode = TooManyRequests,
        };
        return config;
    }
}
