using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Logging;

namespace Ocelot.Acceptance.Logging;

public sealed class DiagnosticListenerTests : Steps
{
    [Fact]
    [Trait("Feat", "2431")] // https://github.com/ThreeMammals/Ocelot/issues/2431
    public void Should_log_middleware_analysis_events()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = GivenConfiguration(GivenRoute(port));
        this
            .Given(x => GivenThereIsAServiceRunningOn(port))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithTestLogger))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenMiddlewareAnalysisEventsAreLogged())
            .BDDfy();
    }

    private static void WithTestLogger(IServiceCollection services) => services
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
}
