﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheManager.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DependencyInjection
{
    public class OcelotBuilderTests
    {
        private IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private IConfiguration _configRoot;
        private IOcelotBuilder _ocelotBuilder;
        private int _maxRetries;

        public OcelotBuilderTests()
        {
                IWebHostBuilder builder = new WebHostBuilder();
                _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
                _services = new ServiceCollection();
                _services.AddSingleton(builder);
                _services.AddSingleton<IHostingEnvironment, HostingEnvironment>();
                _services.AddSingleton<IConfiguration>(_configRoot);
                _maxRetries = 100;
        }
        private Exception _ex;

        [Fact]
        public void should_set_up_services()
        {
            this.When(x => WhenISetUpOcelotServices())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
        }

        [Fact]
        public void should_return_ocelot_builder()
        {
            this.When(x => WhenISetUpOcelotServices())
                .Then(x => ThenAnOcelotBuilderIsReturned())
                .BDDfy();
        }

       
        [Fact]
        public void should_set_up_cache_manager()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpCacheManager())
                .Then(x => ThenAnExceptionIsntThrown())
                .And(x => OnlyOneVersionOfEachCacheIsRegistered())
                .BDDfy();
        }

        [Fact]
        public void should_set_up_consul()
        {            
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpConsul())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
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

        [Fact]
        public void should_use_logger_factory()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenIValidateScopes())
                .When(x => WhenIAccessLoggerFactory())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
        }

        [Fact]
        public void should_set_up_without_passing_in_config()
        {
            this.When(x => WhenISetUpOcelotServicesWithoutConfig())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
        }

        private void ThenTheCorrectAdminPathIsRegitered()
        {
            _serviceProvider = _services.BuildServiceProvider();
            var path = _serviceProvider.GetService<IAdministrationPath>();
            path.Path.ShouldBe("/administration");
        }

        private void OnlyOneVersionOfEachCacheIsRegistered()
        {
            var outputCache = _services.Single(x => x.ServiceType == typeof(IOcelotCache<CachedResponse>));
            var outputCacheManager = _services.Single(x => x.ServiceType == typeof(ICacheManager<CachedResponse>));
            var thing = (CacheManager.Core.ICacheManager<CachedResponse>)outputCacheManager.ImplementationInstance;
            thing.Configuration.MaxRetries.ShouldBe(_maxRetries);
            
            var ocelotConfigCache = _services.Single(x => x.ServiceType == typeof(IOcelotCache<IOcelotConfiguration>));
            var ocelotConfigCacheManager = _services.Single(x => x.ServiceType == typeof(ICacheManager<IOcelotConfiguration>));

            var fileConfigCache = _services.Single(x => x.ServiceType == typeof(IOcelotCache<FileConfiguration>));
            var fileConfigCacheManager = _services.Single(x => x.ServiceType == typeof(ICacheManager<FileConfiguration>));
        }

        private void WhenISetUpConsul()
        {
            try
            {
                _ocelotBuilder.AddStoreOcelotConfigurationInConsul();
            }
            catch (Exception e)
            {
                _ex = e;
            }       
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

        private void ThenAnOcelotBuilderIsReturned()
        {
            _ocelotBuilder.ShouldBeOfType<OcelotBuilder>();
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

        private void WhenISetUpOcelotServicesWithoutConfig()
        {
            try
            {
                _ocelotBuilder = _services.AddOcelot();
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void WhenISetUpCacheManager()
        {
            try
            {
                _ocelotBuilder.AddCacheManager(x => {
                    x.WithMaxRetries(_maxRetries);
                    x.WithDictionaryHandle();
                });
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void WhenIAccessLoggerFactory()
        {
            try
            {
                var logger = _serviceProvider.GetService<IFileConfigurationSetter>();
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void WhenIValidateScopes()
        {
            try
            {
                _serviceProvider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
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
