using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using Ocelot.Requester;
using Ocelot.UnitTests.Requester;
using Shouldly;
using IdentityServer4.AccessTokenValidation;
using TestStack.BDDfy;
using Xunit;
using static Ocelot.UnitTests.Middleware.UserDefinedResponseAggregatorTests;
using Ocelot.Middleware.Multiplexer;

namespace Ocelot.UnitTests.DependencyInjection
{
    public class OcelotBuilderTests
    {
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private readonly IConfiguration _configRoot;
        private IOcelotBuilder _ocelotBuilder;
        private readonly int _maxRetries;
        private Exception _ex;

        public OcelotBuilderTests()
        {
            _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
            _services = new ServiceCollection();
            _services.AddSingleton<IHostingEnvironment, HostingEnvironment>();
            _services.AddSingleton(_configRoot);
            _maxRetries = 100;
        }

        [Fact]
        public void should_add_specific_delegating_handlers_transient()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => AddSpecificTransientDelegatingHandler<FakeDelegatingHandler>())
                .And(x => AddSpecificTransientDelegatingHandler<FakeDelegatingHandlerTwo>())
                .Then(x => ThenTheProviderIsRegisteredAndReturnsSpecificHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .And(x => ThenTheSpecificHandlersAreTransient())
                .BDDfy();
        }

        [Fact]
        public void should_add_specific_delegating_handler_singleton()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => AddSpecificDelegatingHandler<FakeDelegatingHandler>())
                .And(x => AddSpecificDelegatingHandler<FakeDelegatingHandlerTwo>())
                .Then(x => ThenTheProviderIsRegisteredAndReturnsSpecificHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .And(x => ThenTheSpecificHandlersAreSingleton())
                .BDDfy();
        }

        [Fact]
        public void should_add_global_delegating_handlers_transient()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => AddTransientGlobalDelegatingHandler<FakeDelegatingHandler>())
                .And(x => AddTransientGlobalDelegatingHandler<FakeDelegatingHandlerTwo>())
                .Then(x => ThenTheProviderIsRegisteredAndReturnsHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .And(x => ThenTheGlobalHandlersAreTransient())
                .BDDfy();
        }

        [Fact]
        public void should_add_global_delegating_handlers_singleton()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => AddGlobalDelegatingHandler<FakeDelegatingHandler>())
                .And(x => AddGlobalDelegatingHandler<FakeDelegatingHandlerTwo>())
                .Then(x => ThenTheProviderIsRegisteredAndReturnsHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .And(x => ThenTheGlobalHandlersAreSingleton())
                .BDDfy();
        }

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
        public void should_set_up_administration_with_identity_server_options()
        {
            Action<IdentityServerAuthenticationOptions> options = o => {};

            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpAdministration(options))
                .Then(x => ThenAnExceptionIsntThrown())
                .Then(x => ThenTheCorrectAdminPathIsRegitered())
                .BDDfy();
        }

        [Fact]
        public void should_set_up_administration()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpAdministration())
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
        public void should_set_up_tracing()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenISetUpOpentracing())
                .When(x => WhenIAccessOcelotHttpTracingHandler())
                .BDDfy();
        }

        [Fact]
        public void should_set_up_without_passing_in_config()
        {
            this.When(x => WhenISetUpOcelotServicesWithoutConfig())
                .Then(x => ThenAnExceptionIsntThrown())
                .BDDfy();
        }

        [Fact]
        public void should_add_singleton_defined_aggregators()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => AddSingletonDefinedAggregator<TestDefinedAggregator>())
                .When(x => AddSingletonDefinedAggregator<TestDefinedAggregator>())
                .Then(x => ThenTheProviderIsRegisteredAndReturnsSpecificAggregators<TestDefinedAggregator, TestDefinedAggregator>())
                .And(x => ThenTheAggregatorsAreSingleton<TestDefinedAggregator, TestDefinedAggregator>())
                .BDDfy();
        }

        [Fact]
        public void should_add_transient_defined_aggregators()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => AddTransientDefinedAggregator<TestDefinedAggregator>())
                .When(x => AddTransientDefinedAggregator<TestDefinedAggregator>())
                .Then(x => ThenTheProviderIsRegisteredAndReturnsSpecificAggregators<TestDefinedAggregator, TestDefinedAggregator>())
                .And(x => ThenTheAggregatorsAreTransient<TestDefinedAggregator, TestDefinedAggregator>())
                .BDDfy();
        }

        private void AddSingletonDefinedAggregator<T>()
            where T : class, IDefinedAggregator
        {
            _ocelotBuilder.AddSingletonDefinedAggregator<T>();
        }

        private void AddTransientDefinedAggregator<T>()
            where T : class, IDefinedAggregator
        {
            _ocelotBuilder.AddTransientDefinedAggregator<T>();
        }

        private void ThenTheSpecificHandlersAreSingleton()
        {
            var handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
            var first = handlers[0];
            handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
            var second = handlers[0];
            first.ShouldBe(second);
        }

        private void ThenTheSpecificHandlersAreTransient()
        {
            var handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
            var first = handlers[0];
            handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
            var second = handlers[0];
            first.ShouldNotBe(second);
        }

        private void ThenTheGlobalHandlersAreSingleton()
        {
            var handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
            var first = handlers[0].DelegatingHandler;
            handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
            var second = handlers[0].DelegatingHandler;
            first.ShouldBe(second);
        }

        private void ThenTheGlobalHandlersAreTransient()
        {
            var handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
            var first = handlers[0].DelegatingHandler;
            handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
            var second = handlers[0].DelegatingHandler;
            first.ShouldNotBe(second);
        }

        private void WhenISetUpAdministration()
        {
            _ocelotBuilder.AddAdministration("/administration", "secret");
        }

        private void WhenISetUpAdministration(Action<IdentityServerAuthenticationOptions> options)
        {
            _ocelotBuilder.AddAdministration("/administration", options);
        }

        private void AddTransientGlobalDelegatingHandler<T>()
            where T : DelegatingHandler
        {
            _ocelotBuilder.AddTransientDelegatingHandler<T>(true);
        }

        private void AddSpecificTransientDelegatingHandler<T>()
            where T : DelegatingHandler
        {
            _ocelotBuilder.AddTransientDelegatingHandler<T>();
        }

        private void ThenTheCorrectAdminPathIsRegitered()
        {
            _serviceProvider = _services.BuildServiceProvider();
            var path = _serviceProvider.GetService<IAdministrationPath>();
            path.Path.ShouldBe("/administration");
        }

        private void ThenTheProviderIsRegisteredAndReturnsHandlers<TOne, TWo>()
        {
            _serviceProvider = _services.BuildServiceProvider();
            var handlers = _serviceProvider.GetServices<GlobalDelegatingHandler>().ToList();
            handlers[0].DelegatingHandler.ShouldBeOfType<TOne>();
            handlers[1].DelegatingHandler.ShouldBeOfType<TWo>();
        }

        private void ThenTheProviderIsRegisteredAndReturnsSpecificHandlers<TOne, TWo>()
        {
            _serviceProvider = _services.BuildServiceProvider();
            var handlers = _serviceProvider.GetServices<DelegatingHandler>().ToList();
            handlers[0].ShouldBeOfType<TOne>();
            handlers[1].ShouldBeOfType<TWo>();
        }

        private void ThenTheProviderIsRegisteredAndReturnsSpecificAggregators<TOne, TWo>()
        {
            _serviceProvider = _services.BuildServiceProvider();
            var handlers = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
            handlers[0].ShouldBeOfType<TOne>();
            handlers[1].ShouldBeOfType<TWo>();
        }

        private void ThenTheAggregatorsAreTransient<TOne, TWo>()
        {
            var aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
            var first = aggregators[0];
            aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
            var second = aggregators[0];
            first.ShouldNotBe(second);
        }

        private void ThenTheAggregatorsAreSingleton<TOne, TWo>()
        {
            var aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
            var first = aggregators[0];
            aggregators = _serviceProvider.GetServices<IDefinedAggregator>().ToList();
            var second = aggregators[0];
            first.ShouldBe(second);
        }

        private void OnlyOneVersionOfEachCacheIsRegistered()
        {
            var outputCache = _services.Single(x => x.ServiceType == typeof(IOcelotCache<CachedResponse>));
            var outputCacheManager = _services.Single(x => x.ServiceType == typeof(ICacheManager<CachedResponse>));
            var instance = (ICacheManager<CachedResponse>)outputCacheManager.ImplementationInstance;
            var ocelotConfigCache = _services.Single(x => x.ServiceType == typeof(IOcelotCache<IInternalConfiguration>));
            var ocelotConfigCacheManager = _services.Single(x => x.ServiceType == typeof(ICacheManager<IInternalConfiguration>));
            var fileConfigCache = _services.Single(x => x.ServiceType == typeof(IOcelotCache<FileConfiguration>));
            var fileConfigCacheManager = _services.Single(x => x.ServiceType == typeof(ICacheManager<FileConfiguration>));

            instance.Configuration.MaxRetries.ShouldBe(_maxRetries);
            outputCache.ShouldNotBeNull();
            ocelotConfigCache.ShouldNotBeNull();
            ocelotConfigCacheManager.ShouldNotBeNull();
            fileConfigCache.ShouldNotBeNull();
            fileConfigCacheManager.ShouldNotBeNull();
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

        private void AddGlobalDelegatingHandler<T>()
            where T : DelegatingHandler
        {
            _ocelotBuilder.AddSingletonDelegatingHandler<T>(true);
        }

        private void AddSpecificDelegatingHandler<T>()
            where T : DelegatingHandler
        {
            _ocelotBuilder.AddSingletonDelegatingHandler<T>();
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

        private void WhenISetUpOpentracing()
        {
            try
            {
                _ocelotBuilder.AddOpenTracing(
                    option =>
                    {
                        option.CollectorUrl = "http://localhost:9618";
                        option.Service = "Ocelot.ManualTest";
                    }
               );
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
                _serviceProvider = _services.BuildServiceProvider();
                var logger = _serviceProvider.GetService<IFileConfigurationSetter>();
                logger.ShouldNotBeNull();
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }

        private void WhenIAccessOcelotHttpTracingHandler()
        {
            try
            {
                var tracingHandler = _serviceProvider.GetService<OcelotHttpTracingHandler>();
                tracingHandler.ShouldNotBeNull();
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
