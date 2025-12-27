using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests.Configuration;

public sealed class ConfigurationReloadTests : Steps
{
    private readonly FileConfiguration _initialConfig;
    private readonly FileConfiguration _anotherConfig;
    private IOcelotConfigurationChangeTokenSource _changeToken;

    public ConfigurationReloadTests()
    {
        _initialConfig = new FileConfiguration
        {
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RequestIdKey = "initialKey",
            },
        };
        _anotherConfig = new FileConfiguration
        {
            GlobalConfiguration = new FileGlobalConfiguration
            {
                RequestIdKey = "someOtherKey",
            },
        };
    }

    [Fact]
    public void Should_reload_config_on_change()
    {
        this.Given(x => GivenThereIsAConfiguration(_initialConfig))
            .And(x => GivenOcelotIsRunningReloadingConfig(true))
            .And(x => GivenThereIsAConfiguration(_anotherConfig))
            .And(x => ThenConfigShouldBeWithTimeout(_anotherConfig, 10000))
            .BDDfy();
    }

    private async Task ThenConfigShouldBeWithTimeout(FileConfiguration fileConfig, int timeoutMs)
    {
        var result = await Wait.For(timeoutMs).UntilAsync(async () =>
        {
            var internalConfigCreator = ocelotServer.Host.Services.GetService<IInternalConfigurationCreator>();
            var internalConfigRepo = ocelotServer.Host.Services.GetService<IInternalConfigurationRepository>();
            var internalConfig = internalConfigRepo.Get();
            var config = await internalConfigCreator.Create(fileConfig);
            return internalConfig.Data.RequestId == config.Data.RequestId;
        });
        result.ShouldBe(true);
    }

    [Fact]
    public void Should_not_reload_config_on_change()
    {
        this.Given(x => GivenThereIsAConfiguration(_initialConfig))
            .And(x => GivenOcelotIsRunningReloadingConfig(false))
            .And(x => GivenThereIsAConfiguration(_anotherConfig))
            .And(x => Steps.GivenIWait(MillisecondsToWaitForChangeToken))
            .And(x => ThenConfigShouldBe(_initialConfig))
            .BDDfy();
    }

    private async Task ThenConfigShouldBe(FileConfiguration fileConfig)
    {
        var internalConfigCreator = ocelotServer.Host.Services.GetService<IInternalConfigurationCreator>();
        var internalConfigRepo = ocelotServer.Host.Services.GetService<IInternalConfigurationRepository>();
        var internalConfig = internalConfigRepo.Get();
        var config = await internalConfigCreator.Create(fileConfig);
        internalConfig.Data.RequestId.ShouldBe(config.Data.RequestId);
    }

    [Fact]
    public void Should_trigger_change_token_on_change()
    {
        this.Given(x => GivenThereIsAConfiguration(_initialConfig))
            .And(x => GivenOcelotIsRunningReloadingConfig(true))
            .And(x => GivenIHaveAChangeToken())
            .And(x => GivenThereIsAConfiguration(_anotherConfig))
            .And(x => Steps.GivenIWait(MillisecondsToWaitForChangeToken))
            .Then(x => TheChangeTokenShouldBeActive(true))
            .BDDfy();
    }

    [Fact]
    public void Should_not_trigger_change_token_with_no_change()
    {
        this.Given(x => GivenThereIsAConfiguration(_initialConfig))
            .And(x => GivenOcelotIsRunningReloadingConfig(false))
            .And(x => GivenIHaveAChangeToken())
            .And(x => Steps.GivenIWait(MillisecondsToWaitForChangeToken)) // Wait for prior activation to expire.
            .And(x => GivenThereIsAConfiguration(_anotherConfig))
            .And(x => Steps.GivenIWait(MillisecondsToWaitForChangeToken))
            .Then(x => TheChangeTokenShouldBeActive(false))
            .BDDfy();
    }

    private const int MillisecondsToWaitForChangeToken = (int)(OcelotConfigurationChangeToken.PollingIntervalSeconds * 1000) - 100;

    private void GivenOcelotIsRunningReloadingConfig(bool shouldReload)
    {
        GivenOcelotIsRunning((context, config) => config
            .SetBasePath(context.HostingEnvironment.ContentRootPath)
            .AddOcelot(ocelotConfigFileName, false, shouldReload));
    }

    private void GivenIHaveAChangeToken()
    {
        _changeToken = ocelotServer.Host.Services.GetRequiredService<IOcelotConfigurationChangeTokenSource>();
    }

    private void TheChangeTokenShouldBeActive(bool itShouldBeActive)
    {
        _changeToken.ChangeToken.HasChanged.ShouldBe(itShouldBeActive);
    }
}
