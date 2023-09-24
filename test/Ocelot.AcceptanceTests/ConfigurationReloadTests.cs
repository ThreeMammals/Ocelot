using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.File;
using System;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class ConfigurationReloadTests : IDisposable
    {
        private readonly Steps _steps = new();

        /// <summary>
        /// Configs factory to avoid side effects of changing the same config.
        /// xUnit assumes that tests are isolated and side-effect-free, meaning they don't interfere with one another,
        /// so it permits parallel execution to speed up test runs.
        /// </summary>
        /// <param name="id">The current config id.</param>
        /// <returns>A tuple containing the initial config and the other config.</returns>
        private static (FileConfiguration InitialConfig, FileConfiguration AnotherConfig) GetConfigs(int id)
        {
            return (new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = $"initialKey-{id}",
                },
            }, new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = $"someOtherKey-{id}",
                },
            });
        }

        [Fact]
        public void should_reload_config_on_change()
        {
            var (initialConfig, anotherConfig) = GetConfigs(1);
            this.Given(x => _steps.GivenThereIsAConfiguration(initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(true))
                .And(x => _steps.GivenThereIsAConfiguration(anotherConfig))
                .And(x => _steps.GivenIWait(5000))
                .And(x => _steps.ThenConfigShouldBe(anotherConfig))
                .BDDfy();
        }

        [Fact]
        public void should_not_reload_config_on_change()
        {
            var (initialConfig, anotherConfig) = GetConfigs(2);
            this.Given(x => _steps.GivenThereIsAConfiguration(initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(false))
                .And(x => _steps.GivenThereIsAConfiguration(anotherConfig))
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken))
                .And(x => _steps.ThenConfigShouldBe(initialConfig))
                .BDDfy();
        }

        [Fact]
        public void should_trigger_change_token_on_change()
        {
            var (initialConfig, anotherConfig) = GetConfigs(3);
            this.Given(x => _steps.GivenThereIsAConfiguration(initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(true))
                .And(x => _steps.GivenIHaveAChangeToken())
                .And(x => _steps.GivenThereIsAConfiguration(anotherConfig))
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken))
                .Then(x => _steps.TheChangeTokenShouldBeActive(true))
                .BDDfy();
        }

        [Fact]
        public void should_not_trigger_change_token_with_no_change()
        {
            var (initialConfig, anotherConfig) = GetConfigs(4);
            this.Given(x => _steps.GivenThereIsAConfiguration(initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(false))
                .And(x => _steps.GivenIHaveAChangeToken())
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken)) // Wait for prior activation to expire.
                .And(x => _steps.GivenThereIsAConfiguration(anotherConfig))
                .And(x => _steps.GivenIWait(MillisecondsToWaitForChangeToken))
                .Then(x => _steps.TheChangeTokenShouldBeActive(false))
                .BDDfy();
        }

        private const int MillisecondsToWaitForChangeToken = (int)(OcelotConfigurationChangeToken.PollingIntervalSeconds * 1000) - 100;

        public void Dispose()
        {
            _steps.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
