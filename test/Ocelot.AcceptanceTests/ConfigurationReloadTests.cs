using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.Middleware;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class ConfigurationReloadTests : IDisposable
    {
        private int _counter;
        private Steps _steps;

        public ConfigurationReloadTests()
        {
            _counter = 0;
            _steps = new Steps();
        }

        [Fact]
        public void should_reload_config_on_change()
        {
            var initialConfig = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "initialKey"
                }
            };

            var anotherConfig = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "someOtherKey"
                }
            };

            this.Given(x => _steps.GivenThereIsAConfiguration(initialConfig))
                .And(x => _steps.GivenOcelotIsRunningReloadingConfig(true))
                .And(x => _steps.GivenThereIsAConfiguration(anotherConfig))
                .And(x => _steps.GivenIWait(5000))
                .And(x => _steps.ThenConfigShouldBe(anotherConfig))
                .BDDfy();
        }

        private void ThenTheCounterIs(int expected)
        {
            _counter.ShouldBe(expected);
        }

        public void Dispose()
        {
            _steps.Dispose();
        }
    }
}
