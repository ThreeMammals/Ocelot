namespace Ocelot.UnitTests.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Configuration.Setter;
    using Ocelot.DependencyInjection;
    using Ocelot.Requester;
    using Shouldly;
    using IdentityServer4.AccessTokenValidation;
    using TestStack.BDDfy;
    using Xunit;
    using Ocelot.Middleware.Multiplexer;
    using Ocelot.Administration;

    public class OcelotBuilderTests
    {
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private readonly IConfiguration _configRoot;
        private IOcelotBuilder _ocelotBuilder;
        private Exception _ex;

        public OcelotBuilderTests()
        {
            _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
            _services = new ServiceCollection();
            _services.AddSingleton<IHostingEnvironment, HostingEnvironment>();
            _services.AddSingleton(_configRoot);
        }

        //keep
        [Fact]
        public void should_set_up_administration_with_identity_server_options()
        {
            Action<IdentityServerAuthenticationOptions> options = o => {};

            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpAdministration(options))
                .Then(x => ThenAnExceptionIsntThrown())
                .Then(x => ThenTheCorrectAdminPathIsRegitered())
                .BDDfy();
        }

        //keep
        [Fact]
        public void should_set_up_administration()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpAdministration())
                .Then(x => ThenAnExceptionIsntThrown())
                .Then(x => ThenTheCorrectAdminPathIsRegitered())
                .BDDfy();
        }

        private void WhenISetUpAdministration()
        {
            _ocelotBuilder.AddAdministration("/administration", "secret");
        }

        private void WhenISetUpAdministration(Action<IdentityServerAuthenticationOptions> options)
        {
            _ocelotBuilder.AddAdministration("/administration", options);
        }

        private void ThenTheCorrectAdminPathIsRegitered()
        {
            _serviceProvider = _services.BuildServiceProvider();
            var path = _serviceProvider.GetService<IAdministrationPath>();
            path.Path.ShouldBe("/administration");
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
    }
}

