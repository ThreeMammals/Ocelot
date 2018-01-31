using System;
using System.Collections.Generic;
using Moq;
using Ocelot.Configuration.File;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Configuration.Repository;

namespace Ocelot.UnitTests.Configuration
{
    public class FileConfigurationRepositoryTests
    {
        private readonly Mock<IHostingEnvironment> _hostingEnvironment = new Mock<IHostingEnvironment>();
        private IFileConfigurationRepository _repo;
        private FileConfiguration _result;
        private FileConfiguration _fileConfiguration;
        private string _environmentName = "DEV";

        public FileConfigurationRepositoryTests()
        {
            _hostingEnvironment.Setup(he => he.EnvironmentName).Returns(_environmentName);
            _repo = new FileConfigurationRepository(_hostingEnvironment.Object);
        }

        [Fact]
        public void should_return_file_configuration()
        {
            var config = FakeFileConfigurationForGet();

            this.Given(x => x.GivenTheConfigurationIs(config))
                .When(x => x.WhenIGetTheReRoutes())
                .Then(x => x.ThenTheFollowingIsReturned(config))
                .BDDfy();
        }

        [Fact]
        public void should_return_file_configuration_if_environment_name_is_unavailable()
        {
            var config = FakeFileConfigurationForGet();

            this.Given(x => x.GivenTheEnvironmentNameIsUnavailable())
                .And(x => x.GivenTheConfigurationIs(config))
                .When(x => x.WhenIGetTheReRoutes())
                .Then(x => x.ThenTheFollowingIsReturned(config))
                .BDDfy();
        }

        [Fact]
        public void should_set_file_configuration()
        {
            var config = FakeFileConfigurationForSet();

            this.Given(x => GivenIHaveAConfiguration(config))
                .When(x => WhenISetTheConfiguration())
                .Then(x => ThenTheConfigurationIsStoredAs(config))
                .BDDfy();
        }

        [Fact]
        public void should_set_file_configuration_if_environment_name_is_unavailable()
        {
            var config = FakeFileConfigurationForSet();
            this.Given(x => GivenIHaveAConfiguration(config))
                .And(x => GivenTheEnvironmentNameIsUnavailable())
                .When(x => WhenISetTheConfiguration())
                .Then(x => ThenTheConfigurationIsStoredAs(config))
                .BDDfy();
        }

        private void GivenTheEnvironmentNameIsUnavailable()
        {
            _environmentName = null;
            _hostingEnvironment.Setup(he => he.EnvironmentName).Returns(_environmentName);
            _repo = new FileConfigurationRepository(_hostingEnvironment.Object);
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

        private void  GivenTheConfigurationIs(FileConfiguration fileConfiguration)
        {
            var configurationPath = $"{AppContext.BaseDirectory}/configuration{(string.IsNullOrEmpty(_environmentName) ? string.Empty : ".")}{_environmentName}.json";

            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration);

            if (File.Exists(configurationPath))
            {
                File.Delete(configurationPath);
            }

            File.WriteAllText(configurationPath, jsonConfiguration);
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
    }
}
