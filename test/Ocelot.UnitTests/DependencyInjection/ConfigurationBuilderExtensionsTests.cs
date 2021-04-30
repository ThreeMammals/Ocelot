﻿namespace Ocelot.UnitTests.DependencyInjection
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Newtonsoft.Json;
    using Ocelot.Configuration.File;
    using Ocelot.DependencyInjection;
    using Shouldly;
    using System.Collections.Generic;
    using System.IO;
    using TestStack.BDDfy;
    using Xunit;

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
        private Mock<IWebHostEnvironment> _hostingEnvironment;

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
            this.Given(_ => GivenMultipleConfigurationFiles("", false))
                .And(_ => GivenTheEnvironmentIs(null))
                .When(_ => WhenIAddOcelotConfiguration())
                .Then(_ => ThenTheConfigsAreMerged())
                .BDDfy();
        }

        [Fact]
        public void should_merge_files_except_env()
        {
            this.Given(_ => GivenMultipleConfigurationFiles("", true))
                .And(_ => GivenTheEnvironmentIs("Env"))
                .When(_ => WhenIAddOcelotConfiguration())
                .Then(_ => ThenTheConfigsAreMerged())
                .And(_ => NotContainsEnvSpecificConfig())
                .BDDfy();
        }

        [Fact]
        public void should_merge_files_in_specific_folder()
        {
            string configFolder = "ConfigFiles";
            this.Given(_ => GivenMultipleConfigurationFiles(configFolder, false))
                .When(_ => WhenIAddOcelotConfigurationWithSpecificFolder(configFolder))
                .Then(_ => ThenTheConfigsAreMerged())
                .BDDfy();
        }

        private void GivenMultipleConfigurationFiles(string folder, bool addEnvSpecificConfig)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _globalConfig = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    BaseUrl = "BaseUrl",
                    RateLimitOptions = new FileRateLimitOptions
                    {
                        HttpStatusCode = 500,
                        ClientIdHeader = "ClientIdHeader",
                        DisableRateLimitHeaders = true,
                        QuotaExceededMessage = "QuotaExceededMessage",
                        RateLimitCounterPrefix = "RateLimitCounterPrefix"
                    },
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "Host",
                        Port = 80,
                        Type = "Type"
                    },
                    RequestIdKey = "RequestIdKey"
                }
            };

            _routeA = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        DownstreamScheme = "DownstreamScheme",
                        DownstreamPathTemplate = "DownstreamPathTemplate",
                        Key = "Key",
                        UpstreamHost = "UpstreamHost",
                        UpstreamHttpMethod = new List<string>
                        {
                            "UpstreamHttpMethod"
                        },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "Host",
                                Port = 80
                            }
                        }
                    }
                }
            };

            _routeB = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        DownstreamScheme = "DownstreamSchemeB",
                        DownstreamPathTemplate = "DownstreamPathTemplateB",
                        Key = "KeyB",
                        UpstreamHost = "UpstreamHostB",
                        UpstreamHttpMethod = new List<string>
                        {
                            "UpstreamHttpMethodB"
                        },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "HostB",
                                Port = 80
                            }
                        }
                    },
                    new FileRoute
                    {
                        DownstreamScheme = "DownstreamSchemeBB",
                        DownstreamPathTemplate = "DownstreamPathTemplateBB",
                        Key = "KeyBB",
                        UpstreamHost = "UpstreamHostBB",
                        UpstreamHttpMethod = new List<string>
                        {
                            "UpstreamHttpMethodBB"
                        },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "HostBB",
                                Port = 80
                            }
                        }
                    }
                }
            };

            _aggregate = new FileConfiguration
            {
                Aggregates = new List<FileAggregateRoute>
                {
                    new FileAggregateRoute
                    {
                        RouteKeys = new List<string>
                        {
                            "KeyB",
                            "KeyBB"
                        },
                        UpstreamPathTemplate = "UpstreamPathTemplate",
                    },
                    new FileAggregateRoute
                    {
                        RouteKeys = new List<string>
                        {
                            "KeyB",
                            "KeyBB"
                        },
                        UpstreamPathTemplate = "UpstreamPathTemplate",
                    }
                }
            };

            _envSpecific = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new FileRoute
                        {
                            DownstreamScheme = "DownstreamSchemeSpec",
                            DownstreamPathTemplate = "DownstreamPathTemplateSpec",
                            Key = "KeySpec",
                            UpstreamHost = "UpstreamHostSpec",
                            UpstreamHttpMethod = new List<string>
                            {
                                "UpstreamHttpMethodSpec"
                            },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "HostSpec",
                                    Port = 80
                                }
                            }
                        }
                    }
            };

            string globalFilename = Path.Combine(folder, "ocelot.global.json");
            string routesAFilename = Path.Combine(folder, "ocelot.routesA.json");
            string routesBFilename = Path.Combine(folder, "ocelot.routesB.json");
            string aggregatesFilename = Path.Combine(folder, "ocelot.aggregates.json");

            File.WriteAllText(globalFilename, JsonConvert.SerializeObject(_globalConfig));
            File.WriteAllText(routesAFilename, JsonConvert.SerializeObject(_routeA));
            File.WriteAllText(routesBFilename, JsonConvert.SerializeObject(_routeB));
            File.WriteAllText(aggregatesFilename, JsonConvert.SerializeObject(_aggregate));

            if (addEnvSpecificConfig)
            {
                string envSpecificFilename = Path.Combine(folder, "ocelot.Env.json");
                File.WriteAllText(envSpecificFilename, JsonConvert.SerializeObject(_envSpecific));
            }
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

        private void WhenIAddOcelotConfigurationWithSpecificFolder(string folder)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddOcelot(folder, _hostingEnvironment.Object);
            _configRoot = builder.Build();
        }

        private void ThenTheConfigsAreMerged()
        {
            var fc = (FileConfiguration)_configRoot.Get(typeof(FileConfiguration));

            fc.GlobalConfiguration.BaseUrl.ShouldBe(_globalConfig.GlobalConfiguration.BaseUrl);
            fc.GlobalConfiguration.RateLimitOptions.ClientIdHeader.ShouldBe(_globalConfig.GlobalConfiguration.RateLimitOptions.ClientIdHeader);
            fc.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders.ShouldBe(_globalConfig.GlobalConfiguration.RateLimitOptions.DisableRateLimitHeaders);
            fc.GlobalConfiguration.RateLimitOptions.HttpStatusCode.ShouldBe(_globalConfig.GlobalConfiguration.RateLimitOptions.HttpStatusCode);
            fc.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage.ShouldBe(_globalConfig.GlobalConfiguration.RateLimitOptions.QuotaExceededMessage);
            fc.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix.ShouldBe(_globalConfig.GlobalConfiguration.RateLimitOptions.RateLimitCounterPrefix);
            fc.GlobalConfiguration.RequestIdKey.ShouldBe(_globalConfig.GlobalConfiguration.RequestIdKey);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Scheme.ShouldBe(_globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Scheme);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Host.ShouldBe(_globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Host);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Port.ShouldBe(_globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Port);
            fc.GlobalConfiguration.ServiceDiscoveryProvider.Type.ShouldBe(_globalConfig.GlobalConfiguration.ServiceDiscoveryProvider.Type);

            fc.Routes.Count.ShouldBe(_routeA.Routes.Count + _routeB.Routes.Count);

            fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == _routeA.Routes[0].DownstreamPathTemplate);
            fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == _routeB.Routes[0].DownstreamPathTemplate);
            fc.Routes.ShouldContain(x => x.DownstreamPathTemplate == _routeB.Routes[1].DownstreamPathTemplate);

            fc.Routes.ShouldContain(x => x.DownstreamScheme == _routeA.Routes[0].DownstreamScheme);
            fc.Routes.ShouldContain(x => x.DownstreamScheme == _routeB.Routes[0].DownstreamScheme);
            fc.Routes.ShouldContain(x => x.DownstreamScheme == _routeB.Routes[1].DownstreamScheme);

            fc.Routes.ShouldContain(x => x.Key == _routeA.Routes[0].Key);
            fc.Routes.ShouldContain(x => x.Key == _routeB.Routes[0].Key);
            fc.Routes.ShouldContain(x => x.Key == _routeB.Routes[1].Key);

            fc.Routes.ShouldContain(x => x.UpstreamHost == _routeA.Routes[0].UpstreamHost);
            fc.Routes.ShouldContain(x => x.UpstreamHost == _routeB.Routes[0].UpstreamHost);
            fc.Routes.ShouldContain(x => x.UpstreamHost == _routeB.Routes[1].UpstreamHost);

            fc.Aggregates.Count.ShouldBe(_aggregate.Aggregates.Count);
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
            _result = _configuration.GetValue(key, "");
        }

        private void ThenTheResultIs(string expected)
        {
            _result.ShouldBe(expected);
        }
    }
}
