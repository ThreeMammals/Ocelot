using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Ocelot.Configuration.ChangeTracking;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Configuration
{
    public sealed class DiskFileConfigurationRepositoryTests : FileUnitTest
    {
        private readonly Mock<IWebHostEnvironment> _hostingEnvironment;
        private readonly Mock<IOcelotConfigurationChangeTokenSource> _changeTokenSource;
        private IFileConfigurationRepository _repo;
        private FileConfiguration _result;

        public DiskFileConfigurationRepositoryTests()
        {
            _hostingEnvironment = new Mock<IWebHostEnvironment>();
            _changeTokenSource = new Mock<IOcelotConfigurationChangeTokenSource>(MockBehavior.Strict);
            _changeTokenSource.Setup(m => m.Activate());
        }

        private void Arrange([CallerMemberName] string testName = null)
        {
            _hostingEnvironment.Setup(he => he.EnvironmentName).Returns(testName);
            _repo = new DiskFileConfigurationRepository(_hostingEnvironment.Object, _changeTokenSource.Object, TestID);
        }

        [Fact]
        public async Task Should_return_file_configuration()
        {
            Arrange();
            var config = FakeFileConfigurationForGet();
            GivenTheConfigurationIs(config);

            // Act
            await WhenIGetTheRoutes();

            // Assert
            ThenTheFollowingIsReturned(config);
        }

        [Fact]
        public async Task Should_return_file_configuration_if_environment_name_is_unavailable()
        {
            Arrange();
            var config = FakeFileConfigurationForGet();
            GivenTheEnvironmentNameIsUnavailable();
            GivenTheConfigurationIs(config);

            // Act
            await WhenIGetTheRoutes();

            // Assert
            ThenTheFollowingIsReturned(config);
        }

        [Fact]
        public async Task Should_set_file_configuration()
        {
            Arrange();
            var config = FakeFileConfigurationForSet();

            // Act
            await WhenISetTheConfiguration(config);

            // Assert
            ThenTheConfigurationIsStoredAs(config);
            ThenTheConfigurationJsonIsIndented(config);
            AndTheChangeTokenIsActivated();
        }

        [Fact]
        public async Task Should_set_file_configuration_if_environment_name_is_unavailable()
        {
            Arrange();
            var config = FakeFileConfigurationForSet();
            GivenTheEnvironmentNameIsUnavailable();

            // Act
            await WhenISetTheConfiguration(config);

            // Assert
            ThenTheConfigurationIsStoredAs(config);
            ThenTheConfigurationJsonIsIndented(config);
        }

        [Fact]
        public async Task Should_set_environment_file_configuration_and_ocelot_file_configuration()
        {
            Arrange();
            var config = FakeFileConfigurationForSet();
            GivenTheConfigurationIs(config);
            var ocelotJson = GivenTheUserAddedOcelotJson();

            // Act
            await WhenISetTheConfiguration(config);

            // Assert
            ThenTheConfigurationIsStoredAs(config);
            ThenTheConfigurationJsonIsIndented(config);
            ThenTheOcelotJsonIsStoredAs(ocelotJson, config);
        }

        private FileInfo GivenTheUserAddedOcelotJson()
        {
            var primaryFile = Path.Combine(TestID, ConfigurationBuilderExtensions.PrimaryConfigFile);
            var ocelotJson = new FileInfo(primaryFile);
            if (ocelotJson.Exists)
            {
                ocelotJson.Delete();
            }

            File.WriteAllText(ocelotJson.FullName, "Doesnt matter");
            _files.Add(ocelotJson.FullName);
            return ocelotJson;
        }

        private void GivenTheEnvironmentNameIsUnavailable()
        {
            _hostingEnvironment.Setup(he => he.EnvironmentName).Returns((string)null);
        }

        private async Task WhenISetTheConfiguration(FileConfiguration fileConfiguration)
        {
            await _repo.Set(fileConfiguration);
            var response = await _repo.Get();
            _result = response.Data;
        }

        private void ThenTheConfigurationIsStoredAs(FileConfiguration expecteds)
        {
            _result.GlobalConfiguration.RequestIdKey.ShouldBe(expecteds.GlobalConfiguration.RequestIdKey);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Scheme.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Scheme);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for (var i = 0; i < _result.Routes.Count; i++)
            {
                for (var j = 0; j < _result.Routes[i].DownstreamHostAndPorts.Count; j++)
                {
                    var result = _result.Routes[i].DownstreamHostAndPorts[j];
                    var expected = expecteds.Routes[i].DownstreamHostAndPorts[j];

                    result.Host.ShouldBe(expected.Host);
                    result.Port.ShouldBe(expected.Port);
                }

                _result.Routes[i].DownstreamPathTemplate.ShouldBe(expecteds.Routes[i].DownstreamPathTemplate);
                _result.Routes[i].DownstreamScheme.ShouldBe(expecteds.Routes[i].DownstreamScheme);
            }
        }

        private void ThenTheOcelotJsonIsStoredAs(FileInfo ocelotJson, FileConfiguration expecteds)
        {
            var actual = File.ReadAllText(ocelotJson.FullName);
            var expectedText = JsonConvert.SerializeObject(expecteds, Formatting.Indented);
            actual.ShouldBe(expectedText);
        }

        private void GivenTheConfigurationIs(FileConfiguration fileConfiguration, [CallerMemberName] string environmentName = null)
        {
            var environmentSpecificPath = Path.Combine(TestID, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, environmentName));
            var jsonConfiguration = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);
            var environmentSpecific = new FileInfo(environmentSpecificPath);
            if (environmentSpecific.Exists)
            {
                environmentSpecific.Delete();
            }

            File.WriteAllText(environmentSpecific.FullName, jsonConfiguration);
            _files.Add(environmentSpecific.FullName);
        }

        private void ThenTheConfigurationJsonIsIndented(FileConfiguration expecteds, [CallerMemberName] string environmentName = null)
        {
            var environmentSpecific = Path.Combine(TestID, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, environmentName));
            var actual = File.ReadAllText(environmentSpecific);
            var expectedText = JsonConvert.SerializeObject(expecteds, Formatting.Indented);
            actual.ShouldBe(expectedText);
            _files.Add(environmentSpecific);
        }

        private async Task WhenIGetTheRoutes()
        {
            var response = await _repo.Get();
            _result = response.Data;
        }

        private void ThenTheFollowingIsReturned(FileConfiguration expecteds)
        {
            _result.GlobalConfiguration.RequestIdKey.ShouldBe(expecteds.GlobalConfiguration.RequestIdKey);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Scheme.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Scheme);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            _result.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(expecteds.GlobalConfiguration.ServiceDiscoveryProvider.Port);

            for (var i = 0; i < _result.Routes.Count; i++)
            {
                for (var j = 0; j < _result.Routes[i].DownstreamHostAndPorts.Count; j++)
                {
                    var result = _result.Routes[i].DownstreamHostAndPorts[j];
                    var expected = expecteds.Routes[i].DownstreamHostAndPorts[j];

                    result.Host.ShouldBe(expected.Host);
                    result.Port.ShouldBe(expected.Port);
                }

                _result.Routes[i].DownstreamPathTemplate.ShouldBe(expecteds.Routes[i].DownstreamPathTemplate);
                _result.Routes[i].DownstreamScheme.ShouldBe(expecteds.Routes[i].DownstreamScheme);
            }
        }

        private void AndTheChangeTokenIsActivated()
        {
            _changeTokenSource.Verify(m => m.Activate(), Times.Once);
        }

        private static FileConfiguration FakeFileConfigurationForSet()
        {
            var route = GivenRoute("123.12.12.12", "/asdfs/test/{test}");
            return GivenConfiguration(route);
        }

        private static FileConfiguration FakeFileConfigurationForGet()
        {
            var route = GivenRoute("localhost", "/test/test/{test}");
            return GivenConfiguration(route);
        }

        private static FileRoute GivenRoute(string host, string downstream) => new()
        {
            DownstreamHostAndPorts = new() { new(host, 80) },
            DownstreamScheme = Uri.UriSchemeHttps,
            DownstreamPathTemplate = downstream,
        };

        private static FileConfiguration GivenConfiguration(params FileRoute[] routes)
        {
            var config = new FileConfiguration();
            config.Routes.AddRange(routes);
            config.GlobalConfiguration.ServiceDiscoveryProvider = new()
            {
                Scheme = "https",
                Port = 198,
                Host = "blah",
            };
            return config;
        }
    }
}
