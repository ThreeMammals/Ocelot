using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests
{
    [Collection(nameof(SequentialTests))]
    public sealed class ConfigurationReloadTests : IDisposable
    {
        private readonly FileConfiguration _initialConfig;
        private readonly FileConfiguration _anotherConfig;
        private readonly Steps _steps;

        public ConfigurationReloadTests()
        {
            _steps = new Steps();

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
        public void should_reload_config_on_change()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(_initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(true))
                .And(x => _steps.GivenThereIsAConfiguration(_anotherConfig))
                .And(x => _steps.ThenConfigShouldBeWithTimeout(_anotherConfig, 10000))
                .BDDfy();
        }

        [Fact]
        public void should_not_reload_config_on_change()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(_initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(false))
                .And(x => _steps.GivenThereIsAConfiguration(_anotherConfig))
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken))
                .And(x => _steps.ThenConfigShouldBe(_initialConfig))
                .BDDfy();
        }

        [Fact]
        public void should_trigger_change_token_on_change()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(_initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(true))
                .And(x => _steps.GivenIHaveAChangeToken())
                .And(x => _steps.GivenThereIsAConfiguration(_anotherConfig))
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken))
                .Then(x => _steps.TheChangeTokenShouldBeActive(true))
                .BDDfy();
        }

        [Fact]
        public void should_not_trigger_change_token_with_no_change()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(_initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(false))
                .And(x => _steps.GivenIHaveAChangeToken())
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken)) // Wait for prior activation to expire.
                .And(x => _steps.GivenThereIsAConfiguration(_anotherConfig))
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken))
                .Then(x => _steps.TheChangeTokenShouldBeActive(false))
                .BDDfy();
        }

        private const int MillisecondsToWaitForChangeToken = (int)(OcelotConfigurationChangeToken.PollingIntervalSeconds * 1000) - 100;

        public void Dispose()
        {
            _steps.Dispose();
        }
    }
}
