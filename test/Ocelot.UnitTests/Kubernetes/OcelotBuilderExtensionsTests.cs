using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes;
using Shouldly;
using System;
using System.Collections.Generic;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Kubernetes
{
    public class OcelotBuilderExtensionsTests
    {
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private readonly IConfiguration _configRoot;
        private IOcelotBuilder _ocelotBuilder;
        private Exception _ex;

        public OcelotBuilderExtensionsTests()
        {
            _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
            _services = new ServiceCollection();
            _services.AddSingleton<IHostingEnvironment, HostingEnvironment>();
            _services.AddSingleton(_configRoot);
        }

        [Fact]
        public void should_set_up_kubernetes()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpKubernetes())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
        }

        private void WhenISetUpOcelotServices()
        {
            try
            {
                _ocelotBuilder = _services.AddOcelot(_configRoot);
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void WhenISetUpKubernetes()
        {
            try
            {
                _ocelotBuilder.AddKubernetes();
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
