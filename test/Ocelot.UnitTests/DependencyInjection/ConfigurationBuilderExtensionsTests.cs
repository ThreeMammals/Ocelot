using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DependencyInjection
{
    public class ConfigurationBuilderExtensionsTests
    {
        private IConfigurationRoot _configuration;
        private string _result;
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

            // Clean up config files before each test
            var subConfigFiles = new DirectoryInfo(".").GetFiles("ocelot.*.json");

            foreach (var config in subConfigFiles)
            {
                config.Delete();
            }
        }

        [Fact]
        public void should_add_base_url_to_config()
        {
            this.Given(_ => GivenTheBaseUrl("test"))
                .When(_ => WhenIGet("BaseUrl"))
                .Then(_ => ThenTheResultIs("test"))
                .BDDfy();
        }

        [Fact]
        public void should_merge_files()
        {
            this.Given(_ => GivenMultipleConfigurationFiles(string.Empty, false))
                .And(_ => GivenTheEnvironmentIs(null))
                .When(_ => WhenIAddOcelotConfiguration())
                .Then(_ => ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false))
                .BDDfy();
        }

        [Fact]
        public void AddOcelot_WhenProvidedFileConfigurationObject_ShouldStoreGivenConfigurations()
        {
            this.Given(_ => GivenCombinedFileConfigurationObject(string.Empty))
                .And(_ => GivenTheEnvironmentIs(null))
                .When(_ => WhenIAddOcelotConfigurationWithCombinedFileConfiguration())
                .Then(_ => ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(true))
                .BDDfy();
        }

        [Fact]
        public void should_merge_files_except_env()
        {
            this.Given(_ => GivenMultipleConfigurationFiles(string.Empty, true))
                .And(_ => GivenTheEnvironmentIs("Env"))
                .When(_ => WhenIAddOcelotConfiguration())
                .Then(_ => ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false))
                .And(_ => NotContainsEnvSpecificConfig())
                .BDDfy();
        }

        [Fact]
        public void should_merge_files_in_specific_folder()
        {
            var configFolder = "ConfigFiles";
            this.Given(_ => GivenMultipleConfigurationFiles(configFolder, false))
                .When(_ => WhenIAddOcelotConfigurationWithSpecificFolder(configFolder))
                .Then(_ => ThenTheConfigsAreMergedAndAddedInApplicationConfiguration(false))
                .BDDfy();
        }

        private void GivenCombinedFileConfigurationObject(string folder)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _combinedFileConfiguration = new FileConfiguration
            {
                GlobalConfiguration = GetFileGlobalConfigurationData(),
                Routes = GetServiceARoutes().Concat(GetServiceBRoutes()).Concat(GetEnvironmentSpecificRoutes()).ToList(),
                Aggregates = GetFileAggregatesRouteData(),
            };
        }

        private void GivenMultipleConfigurationFiles(string folder, bool addEnvSpecificConfig)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _globalConfig = new FileConfiguration
            {
                GlobalConfiguration = GetFileGlobalConfigurationData(),
            };

            _routeA = new FileConfiguration
            {
                Routes = GetServiceARoutes(),
            };

            _routeB = new FileConfiguration
            {
                Routes = GetServiceBRoutes(),
            };

            _aggregate = new FileConfiguration
            {
                Aggregates = GetFileAggregatesRouteData(),
            };

            _envSpecific = new FileConfiguration
            {
                Routes = GetEnvironmentSpecificRoutes(),
            };

            var globalFilename = Path.Combine(folder, "ocelot.global.json");
            var routesAFilename = Path.Combine(folder, "ocelot.routesA.json");
            var routesBFilename = Path.Combine(folder, "ocelot.routesB.json");
            var aggregatesFilename = Path.Combine(folder, "ocelot.aggregates.json");

            File.WriteAllText(globalFilename, JsonConvert.SerializeObject(_globalConfig));
            File.WriteAllText(routesAFilename, JsonConvert.SerializeObject(_routeA));
            File.WriteAllText(routesBFilename, JsonConvert.SerializeObject(_routeB));
            File.WriteAllText(aggregatesFilename, JsonConvert.SerializeObject(_aggregate));

            if (addEnvSpecificConfig)
            {
                var envSpecificFilename = Path.Combine(folder, "ocelot.Env.json");
                File.WriteAllText(envSpecificFilename, JsonConvert.SerializeObject(_envSpecific));
            }
        }

        private static FileGlobalConfiguration GetFileGlobalConfigurationData()
        {
            return new FileGlobalConfiguration
            {
                BaseUrl = "BaseUrl",
                RateLimitOptions = new FileRateLimitOptions
                {
                    HttpStatusCode = 500,
                    ClientIdHeader = "ClientIdHeader",
                    DisableRateLimitHeaders = true,
                    QuotaExceededMessage = "QuotaExceededMessage",
                    RateLimitCounterPrefix = "RateLimitCounterPrefix",
                },
                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                {
                    Scheme = "https",
                    Host = "Host",
                    Port = 80,
                    Type = "Type",
                },
                RequestIdKey = "RequestIdKey",
            };
        }

        private static List<FileAggregateRoute> GetFileAggregatesRouteData()
        {
            return new List<FileAggregateRoute>
                {
                    new()
                    {
                        RouteKeys = new List<string>
                        {
                            "KeyB",
                            "KeyBB",
                        },
                        UpstreamPathTemplate = "UpstreamPathTemplate",
                    },
                    new()
                    {
                        RouteKeys = new List<string>
                        {
                            "KeyB",
                            "KeyBB",
                        },
                        UpstreamPathTemplate = "UpstreamPathTemplate",
                    },
                };
        }

        private static List<FileRoute> GetServiceARoutes()
        {
            return new List<FileRoute>
                {
                    new()
                    {
                        DownstreamScheme = "DownstreamScheme",
                        DownstreamPathTemplate = "DownstreamPathTemplate",
                        Key = "Key",
                        UpstreamHost = "UpstreamHost",
                        UpstreamHttpMethod = new List<string>
                        {
                            "UpstreamHttpMethod",
                        },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "Host",
                                Port = 80,
                            },
                        },
                    },
                };
        }

        private static List<FileRoute> GetServiceBRoutes()
        {
            return new List<FileRoute>
                {
                    new ()
                    {
                        DownstreamScheme = "DownstreamSchemeB",
                        DownstreamPathTemplate = "DownstreamPathTemplateB",
                        Key = "KeyB",
                        UpstreamHost = "UpstreamHostB",
                        UpstreamHttpMethod = new List<string>
                        {
                            "UpstreamHttpMethodB",
                        },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new ()
                            {
                                Host = "HostB",
                                Port = 80,
                            },
                        },
                    },
                    new()
                    {
                        DownstreamScheme = "DownstreamSchemeBB",
                        DownstreamPathTemplate = "DownstreamPathTemplateBB",
                        Key = "KeyBB",
                        UpstreamHost = "UpstreamHostBB",
                        UpstreamHttpMethod = new List<string>
                        {
                            "UpstreamHttpMethodBB",
                        },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "HostBB",
                                Port = 80,
                            },
                        },
                    },
                };
        }

        private static List<FileRoute> GetEnvironmentSpecificRoutes()
        {
            return new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamScheme = "DownstreamSchemeSpec",
                            DownstreamPathTemplate = "DownstreamPathTemplateSpec",
                            Key = "KeySpec",
                            UpstreamHost = "UpstreamHostSpec",
                            UpstreamHttpMethod = new List<string>
                            {
                                "UpstreamHttpMethodSpec",
                            },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "HostSpec",
                                    Port = 80,
                                },
                            },
                        },
                    };
        }

        private void GivenTheEnvironmentIs(string env)
        {
            _hostingEnvironment.SetupGet(x => x.EnvironmentName).Returns(env);
        }

        private void WhenIAddOcelotConfiguration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();

            builder.AddOcelot(_hostingEnvironment.Object);

            _configRoot = builder.Build();
        }

        private void WhenIAddOcelotConfigurationWithCombinedFileConfiguration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();

            builder.AddOcelot(_combinedFileConfiguration);

            _configRoot = builder.Build();
        }

        private void WhenIAddOcelotConfigurationWithSpecificFolder(string folder)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddOcelot(folder, _hostingEnvironment.Object);
            _configRoot = builder.Build();
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

        private void GivenTheBaseUrl(string baseUrl)
        {
#pragma warning disable CS0618
            var builder = new ConfigurationBuilder()
                .AddOcelotBaseUrl(baseUrl);
#pragma warning restore CS0618
            _configuration = builder.Build();
        }

        private void WhenIGet(string key)
        {
            _result = _configuration.GetValue(key, string.Empty);
        }

        private void ThenTheResultIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
