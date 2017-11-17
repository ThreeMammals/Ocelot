using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DependencyInjection
{
    public class ServiceCollectionExtensionTests
    {
        private Exception _ex;

        [Fact]
        public void should_set_up_services()
        {
            this.When(x => WhenISetUpOcelotServices())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
        }

        private void WhenISetUpOcelotServices()
        {
            try
            {
                IWebHostBuilder builder = new WebHostBuilder();
                IConfigurationRoot configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
                IServiceCollection services = new ServiceCollection();
                services.AddSingleton(builder);
                services.AddOcelot(configRoot);
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void ThenAnExceptionIsntThrown()
        {
            _ex.ShouldBeNull();
        }
    }
}
