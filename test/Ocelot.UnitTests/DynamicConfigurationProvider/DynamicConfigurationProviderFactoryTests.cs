using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.DynamicConfigurationProvider;
using Ocelot.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Parser;

namespace Ocelot.UnitTests.DynamicConfigurationProvider
{
    public class DynamicConfigurationProviderFactoryTests
    {
        private readonly Mock<IOcelotLogger> _logger;
        private readonly IDynamicConfigurationProviderFactory _factory;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IServiceProvider> _provider;
        private readonly Mock<IRedisDataConfigurationParser> _redisDataConfigurationParser;
        private IInternalConfiguration _config;
        private Ocelot.DynamicConfigurationProvider.DynamicConfigurationProvider _result;

        public DynamicConfigurationProviderFactoryTests()
        {
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _provider = new Mock<IServiceProvider>();
            _redisDataConfigurationParser = new Mock<IRedisDataConfigurationParser>();
            _loggerFactory.Setup(x => x.CreateLogger<RedisDynamicConfigurationProvider>()).Returns(_logger.Object);
            _loggerFactory.Setup(x => x.CreateLogger<DynamicConfigurationProviderFactory>()).Returns(_logger.Object);

            var services = new ServiceCollection();
            services.AddSingleton(typeof(IServiceProvider), _provider.Object);
            services.AddSingleton(typeof(IOcelotLogger), _logger.Object);
            services.AddSingleton(typeof(IOcelotLoggerFactory), _loggerFactory.Object);
            services.AddSingleton(typeof(IRedisDataConfigurationParser), _redisDataConfigurationParser.Object);
            services.AddSingleton<Ocelot.DynamicConfigurationProvider.DynamicConfigurationProvider, FakeOneProvider>();
            services.AddSingleton<Ocelot.DynamicConfigurationProvider.DynamicConfigurationProvider, FakeTwoProvider>();
            services.AddSingleton<Ocelot.DynamicConfigurationProvider.DynamicConfigurationProvider, RedisDynamicConfigurationProvider>();
            var provider = services.BuildServiceProvider();
            
            _factory = new DynamicConfigurationProviderFactory(provider, _loggerFactory.Object);
        }

        [Fact]
        public void should_return_no_provider()
        {
            var internalConfiguration = new InternalConfiguration(null, null, null, null, null, null, null, null, null, null);

            this.Given(_ => GivenTheConfiguration(internalConfiguration))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBeNull())
                .BDDfy();
        }

        [Fact]
        public void should_return_no_provider_with_invalid_store()
        {
            var dynamicReRouteConfiguration = new DynamicConfigurationBuilder()
                                                .WithStore("InvalidStore")
                                                .Build();

            var internalConfiguration = new InternalConfiguration(null, null, null, null, dynamicReRouteConfiguration, null, null, null, null, null);

            this.Given(_ => GivenTheConfiguration(internalConfiguration))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBeNull())
                .BDDfy();
        }

        [Fact]
        public void should_return_fake_one_provider()
        {
            var dynamicReRouteConfiguration = new DynamicConfigurationBuilder()
                                                .WithStore("StoreOne")
                                                .Build();

            var internalConfiguration = new InternalConfiguration(null, null, null, null, dynamicReRouteConfiguration, null, null, null, null, null);

            this.Given(_ => GivenTheConfiguration(internalConfiguration))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<FakeOneProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_fake_two_provider()
        {
            var dynamicReRouteConfiguration = new DynamicConfigurationBuilder()
                                                .WithStore("StoreTwo")
                                                .Build();

            var internalConfiguration = new InternalConfiguration(null, null, null, null, dynamicReRouteConfiguration, null, null, null, null, null);

            this.Given(_ => GivenTheConfiguration(internalConfiguration))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<FakeTwoProvider>())
                .BDDfy();
        }

        [Fact]
        public void should_return_redis_provider()
        {
            var dynamicReRouteConfiguration = new DynamicConfigurationBuilder()
                                                .WithStore("Redis")
                                                .Build();

            var internalConfiguration = new InternalConfiguration(null, null, null, null, dynamicReRouteConfiguration, null, null, null, null, null);

            this.Given(_ => GivenTheConfiguration(internalConfiguration))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<RedisDynamicConfigurationProvider>())
                .BDDfy();
        }

        private void WhenIGet()
        {
            _result = _factory.Get(_config);
        }

        private void GivenTheConfiguration(IInternalConfiguration configuration)
        {
            _config = configuration;
        }

        private void ThenTheResultShouldBe<T>()
        {
            _result.ShouldBeOfType<T>();
        }

        private void ThenTheResultShouldBeNull()
        {
            _result.ShouldBeNull();
        }

        [ConfigurationStoreAttribute("StoreOne")]
        class FakeOneProvider : Ocelot.DynamicConfigurationProvider.DynamicConfigurationProvider
        {
            public FakeOneProvider(IOcelotLogger logger) : base(logger) { }

            protected override Task<FileReRoute> GetRouteConfigurationAsync(string host, string port, string key)
            {
                throw new NotImplementedException();
            }
        }

        [ConfigurationStoreAttribute("StoreTwo")]
        class FakeTwoProvider : Ocelot.DynamicConfigurationProvider.DynamicConfigurationProvider
        {
            public FakeTwoProvider(IOcelotLogger logger) : base(logger) { }

            protected override Task<FileReRoute> GetRouteConfigurationAsync(string host, string port, string key)
            {
                throw new NotImplementedException();
            }
        }
    }
}
