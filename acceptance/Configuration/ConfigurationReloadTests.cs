using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;

namespace Ocelot.Acceptance.Configuration;

public sealed class ConfigurationReloadTests : Steps
{
    private readonly FileConfiguration _initialConfig;
    private readonly FileConfiguration _anotherConfig;
    private IOcelotConfigurationChangeTokenSource _changeToken;

    public ConfigurationReloadTests()
    {
        _initialConfig = GivenConfiguration();
        _initialConfig.GlobalConfiguration.RequestIdKey = "initialKey";
        _anotherConfig = GivenConfiguration();
        _anotherConfig.GlobalConfiguration.RequestIdKey = "someOtherKey";
    }

    [Fact]
    public async Task Should_reload_config_on_change()
    {
        GivenThereIsAConfiguration(_initialConfig);
        GivenOcelotIsRunningReloadingConfig(true);
        GivenThereIsAConfiguration(_anotherConfig);
        await ThenConfigShouldBeWithTimeout(_anotherConfig, 10000);
    }

    private async Task ThenConfigShouldBeWithTimeout(FileConfiguration fileConfig, int timeoutMs)
    {
        var result = await Wait.For(timeoutMs).UntilAsync(async (ct) =>
        {
            var internalConfigCreator = OcelotServices.GetService<IInternalConfigurationCreator>();
            var internalConfigRepo = OcelotServices.GetService<IInternalConfigurationRepository>();
            var internalConfig = internalConfigRepo.Get();
            var config = await internalConfigCreator.Create(fileConfig);
            return internalConfig.RequestId == config.Data.RequestId;
        }, CancelMe);
        result.ShouldBe(true);
    }

    [Fact]
    public async Task Should_not_reload_config_on_change()
    {
        GivenThereIsAConfiguration(_initialConfig);
        GivenOcelotIsRunningReloadingConfig(false);
        GivenThereIsAConfiguration(_anotherConfig);
        GivenIWait(MillisecondsToWaitForChangeToken);
        await ThenConfigShouldBe(_initialConfig);
    }

    private async Task ThenConfigShouldBe(FileConfiguration fileConfig)
    {
        var internalConfigCreator = OcelotServices.GetService<IInternalConfigurationCreator>();
        var internalConfigRepo = OcelotServices.GetService<IInternalConfigurationRepository>();
        var internalConfig = internalConfigRepo.Get();
        var config = await internalConfigCreator.Create(fileConfig);
        internalConfig.RequestId.ShouldBe(config.Data.RequestId);
    }

    [Fact]
    public void Should_trigger_change_token_on_change()
    {
        GivenThereIsAConfiguration(_initialConfig);
        GivenOcelotIsRunningReloadingConfig(true);
        GivenIHaveAChangeToken();
        GivenThereIsAConfiguration(_anotherConfig);
        GivenIWait(MillisecondsToWaitForChangeToken);
        TheChangeTokenShouldBeActive(true);
    }

    [Fact]
    public void Should_not_trigger_change_token_with_no_change()
    {
        GivenThereIsAConfiguration(_initialConfig);
        GivenOcelotIsRunningReloadingConfig(false);
        GivenIHaveAChangeToken();
        GivenIWait(MillisecondsToWaitForChangeToken); // Wait for prior activation to expire.
        GivenThereIsAConfiguration(_anotherConfig);
        GivenIWait(MillisecondsToWaitForChangeToken);
        TheChangeTokenShouldBeActive(false);
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
        _changeToken = OcelotServices.GetRequiredService<IOcelotConfigurationChangeTokenSource>();
    }

    private void TheChangeTokenShouldBeActive(bool itShouldBeActive)
    {
        _changeToken.ChangeToken.HasChanged.ShouldBe(itShouldBeActive);
    }
}
