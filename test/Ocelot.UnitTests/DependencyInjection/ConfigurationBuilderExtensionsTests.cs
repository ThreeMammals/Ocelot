using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.DependencyInjection
{
    public sealed class ConfigurationBuilderExtensionsTests : FileUnitTest
    {
        private IConfigurationRoot _configuration;
        private IConfigurationRoot _configRoot;
        private FileConfiguration _globalConfig;
        private FileConfiguration _routeA;
        private FileConfiguration _routeB;
        private FileConfiguration _aggregate;
        private FileConfiguration _envSpecific;
        private FileConfiguration _combinedFileConfiguration;
        private readonly Mock<IWebHostEnvironment> _hostingEnvironment;

        public ConfigurationBuilderExtensionsTests()
        {
            _hostingEnvironment = new Mock<IWebHostEnvironment>();
        }

        protected override string EnvironmentName()
            => _hostingEnvironment?.Object?.EnvironmentName ?? base.EnvironmentName();

        [Fact]
        public void Should_add_base_url_to_config()
        {
            // Arrange
            _configuration = new ConfigurationBuilder()
                .AddOcelotBaseUrl("test")
                .Build();

            // Act
            var actual = _configuration.GetValue("BaseUrl", string.Empty);

            // Assert
            actual.ShouldBe("test");
        }

        [Fact]
        [Trait("PR", "1227")]
        [Trait("Issue", "1216")]
        public void Should_merge_files_to_file()
        {
            // Arrange
            GivenTheEnvironmentIs(TestID);
            GivenMultipleConfigurationFiles(TestID);

            // Act
            WhenIAddOcelotConfiguration(TestID);

            // Assert
            ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
            TheOcelotPrimaryConfigFileExists(true);
        }

        [Fact]
        public void Should_store_given_configurations_when_provided_file_configuration_object()
        {
            // Arrange
            GivenTheEnvironmentIs(TestID);
            GivenCombinedFileConfigurationObject();

            // Act
            WhenIAddOcelotConfigurationWithCombinedFileConfiguration();

            // Assert
            ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(true);
        }

        [Fact]
        public void Should_merge_files_except_env()
        {
            // Arrange
            GivenTheEnvironmentIs(TestID);
            GivenMultipleConfigurationFiles(TestID, true);

            // Act
            WhenIAddOcelotConfiguration(TestID);

            // Assert
            ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
            NotContainsEnvSpecificConfig();
        }

        [Fact]
        public void Should_merge_files_in_specific_folder()
        {
            // Arrange
            GivenMultipleConfigurationFiles(TestID);

            // Act
            WhenIAddOcelotConfiguration(TestID);

            // Assert
            ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
        }

        [Fact]
        [Trait("PR", "1227")]
        [Trait("Issue", "1216")]
        public void Should_merge_files_to_memory()
        {
            // Arrange
            GivenTheEnvironmentIs(TestID);
            GivenMultipleConfigurationFiles(TestID);

            // Act
            WhenIAddOcelotConfiguration(TestID, MergeOcelotJson.ToMemory);

            // Assert
            ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
            TheOcelotPrimaryConfigFileExists(false);
        }

        [Fact]
        [Trait("PR", "1986")]
        [Trait("Issue", "1518")]
        public void Should_merge_files_with_null_environment()
        {
            // Arrange
            _environmentConfigFileName = null; // Ups!
            const IWebHostEnvironment NullEnvironment = null; // Wow!
            GivenMultipleConfigurationFiles(TestID, false);

            // Act
            _configRoot = new ConfigurationBuilder()
                .AddOcelot(TestID, NullEnvironment, MergeOcelotJson.ToMemory, _primaryConfigFileName, _globalConfigFileName, _environmentConfigFileName, false, false)
                .Build();

            // Assert
            ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false);
            TheOcelotPrimaryConfigFileExists(false);
        }

        private void GivenCombinedFileConfigurationObject()
        {
            _combinedFileConfiguration = new FileConfiguration
            {
                GlobalConfiguration = GetFileGlobalConfigurationData(),
                Routes = GetServiceARoutes().Concat(GetServiceBRoutes()).Concat(GetEnvironmentSpecificRoutes()).ToList(),
                Aggregates = GetFileAggregatesRouteData(),
            };
        }

        private void GivenMultipleConfigurationFiles(string folder, bool withEnvironment = false)
        {
            _globalConfig = new() { GlobalConfiguration = GetFileGlobalConfigurationData() };
            _routeA = new() { Routes = GetServiceARoutes() };
            _routeB = new() { Routes = GetServiceBRoutes() };
            _aggregate = new() { Aggregates = GetFileAggregatesRouteData() };
            _envSpecific = new() { Routes = GetEnvironmentSpecificRoutes() };

            var configParts = new Dictionary<string, FileConfiguration>
            {
                { "global", _globalConfig },
                { "routesA", _routeA },
                { "routesB", _routeB },
                { "aggregates", _aggregate },
            };

            if (withEnvironment)
            {
                configParts.Add(EnvironmentName(), _envSpecific);
            }

            foreach (var part in configParts)
            {
                var filename = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, part.Key));
                File.WriteAllText(filename, JsonConvert.SerializeObject(part.Value, Formatting.Indented));
                _files.Add(filename);
            }
        }

        private static FileGlobalConfiguration GetFileGlobalConfigurationData() => new()
        {
            BaseUrl = "BaseUrl",
            RateLimitOptions = new()
            {
                HttpStatusCode = 500,
                ClientIdHeader = "ClientIdHeader",
                DisableRateLimitHeaders = true,
                QuotaExceededMessage = "QuotaExceededMessage",
                RateLimitCounterPrefix = "RateLimitCounterPrefix",
            },
            ServiceDiscoveryProvider = new()
            {
                Scheme = "https",
                Host = "Host",
                Port = 80,
                Type = "Type",
            },
            RequestIdKey = "RequestIdKey",
        };

        private static List<FileAggregateRoute> GetFileAggregatesRouteData() => new()
        {
            new()
            {
                RouteKeys = new() { "KeyB", "KeyBB" },
                UpstreamPathTemplate = "UpstreamPathTemplate",
            },
        };

        private static FileRoute GetRoute(string suffix) => new()
        {
            DownstreamScheme = "DownstreamScheme" + suffix,
            DownstreamPathTemplate = "DownstreamPathTemplate" + suffix,
            Key = "Key" + suffix,
            UpstreamHost = "UpstreamHost" + suffix,
            UpstreamHttpMethod = new() { "UpstreamHttpMethod" + suffix },
            DownstreamHostAndPorts = new()
            {
                new("Host"+suffix, 80),
            },
        };

        private static List<FileRoute> GetServiceARoutes() => new() { GetRoute("A") };
        private static List<FileRoute> GetServiceBRoutes() => new() { GetRoute("B"), GetRoute("BB") };
        private static List<FileRoute> GetEnvironmentSpecificRoutes() => new() { GetRoute("Spec") };

        private void GivenTheEnvironmentIs(string folder, [CallerMemberName] string testName = null)
        {
            _hostingEnvironment.SetupGet(x => x.EnvironmentName).Returns(testName);
            _environmentConfigFileName = Path.Combine(folder, string.Format(ConfigurationBuilderExtensions.EnvironmentConfigFile, testName));
            _files.Add(_environmentConfigFileName);
        }

        private void WhenIAddOcelotConfigurationWithCombinedFileConfiguration()
        {
            _configRoot = new ConfigurationBuilder()
                .AddOcelot(_combinedFileConfiguration, _primaryConfigFileName, false, false)
                .Build();
        }

        private void WhenIAddOcelotConfiguration(string folder, MergeOcelotJson mergeOcelotJson = MergeOcelotJson.ToFile)
        {
            _configRoot = new ConfigurationBuilder()
                .AddOcelot(folder, _hostingEnvironment.Object, mergeOcelotJson, _primaryConfigFileName, _globalConfigFileName, _environmentConfigFileName, false, false)
                .Build();
        }

        private void ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(bool useCombinedConfig)
        {
            var fc = (FileConfiguration)_configRoot.Get(typeof(FileConfiguration));

            fc.GlobalConfiguration.BaseUrl.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.BaseUrl : _globalConfig.GlobalConfiguration.BaseUrl);
            fc.GlobalConfiguration.RateLimitOptions.ClientIdHeader.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.RateLimitOptions.ClientIdHeader : _globalConfig.GlobalConfiguration.RateLimitOptions.ClientIdHeader);
            fc.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders : _globalConfig.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders);
            fc.GlobalConfiguration.RateLimitOptions.HttpStatusCode.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.RateLimitOptions.HttpStatusCode : _globalConfig.GlobalConfiguration.RateLimitOptions.HttpStatusCode);
            fc.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage : _globalConfig.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage);
            fc.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix : _globalConfig.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix);
            fc.GlobalConfiguration.RequestIdKey.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.RequestIdKey : _globalConfig.GlobalConfiguration.RequestIdKey);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Scheme.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.ServiceDiscoveryProvider.Scheme : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Scheme);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.ServiceDiscoveryProvider.Host : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.ServiceDiscoveryProvider.Port : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Port);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Type.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.GlobalConfiguration.ServiceDiscoveryProvider.Type : _globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Type);

            fc.Routes.Count.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.Routes.Count : _routeA.Routes.Count + _routeB.Routes.Count);

            fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == (useCombinedConfig ? _combinedFileConfiguration.Routes[0].DownstreamPathTemplate : _routeA.Routes[0].DownstreamPathTemplate));
            fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == (useCombinedConfig ? _combinedFileConfiguration.Routes[1].DownstreamPathTemplate : _routeB.Routes[0].DownstreamPathTemplate));
            fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == (useCombinedConfig ? _combinedFileConfiguration.Routes[2].DownstreamPathTemplate : _routeB.Routes[1].DownstreamPathTemplate));

            fc.Routes.ShouldContain(x => x.DownstreamScheme == (useCombinedConfig ? _combinedFileConfiguration.Routes[0].DownstreamScheme : _routeA.Routes[0].DownstreamScheme));
            fc.Routes.ShouldContain(x => x.DownstreamScheme == (useCombinedConfig ? _combinedFileConfiguration.Routes[1].DownstreamScheme : _routeB.Routes[0].DownstreamScheme));
            fc.Routes.ShouldContain(x => x.DownstreamScheme == (useCombinedConfig ? _combinedFileConfiguration.Routes[2].DownstreamScheme : _routeB.Routes[1].DownstreamScheme));

            fc.Routes.ShouldContain(x => x.Key == (useCombinedConfig ? _combinedFileConfiguration.Routes[0].Key : _routeA.Routes[0].Key));
            fc.Routes.ShouldContain(x => x.Key == (useCombinedConfig ? _combinedFileConfiguration.Routes[1].Key : _routeB.Routes[0].Key));
            fc.Routes.ShouldContain(x => x.Key == (useCombinedConfig ? _combinedFileConfiguration.Routes[2].Key : _routeB.Routes[1].Key));

            fc.Routes.ShouldContain(x => x.UpstreamHost == (useCombinedConfig ? _combinedFileConfiguration.Routes[0].UpstreamHost : _routeA.Routes[0].UpstreamHost));
            fc.Routes.ShouldContain(x => x.UpstreamHost == (useCombinedConfig ? _combinedFileConfiguration.Routes[1].UpstreamHost : _routeB.Routes[0].UpstreamHost));
            fc.Routes.ShouldContain(x => x.UpstreamHost == (useCombinedConfig ? _combinedFileConfiguration.Routes[2].UpstreamHost : _routeB.Routes[1].UpstreamHost));

            fc.Aggregates.Count.ShouldBe(useCombinedConfig ? _combinedFileConfiguration.Aggregates.Count :_aggregate.Aggregates.Count);
        }

        private void NotContainsEnvSpecificConfig()
        {
            var fc = (FileConfiguration)_configRoot.Get(typeof(FileConfiguration));
            fc.Routes.ShouldNotContain(x => x.DownstreamScheme == _envSpecific.Routes[0].DownstreamScheme);
            fc.Routes.ShouldNotContain(x => x.DownstreamPathTemplate == _envSpecific.Routes[0].DownstreamPathTemplate);
            fc.Routes.ShouldNotContain(x => x.Key == _envSpecific.Routes[0].Key);
        }
    }
}
