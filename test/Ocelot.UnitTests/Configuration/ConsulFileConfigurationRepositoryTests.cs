namespace Ocelot.UnitTests.Configuration
{
    using Xunit;
    using TestStack.BDDfy;
    using Shouldly;
    using Ocelot.Configuration.Repository;
    using Moq;
    using Ocelot.Infrastructure.Consul;
    using Ocelot.Logging;
    using Ocelot.Configuration.File;
    using Ocelot.Cache;
    using System;
    using System.Collections.Generic;
    using Ocelot.Responses;
    using System.Threading.Tasks;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.ServiceDiscovery.Configuration;
    using Consul;
    using Newtonsoft.Json;
    using System.Text;
    using System.Threading;
    using System.Linq;

    public class ConsulFileConfigurationRepositoryTests
    {
        private ConsulFileConfigurationRepository _repo;
        private Mock<IOcelotCache<FileConfiguration>> _cache;
        private Mock<IInternalConfigurationRepository> _internalRepo;
        private Mock<IConsulClientFactory> _factory;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IConsulClient> _client;
        private Mock<IKVEndpoint> _kvEndpoint;
        private FileConfiguration _fileConfiguration;
        private Response _result;

        public ConsulFileConfigurationRepositoryTests()
        {
            _cache = new Mock<IOcelotCache<FileConfiguration>>();
            _internalRepo = new Mock<IInternalConfigurationRepository>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();

            _factory = new Mock<IConsulClientFactory>();
            _client = new Mock<IConsulClient>();
            _kvEndpoint = new Mock<IKVEndpoint>();

            _client
                .Setup(x => x.KV)
                .Returns(_kvEndpoint.Object);
            _factory
                .Setup(x => x.Get(It.IsAny<ConsulRegistryConfiguration>()))
                .Returns(_client.Object);

            _internalRepo
                .Setup(x => x.Get())
                .Returns(new OkResponse<IInternalConfiguration>(new InternalConfiguration(new List<ReRoute>(), "", new ServiceProviderConfigurationBuilder().Build(), "")));
            
            _repo = new ConsulFileConfigurationRepository(_cache.Object, _internalRepo.Object, _factory.Object, _loggerFactory.Object);
        }

        [Fact]
        public void should_set_config()
        {
            var config = FakeFileConfiguration();

            this.Given(_ => GivenIHaveAConfiguration(config))
                .And(_ => GivenWritingToConsulSucceeds())
                .When(_ => WhenISetTheConfiguration())
                .Then(_ => ThenTheConfigurationIsStoredAs(config))
                .BDDfy();
        }

        private void GivenWritingToConsulSucceeds()
        {
            var response = new WriteResult<bool>();
            response.Response = true;

            _kvEndpoint
                .Setup(x => x.Put(It.IsAny<KVPair>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);
        }

        private void ThenTheConfigurationIsStoredAs(FileConfiguration config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);

            var bytes = Encoding.UTF8.GetBytes(json);

            _kvEndpoint
                .Verify(x => x.Put(It.Is<KVPair>(k => k.Value.SequenceEqual(bytes)), It.IsAny<CancellationToken>()), Times.Once);
        }

        private async Task WhenISetTheConfiguration()
        {
            _result = await _repo.Set(_fileConfiguration);
        }

        private void GivenIHaveAConfiguration(FileConfiguration config)
        {
            _fileConfiguration = config;
        }
        
        private FileConfiguration FakeFileConfiguration()
        {
            var reRoutes = new List<FileReRoute>
            {
                new FileReRoute
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = "123.12.12.12",
                            Port = 80,
                        }
                    },
                    DownstreamScheme = "https",
                    DownstreamPathTemplate = "/asdfs/test/{test}"
                }
            };

            var globalConfiguration = new FileGlobalConfiguration
            {
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Port = 198,
                    Host = "blah"
                }
            };

            return new FileConfiguration
            {
                GlobalConfiguration = globalConfiguration,
                ReRoutes = reRoutes
            };
        }
    }
}