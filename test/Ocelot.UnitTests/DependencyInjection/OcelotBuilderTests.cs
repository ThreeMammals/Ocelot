using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using CacheManager.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DependencyInjection
{
    public class OcelotBuilderTests
    {
        private IServiceCollection _services;
        private IConfigurationRoot _configRoot;
        private IOcelotBuilder _ocelotBuilder;
        private int _maxRetries;

        public OcelotBuilderTests()
        {
                IWebHostBuilder builder = new WebHostBuilder();
                _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
                _services = new ServiceCollection();
                _services.AddSingleton(builder);
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

        private void ThenAnExceptionIsntThrown()
        {
            _ex.ShouldBeNull();
        }
    }
}
