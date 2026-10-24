using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.Acceptance;

public class Steps : AcceptanceSteps
{
    protected static readonly string NL = Environment.NewLine;

    public Steps() : base()
    {
    }

    public override CancellationToken CancelMe => Xunit.TestContext.Current.CancellationToken;

    public void GivenOcelotIsRunningWithDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
        => GivenOcelotIsRunning(s => s.AddOcelot().AddDelegatingHandler<THandler>(global));

    public Task<int> GivenOcelotIsRunningAsync(OcelotPipelineConfiguration pipelineConfig)
        => GivenOcelotIsRunningAsync(WithBasicConfiguration, WithAddOcelot,
            async a => await a.UseOcelot(pipelineConfig));

    #region TODO: Move to Ocelot.Testing package
    public static void NoConfiguration(WebHostBuilderContext _, IConfigurationBuilder __) { }
    public static void NoServices(IServiceCollection _) { }
    public static void NoLogging(WebHostBuilderContext _, ILoggingBuilder __) { }
    public static void NoApplications(IApplicationBuilder _) { }
    #endregion
}
