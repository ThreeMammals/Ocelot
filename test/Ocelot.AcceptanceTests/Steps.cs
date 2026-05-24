using Ocelot.AcceptanceTests.Properties;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using TestStack.BDDfy.Configuration;

namespace Ocelot.AcceptanceTests;

public class Steps : AcceptanceSteps
{
    public Steps() : base()
    {
        BddfyConfig.Configure();
        Configurator.Processors.ConsoleReport.Disable();
    }

    /*
    protected readonly ITestOutputHelper _output;
    public Steps(ITestOutputHelper output) : base()
    {
        _output = output;
        BddfyConfig.Configure();

        // Important: Redirect BDDfy console output
        // Configurator.Processors.ConsoleReport.Use(_output);   // Try this first
    }
    protected void Report(string message) => _output.WriteLine(message);
    */

    public override CancellationToken CancelMe => Xunit.TestContext.Current.CancellationToken;

    public IFluentStepBuilder<TScenario> Scenario<TScenario>() where TScenario : class
        => new FluentStepBuilder<TScenario>(this as TScenario);
    public static IFluentStepBuilder<TScenario> Scenario<TScenario>(TScenario scenario) where TScenario : class
        => new FluentStepBuilder<TScenario>(scenario);

    /// <summary>
    /// Runs the BDDfy scenario. Call this at the end of each test method.
    /// </summary>
    protected void RunBddfy(string title = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            this.BDDfy();
        else
            this.BDDfy(title);
    }

    public void GivenOcelotIsRunningWithDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
        => GivenOcelotIsRunning(s => s.AddOcelot().AddDelegatingHandler<THandler>(global));

    public Task<int> GivenOcelotIsRunningAsync(OcelotPipelineConfiguration pipelineConfig)
        => GivenOcelotIsRunningAsync(WithBasicConfiguration, WithAddOcelot,
            async a => await a.UseOcelot(pipelineConfig));

    #region TODO: Move to Ocelot.Testing package
    #endregion
}
