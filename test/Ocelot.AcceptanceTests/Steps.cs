using Ocelot.AcceptanceTests.Properties;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.AcceptanceTests;

public class Steps : AcceptanceSteps
{
    public Steps() : base()
    {
        BddfyConfig.Configure();
    }
    public static bool IsCiCd() => IsRunningInGitHubActions();
    public static bool IsRunningInGitHubActions()
        => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    public void GivenOcelotIsRunningWithDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
        => GivenOcelotIsRunning(s => s.AddOcelot().AddDelegatingHandler<THandler>(global));

    public Task<int> GivenOcelotIsRunningAsync(OcelotPipelineConfiguration pipelineConfig)
        => GivenOcelotIsRunningAsync(WithBasicConfiguration, WithAddOcelot,
            async a => await a.UseOcelot(pipelineConfig));

    #region TODO: Move to Ocelot.Testing package
    #endregion
}
