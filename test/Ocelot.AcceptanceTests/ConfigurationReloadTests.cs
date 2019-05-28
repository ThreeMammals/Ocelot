using Ocelot.Configuration.File;
using System;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class ConfigurationReloadTests : IDisposable
    {
        private FileConfiguration _initialConfig;
        private FileConfiguration _anotherConfig;
        private Steps _steps;

        public ConfigurationReloadTests()
        {
            _steps = new Steps();

            _initialConfig = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "initialKey"
                }
            };

            _anotherConfig = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "someOtherKey"
                }
            };
        }

        [Fact]
        public void should_reload_config_on_change()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(_initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(true))
                .And(x => _steps.GivenThereIsAConfiguration(_anotherConfig))
                .And(x => _steps.GivenIWait(2500))
                .And(x => _steps.ThenConfigShouldBe(_anotherConfig))
                .BDDfy();
        }

        [Fact]
        public void should_not_reload_config_on_change()
        {
            this.Given(x => _steps.GivenThereIsAConfiguration(_initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(false))
                .And(x => _steps.GivenThereIsAConfiguration(_anotherConfig))
                .And(x => _steps.GivenIWait(2500))
                .And(x => _steps.ThenConfigShouldBe(_initialConfig))
                .BDDfy();
        }

        public void Dispose()
        {
            _steps.Dispose();
        }
    }
}
