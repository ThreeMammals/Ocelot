using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.AcceptanceTests.Properties;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Ocelot.AcceptanceTests;

public class Steps : AcceptanceSteps
{
    public Steps() : base()
    {
        BddfyConfig.Configure();
    }

    protected FileConfiguration GivenConfiguration(FileGlobalConfiguration globalConfig, params FileRoute[] routes)
    {
        var config = GivenConfiguration(routes);
        config.GlobalConfiguration = globalConfig;
        return config;
    }

    public void GivenOcelotIsRunningWithDelegatingHandler<THandler>(bool global = false)
        where THandler : DelegatingHandler
    {
        GivenOcelotIsRunning(s => s.AddOcelot().AddDelegatingHandler<THandler>(global));
    }

    public void GivenOcelotIsRunning(OcelotPipelineConfiguration pipelineConfig)
    {
        var builder = TestHostBuilder.Create()
            .ConfigureAppConfiguration(WithBasicConfiguration)
            .ConfigureServices(WithAddOcelot)
            .Configure(async a => await a.UseOcelot(pipelineConfig));
        ocelotServer = new TestServer(builder);
        ocelotClient = ocelotServer.CreateClient();
    }
}
