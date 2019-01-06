﻿namespace Ocelot.UnitTests.Rafty
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Administration;
    using Ocelot.DependencyInjection;
    using Provider.Rafty;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class OcelotAdministrationBuilderExtensionsTests
    {
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private readonly IConfiguration _configRoot;
        private IOcelotBuilder _ocelotBuilder;
        private Exception _ex;

        public OcelotAdministrationBuilderExtensionsTests()
        {
            _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
            _services = new ServiceCollection();
            _services.AddSingleton<IHostingEnvironment, HostingEnvironment>();
            _services.AddSingleton(_configRoot);
        }

        [Fact]
        public void should_set_up_rafty()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpRafty())
                .Then(x => ThenAnExceptionIsntThrown())
                .Then(x => ThenTheCorrectAdminPathIsRegitered())
                .BDDfy();
        }

        private void WhenISetUpRafty()
        {
            try
            {
                _ocelotBuilder.AddAdministration("/administration", "secret").AddRafty();
            }
            catch (Exception e)
            {
                _ex = e;
            }
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

        private void ThenAnExceptionIsntThrown()
        {
            _ex.ShouldBeNull();
        }

        private void ThenTheCorrectAdminPathIsRegitered()
        {
            _serviceProvider = _services.BuildServiceProvider();
            var path = _serviceProvider.GetService<IAdministrationPath>();
            path.Path.ShouldBe("/administration");
        }
    }
}
