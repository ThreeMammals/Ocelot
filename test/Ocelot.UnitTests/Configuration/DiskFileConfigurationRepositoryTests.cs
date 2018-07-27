namespace Ocelot.UnitTests.Configuration
{
    using System;
    using System.Collections.Generic;
    using Moq;
    using Ocelot.Configuration.File;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using Newtonsoft.Json;
    using System.IO;
    using System.Threading;
    using Microsoft.AspNetCore.Hosting;
    using Ocelot.Configuration.Repository;

    public class DiskFileConfigurationRepositoryTests : IDisposable
    {
        private readonly Mock<IHostingEnvironment> _hostingEnvironment;
        private IFileConfigurationRepository _repo;
        private string _environmentSpecificPath;
        private string _ocelotJsonPath;
        private FileConfiguration _result;
        private FileConfiguration _fileConfiguration;

        // This is a bit dirty and it is dev.dev so that the ConfigurationBuilderExtensionsTests
        // cant pick it up if they run in parralel..and the semaphore stops them running at the same time...sigh
        // these are not really unit tests but whatever...
        private string _environmentName = "DEV.DEV";
        private static SemaphoreSlim _semaphore;

        public DiskFileConfigurationRepositoryTests()
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _semaphore.Wait();
            _hostingEnvironment = new Mock<IHostingEnvironment>();
            _hostingEnvironment.Setup(he => he.EnvironmentName).Returns(_environmentName);
            _repo = new DiskFileConfigurationRepository(_hostingEnvironment.Object);
        }

        [Fact]
        public void should_return_file_configuration()
        {
            var config = FakeFileConfigurationForGet();

            this.Given(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenIGetTheReRoutes())
                .Then(_ => ThenTheFollowingIsReturned(config))
                .BDDfy();
        }

        [Fact]
        public void should_return_file_configuration_if_environment_name_is_unavailable()
        {
            var config = FakeFileConfigurationForGet();

            this.Given(_ => GivenTheEnvironmentNameIsUnavailable())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenIGetTheReRoutes())
                .Then(_ => ThenTheFollowingIsReturned(config))
                .BDDfy();
        }

        [Fact]
        public void should_set_file_configuration()
        {
            var config = FakeFileConfigurationForSet();

            this.Given(_ => GivenIHaveAConfiguration(config))
                .When(_ => WhenISetTheConfiguration())
                .Then(_ => ThenTheConfigurationIsStoredAs(config))
                .And(_ => ThenTheConfigurationJsonIsIndented(config))
                .BDDfy();
        }

        [Fact]
        public void should_set_file_configuration_if_environment_name_is_unavailable()
        {
            var config = FakeFileConfigurationForSet();

            this.Given(_ => GivenIHaveAConfiguration(config))
                .And(_ => GivenTheEnvironmentNameIsUnavailable())
                .When(_ => WhenISetTheConfiguration())
                .Then(_ => ThenTheConfigurationIsStoredAs(config))
                .And(_ => ThenTheConfigurationJsonIsIndented(config))
                .BDDfy();
        }

        [Fact]
        public void should_set_environment_file_configuration_and_ocelot_file_configuration()
        {
            var config = FakeFileConfigurationForSet();

            this.Given(_ => GivenIHaveAConfiguration(config))
                .And(_ => GivenTheConfigurationIs(config))
                .And(_ => GivenTheUserAddedOcelotJson())
                .When(_ => WhenISetTheConfiguration())
                .Then(_ => ThenTheConfigurationIsStoredAs(config))
                .And(_ => ThenTheConfigurationJsonIsIndented(config))
                .Then(_ => ThenTheOcelotJsonIsStoredAs(config))
                .BDDfy();
        }

        private void GivenTheUserAddedOcelotJson()
        {
             _ocelotJsonPath = $"{AppContext.BaseDirectory}/ocelot.json";

            if (File.Exists(_ocelotJsonPath))
            {
                File.Delete(_ocelotJsonPath);
            }

            File.WriteAllText(_ocelotJsonPath, "Doesnt matter");
        }

        private void GivenTheEnvironmentNameIsUnavailable()
        {
            _environmentName = null;
            _hostingEnvironment.Setup(he => he.EnvironmentName).Returns(_environmentName);
            _repo = new DiskFileConfigurationRepository(_hostingEnvironment.Object);
        }

        private void GivenIHaveAConfiguration(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
        }

        private void WhenISetTheConfiguration()
        {
            _repo.Set(_fileConfiguration);
            _result = _repo.Get().Result.Data;
        }

        private void ThenTheConfigurationIsStoredAs(FileConfiguration expecteds)
        {
            _result.GlobalConfiguration.RequestIdKey.ShouldBe(expecteds.GlobalConfiguration.RequestIdKey);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for(var i = 0; i < _result.ReRoutes.Count; i++)
            {
                for (int j = 0; j < _result.ReRoutes[i].DownstreamHostAndPorts.Count; j++)
                {
                    var result = _result.ReRoutes[i].DownstreamHostAndPorts[j];
                    var expected = expecteds.ReRoutes[i].DownstreamHostAndPorts[j];

                    result.Host.ShouldBe(expected.Host);
                    result.Port.ShouldBe(expected.Port);
                }

                _result.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expecteds.ReRoutes[i].DownstreamPathTemplate);
                _result.ReRoutes[i].DownstreamScheme.ShouldBe(expecteds.ReRoutes[i].DownstreamScheme);
            }
        }

        private void ThenTheOcelotJsonIsStoredAs(FileConfiguration expecteds)
        {
            var resultText = File.ReadAllText(_ocelotJsonPath);
            var expectedText = JsonConvert.SerializeObject(expecteds, Formatting.Indented);
            resultText.ShouldBe(expectedText);
        }

        private void GivenTheConfigurationIs(FileConfiguration fileConfiguration)
        {
            _environmentSpecificPath = $"{AppContext.BaseDirectory}/ocelot{(string.IsNullOrEmpty(_environmentName) ? string.Empty : ".")}{_environmentName}.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);

            if (File.Exists(_environmentSpecificPath))
            {
                File.Delete(_environmentSpecificPath);
            }

            File.WriteAllText(_environmentSpecificPath, jsonConfiguration);
        }

        private void ThenTheConfigurationJsonIsIndented(FileConfiguration expecteds)
        {
            var path = !string.IsNullOrEmpty(_environmentSpecificPath) ? _environmentSpecificPath : _environmentSpecificPath = $"{AppContext.BaseDirectory}/ocelot{(string.IsNullOrEmpty(_environmentName) ? string.Empty : ".")}{_environmentName}.json";
            
            var resultText = File.ReadAllText(path);
            var expectedText = JsonConvert.SerializeObject(expecteds, Formatting.Indented);
            resultText.ShouldBe(expectedText);
        }

        private void WhenIGetTheReRoutes()
        {
            _result = _repo.Get().Result.Data;
        }

        private void ThenTheFollowingIsReturned(FileConfiguration expecteds)
        {
            _result.GlobalConfiguration.RequestIdKey.ShouldBe(expecteds.GlobalConfiguration.RequestIdKey);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for(var i = 0; i < _result.ReRoutes.Count; i++)
            {
                for (int j = 0; j < _result.ReRoutes[i].DownstreamHostAndPorts.Count; j++)
                {
                    var result = _result.ReRoutes[i].DownstreamHostAndPorts[j];
                    var expected = expecteds.ReRoutes[i].DownstreamHostAndPorts[j];

                    result.Host.ShouldBe(expected.Host);
                    result.Port.ShouldBe(expected.Port);
                }

                _result.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expecteds.ReRoutes[i].DownstreamPathTemplate);
                _result.ReRoutes[i].DownstreamScheme.ShouldBe(expecteds.ReRoutes[i].DownstreamScheme);
            }
        }

        private FileConfiguration FakeFileConfigurationForSet()
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

        private FileConfiguration FakeFileConfigurationForGet()
        {
            var reRoutes = new List<FileReRoute>
            {
                new FileReRoute
                {
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new FileHostAndPort
                        {
                            Host = "localhost",
                            Port = 80,
                        }
                    },
                    DownstreamScheme = "https",
                    DownstreamPathTemplate = "/test/test/{test}"
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

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
