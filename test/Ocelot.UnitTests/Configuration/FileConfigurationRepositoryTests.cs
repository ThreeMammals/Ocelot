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

        private void ThenTheConfigurationIsStoredAs(FileConfiguration expected)
        {
            _result.GlobalConfiguration.AdministrationPath.ShouldBe(expected.GlobalConfiguration.AdministrationPath);
            _result.GlobalConfiguration.RequestIdKey.ShouldBe(expected.GlobalConfiguration.RequestIdKey);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for(var i = 0; i < _result.ReRoutes.Count; i++)
            {
                _result.ReRoutes[i].DownstreamHost.ShouldBe(expected.ReRoutes[i].DownstreamHost);
                _result.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expected.ReRoutes[i].DownstreamPathTemplate);
                _result.ReRoutes[i].DownstreamPort.ShouldBe(expected.ReRoutes[i].DownstreamPort);
                _result.ReRoutes[i].DownstreamScheme.ShouldBe(expected.ReRoutes[i].DownstreamScheme);
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

        private void ThenTheFollowingIsReturned(FileConfiguration expected)
        {
            _result.GlobalConfiguration.AdministrationPath.ShouldBe(expected.GlobalConfiguration.AdministrationPath);
            _result.GlobalConfiguration.RequestIdKey.ShouldBe(expected.GlobalConfiguration.RequestIdKey);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expected.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for(var i = 0; i < _result.ReRoutes.Count; i++)
            {
                _result.ReRoutes[i].DownstreamHost.ShouldBe(expected.ReRoutes[i].DownstreamHost);
                _result.ReRoutes[i].DownstreamPathTemplate.ShouldBe(expected.ReRoutes[i].DownstreamPathTemplate);
                _result.ReRoutes[i].DownstreamPort.ShouldBe(expected.ReRoutes[i].DownstreamPort);
                _result.ReRoutes[i].DownstreamScheme.ShouldBe(expected.ReRoutes[i].DownstreamScheme);
            }
        }

        private FileConfiguration FakeFileConfigurationForSet()
        {
            var reRoutes = new List<FileReRoute>
            {
                new FileReRoute
                {
                    DownstreamHost = "123.12.12.12",
                    DownstreamPort = 80,
                    DownstreamScheme = "https",
                    DownstreamPathTemplate = "/asdfs/test/{test}"
                }
            };

            var globalConfiguration = new FileGlobalConfiguration
            {
                AdministrationPath = "asdas",
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
                    DownstreamHost = "localhost",
                    DownstreamPort = 80,
                    DownstreamScheme = "https",
                    DownstreamPathTemplate = "/test/test/{test}"
                }
            };

            var globalConfiguration = new FileGlobalConfiguration
            {
                AdministrationPath = "testy",
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