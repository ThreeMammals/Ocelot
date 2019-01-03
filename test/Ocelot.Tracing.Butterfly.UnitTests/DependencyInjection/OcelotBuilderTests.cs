namespace Ocelot.Tracing.Butterfly.UnitTests.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.DependencyInjection;
    using Requester;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

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
        public void should_set_up_tracing()
        {
            this.Given(x => WhenISetUpOcelotServices())
                .When(x => WhenIAddButterflyTracing())
                .When(x => WhenIAccessOcelotHttpTracingHandler())
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
        
        private void WhenIAddButterflyTracing()
        {
            try
            {
                _ocelotBuilder.AddButterfly(
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

        private void WhenIAccessOcelotHttpTracingHandler()
        {
            try
            {
                _serviceProvider = _services.BuildServiceProvider();
                var tracingHandler = _serviceProvider.GetService<OcelotHttpTracingHandler>();
                tracingHandler.ShouldNotBeNull();
                var tracer = _serviceProvider.GetService<ITracer>();
                tracer.ShouldNotBeNull();
            }
            catch (Exception e)
            {
                _ex = e;
            }
        }
    }
}
