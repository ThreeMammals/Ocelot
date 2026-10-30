using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

namespace Ocelot.Acceptance.Logging;

public sealed class DiagnosticListenerTests : AcceptanceSteps
{
    [Fact]
    [Trait("Feat", "2431")] // https://github.com/ThreeMammals/Ocelot/issues/2431
    public async Task Should_log_middleware_analysis_events()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = GivenConfiguration(GivenRoute(port));
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        await GivenOcelotIsRunningAsync(WithDiagnosticsLogging);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenMiddlewareAnalysisEventsAreLogged();
    }

    [Fact]
    [Trait("Feat", "2431")] // https://github.com/ThreeMammals/Ocelot/issues/2431
    public async Task Should_log_middleware_analysis_exception_event()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = GivenConfiguration(GivenRoute(port));
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        await GivenOcelotIsRunningAsync(WithBasicConfiguration, WithDiagnosticsLogging, WithFailingMiddleware);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.InternalServerError);
        ThenMiddlewareExceptionEventIsLogged();
    }

    private const string FailureMessage = "Diagnostics test failure";

    private static void WithFailingMiddleware(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (InvalidOperationException)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        });
        app.Use((HttpContext context, RequestDelegate next) => throw new InvalidOperationException(FailureMessage));
        WithUseOcelot(app);
    }

    private static void WithDiagnosticsLogging(IServiceCollection services) => services
        .AddOcelot()
        .Services.RemoveAll<IOcelotLoggerFactory>()
        .AddSingleton<IOcelotLoggerFactory, TestLoggerFactory<DiagnosticListenerTests>>();

    private void GivenThereIsAServiceRunningOn(int port)
        => handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

    private void ThenMiddlewareAnalysisEventsAreLogged()
    {
        var factory = OcelotServices.GetRequiredService<IOcelotLoggerFactory>();
        var logger = factory.ShouldBeOfType<TestLoggerFactory<DiagnosticListenerTests>>().Logger;
        logger.Messages.ShouldContain(m => m.Contains("MiddlewareStarting: ") && m.EndsWith("; /"), logger.Logbook);
        logger.Messages.ShouldContain(m => m.Contains("MiddlewareFinished: ") && m.EndsWith("; 200"), logger.Logbook);
    }

    private void ThenMiddlewareExceptionEventIsLogged()
    {
        var factory = OcelotServices.GetRequiredService<IOcelotLoggerFactory>();
        var logger = factory.ShouldBeOfType<TestLoggerFactory<DiagnosticListenerTests>>().Logger;
        logger.Messages.ShouldContain(m => m.Contains("MiddlewareException: ") && m.EndsWith($"; {FailureMessage};"), logger.Logbook);
    }
}
