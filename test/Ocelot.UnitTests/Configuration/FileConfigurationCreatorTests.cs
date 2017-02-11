using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Validator;
using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    public class FileConfigurationCreatorTests
    {
        private readonly Mock<IOptions<FileConfiguration>> _fileConfig;
        private readonly Mock<IConfigurationValidator> _validator;
        private Response<IOcelotConfiguration> _config;
        private FileConfiguration _fileConfiguration;
        private readonly Mock<IClaimToThingConfigurationParser> _configParser;
        private readonly Mock<ILogger<FileOcelotConfigurationCreator>> _logger;
        private readonly FileOcelotConfigurationCreator _ocelotConfigurationCreator;
        private readonly Mock<ILoadBalancerFactory> _loadBalancerFactory;
        private readonly Mock<ILoadBalancerHouse> _loadBalancerHouse;
        private readonly Mock<ILoadBalancer> _loadBalancer;
        private readonly Mock<IQoSProviderFactory> _qosProviderFactory;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly Mock<IQoSProvider> _qosProvider;

        public FileConfigurationCreatorTests()
        {
            _qosProviderFactory = new Mock<IQoSProviderFactory>();
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _qosProvider = new Mock<IQoSProvider>();
            _logger = new Mock<ILogger<FileOcelotConfigurationCreator>>();
            _configParser = new Mock<IClaimToThingConfigurationParser>();
            _validator = new Mock<IConfigurationValidator>();
            _fileConfig = new Mock<IOptions<FileConfiguration>>();
            _loadBalancerFactory = new Mock<ILoadBalancerFactory>();
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            _loadBalancer = new Mock<ILoadBalancer>();
            _ocelotConfigurationCreator = new FileOcelotConfigurationCreator( 
                _fileConfig.Object, _validator.Object, _configParser.Object, _logger.Object,
                _loadBalancerFactory.Object, _loadBalancerHouse.Object, 
                _qosProviderFactory.Object, _qosProviderHouse.Object);
        }

        [Fact]
        public void should_create_load_balancer()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
                            {
                                ReRoutes = new List<FileReRoute>
                                {
                                    new FileReRoute
                                    {
                                        DownstreamHost = "127.0.0.1",
                                        UpstreamPathTemplate = "/api/products/{productId}",
                                        DownstreamPathTemplate = "/products/{productId}",
                                        UpstreamHttpMethod = "Get",
                                    }
                                },
                            }))
                                .And(x => x.GivenTheConfigIsValid())
                                .And(x => x.GivenTheLoadBalancerFactoryReturns())
                                .When(x => x.WhenICreateTheConfig())
                                .Then(x => x.TheLoadBalancerFactoryIsCalledCorrectly())
                                .And(x => x.ThenTheLoadBalancerHouseIsCalledCorrectly())
                    .BDDfy();
        }

        [Fact]
        public void should_create_qos_provider()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamHost = "127.0.0.1",
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        QoSOptions = new FileQoSOptions
                        {
                            TimeoutValue = 1,
                            DurationOfBreak = 1,
                            ExceptionsAllowedBeforeBreaking = 1
                        }
                    }
                },
            }))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheQosProviderFactoryReturns())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.TheQosProviderFactoryIsCalledCorrectly())
                .And(x => x.ThenTheQosProviderHouseIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_use_downstream_host()
        {
                this.Given(x => x.GivenTheConfigIs(new FileConfiguration
                            {
                                ReRoutes = new List<FileReRoute>
                                {
                                    new FileReRoute
                                    {
                                        DownstreamHost = "127.0.0.1",
                                        UpstreamPathTemplate = "/api/products/{productId}",
                                        DownstreamPathTemplate = "/products/{productId}",
                                        UpstreamHttpMethod = "Get",
                                    }
                                },
                            }))
                                .And(x => x.GivenTheConfigIsValid())
                                .When(x => x.WhenICreateTheConfig())
                                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                                {
                                    new ReRouteBuilder()
                                        .WithDownstreamHost("127.0.0.1")
                                        .WithDownstreamPathTemplate("/products/{productId}")
                                        .WithUpstreamPathTemplate("/api/products/{productId}")
                                        .WithUpstreamHttpMethod("Get")
                                        .WithUpstreamTemplatePattern("(?i)/api/products/.*/$")
                                        .Build()
                                }))
                    .BDDfy();
        }

        [Fact]
        public void should_use_downstream_scheme()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
                                        {
                                            ReRoutes = new List<FileReRoute>
                                            {
                                                new FileReRoute
                                                {
                                                    DownstreamScheme = "https",
                                                    UpstreamPathTemplate = "/api/products/{productId}",
                                                    DownstreamPathTemplate = "/products/{productId}",
                                                    UpstreamHttpMethod = "Get",
                                                }
                                            },
                                        }))
                                            .And(x => x.GivenTheConfigIsValid())
                                            .When(x => x.WhenICreateTheConfig())
                                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                                            {
                                                new ReRouteBuilder()
                                                    .WithDownstreamScheme("https")
                                                    .WithDownstreamPathTemplate("/products/{productId}")
                                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                                    .WithUpstreamHttpMethod("Get")
                                                    .WithUpstreamTemplatePattern("(?i)/api/products/.*/$")
                                                    .Build()
                                            }))
                                .BDDfy();
        }

        [Fact]
        public void should_use_service_discovery_for_downstream_service_host()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
                        {
                            ReRoutes = new List<FileReRoute>
                            {
                                new FileReRoute
                                {
                                    UpstreamPathTemplate = "/api/products/{productId}",
                                    DownstreamPathTemplate = "/products/{productId}",
                                    UpstreamHttpMethod = "Get",
                                    ReRouteIsCaseSensitive = false,
                                    ServiceName = "ProductService"
                                }
                            },
                            GlobalConfiguration = new FileGlobalConfiguration
                            {
                                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                                {
                                     Provider = "consul",
                                     Host = "127.0.0.1"
                                }
                            }
                        }))
                            .And(x => x.GivenTheConfigIsValid())
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamPathTemplate("/products/{productId}")
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod("Get")
                                    .WithUpstreamTemplatePattern("(?i)/api/products/.*/$")
                                    .WithServiceProviderConfiguraion(new ServiceProviderConfiguraionBuilder()
                                        .WithUseServiceDiscovery(true)
                                        .WithServiceDiscoveryProvider("consul")
                                        .WithServiceDiscoveryProviderHost("127.0.0.1")
                                        .WithServiceName("ProductService")
                                        .Build())
                                    .Build()
                            }))
                            .BDDfy();
        }

         [Fact]
        public void should_not_use_service_discovery_for_downstream_host_url_when_no_service_name()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
                        {
                            ReRoutes = new List<FileReRoute>
                            {
                                new FileReRoute
                                {
                                    UpstreamPathTemplate = "/api/products/{productId}",
                                    DownstreamPathTemplate = "/products/{productId}",
                                    UpstreamHttpMethod = "Get",
                                    ReRouteIsCaseSensitive = false,
                                }
                            }
                        }))
                            .And(x => x.GivenTheConfigIsValid())
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamPathTemplate("/products/{productId}")
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod("Get")
                                    .WithUpstreamTemplatePattern("(?i)/api/products/.*/$")
                                    .WithServiceProviderConfiguraion(new ServiceProviderConfiguraionBuilder()
                                        .WithUseServiceDiscovery(false)
                                        .Build())
                                    .Build()
                            }))
                            .BDDfy();
        }

        [Fact]
        public void should_use_reroute_case_sensitivity_value()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = false
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("(?i)/api/products/.*/$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_set_upstream_template_pattern_to_ignore_case_sensitivity()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get"
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("(?i)/api/products/.*/$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_set_upstream_template_pattern_to_respect_case_sensitivity()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
              .And(x => x.GivenTheConfigIsValid())
              .When(x => x.WhenICreateTheConfig())
              .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
              {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*/$")
                        .Build()
              }))
              .BDDfy();
        }

        [Fact]
        public void should_set_global_request_id_key()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    RequestIdKey = "blahhhh"
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*/$")
                        .WithRequestIdKey("blahhhh")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_anything_to_end_of_string()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*/$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_with_headers_to_extract()
        {
            var authenticationOptions = new AuthenticationOptionsBuilder()
                    .WithProvider("IdentityServer")
                    .WithProviderRootUrl("http://localhost:51888")
                    .WithRequireHttps(false)
                    .WithScopeSecret("secret")
                    .WithScopeName("api")
                    .WithAdditionalScopes(new List<string>())
                    .Build();

            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("/products/{productId}")
                    .WithUpstreamPathTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod("Get")
                    .WithUpstreamTemplatePattern("/api/products/.*/$")
                    .WithAuthenticationOptions(authenticationOptions)
                    .WithClaimsToHeaders(new List<ClaimToThing>
                    {
                        new ClaimToThing("CustomerId", "CustomerId", "", 0),
                    })
                    .Build()
            };

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true,
                        AuthenticationOptions = new FileAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = "http://localhost:51888",
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            },
                        AddHeadersToRequest =
                        {
                            {"CustomerId", "Claims[CustomerId] > value"},
                        }
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheConfigHeaderExtractorReturns(new ClaimToThing("CustomerId", "CustomerId", "", 0)))
                .And(x => x.GivenTheLoadBalancerFactoryReturns())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .BDDfy();
        }

        private void GivenTheConfigHeaderExtractorReturns(ClaimToThing expected)
        {
            _configParser
                .Setup(x => x.Extract(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new OkResponse<ClaimToThing>(expected));
        }

        [Fact]
        public void should_create_with_authentication_properties()
        {
             var authenticationOptions = new AuthenticationOptionsBuilder()
                    .WithProvider("IdentityServer")
                    .WithProviderRootUrl("http://localhost:51888")
                    .WithRequireHttps(false)
                    .WithScopeSecret("secret")
                    .WithScopeName("api")
                    .WithAdditionalScopes(new List<string>())
                    .Build();

            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("/products/{productId}")
                    .WithUpstreamPathTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod("Get")
                    .WithUpstreamTemplatePattern("/api/products/.*/$")
                    .WithAuthenticationOptions(authenticationOptions)
                    .Build()
            };

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true,
                        AuthenticationOptions = new FileAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = "http://localhost:51888",
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            }
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheLoadBalancerFactoryReturns())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_more_than_one_placeholder()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}/variants/{variantId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*/variants/.*/$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_more_than_one_placeholder_with_trailing_slash()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/api/products/{productId}/variants/{variantId}/",
                        DownstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}/variants/{variantId}/")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("/api/products/.*/variants/.*/$")
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_create_template_pattern_that_matches_to_end_of_string()
        {
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        UpstreamPathTemplate = "/",
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamHttpMethod = "Get",
                        ReRouteIsCaseSensitive = true
                    }
                }
            }))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/api/products/")
                        .WithUpstreamPathTemplate("/")
                        .WithUpstreamHttpMethod("Get")
                        .WithUpstreamTemplatePattern("^/$")
                        .Build()
                }))
                .BDDfy();
        }

        private void GivenTheConfigIsValid()
        {
            _validator
                .Setup(x => x.IsValid(It.IsAny<FileConfiguration>()))
                .Returns(new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false)));
        }

        private void GivenTheConfigIs(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
            _fileConfig
                .Setup(x => x.Value)
                .Returns(_fileConfiguration);
        }

        private void WhenICreateTheConfig()
        {
            _config = _ocelotConfigurationCreator.Create().Result;
        }

        private void ThenTheReRoutesAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i];
                var expected = expectedReRoutes[i];

                result.DownstreamPathTemplate.Value.ShouldBe(expected.DownstreamPathTemplate.Value);
                result.UpstreamHttpMethod.ShouldBe(expected.UpstreamHttpMethod);
                result.UpstreamPathTemplate.Value.ShouldBe(expected.UpstreamPathTemplate.Value);
                result.UpstreamTemplatePattern.ShouldBe(expected.UpstreamTemplatePattern);
            }
        }

        private void ThenTheAuthenticationOptionsAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i].AuthenticationOptions;
                var expected = expectedReRoutes[i].AuthenticationOptions;

                result.AdditionalScopes.ShouldBe(expected.AdditionalScopes);
                result.Provider.ShouldBe(expected.Provider);
                result.ProviderRootUrl.ShouldBe(expected.ProviderRootUrl);
                result.RequireHttps.ShouldBe(expected.RequireHttps);
                result.ScopeName.ShouldBe(expected.ScopeName);
                result.ScopeSecret.ShouldBe(expected.ScopeSecret);

            }
        }

        private void GivenTheLoadBalancerFactoryReturns()
        {
            _loadBalancerFactory
                .Setup(x => x.Get(It.IsAny<ReRoute>()))
                .ReturnsAsync(_loadBalancer.Object);
        }

        private void TheLoadBalancerFactoryIsCalledCorrectly()
        {
            _loadBalancerFactory
                .Verify(x => x.Get(It.IsAny<ReRoute>()), Times.Once);
        }

        private void ThenTheLoadBalancerHouseIsCalledCorrectly()
        {
            _loadBalancerHouse
                .Verify(x => x.Add(It.IsAny<string>(), _loadBalancer.Object), Times.Once);
        }

        private void GivenTheQosProviderFactoryReturns()
        {
            _qosProviderFactory
                .Setup(x => x.Get(It.IsAny<ReRoute>()))
                .Returns(_qosProvider.Object);
        }

        private void TheQosProviderFactoryIsCalledCorrectly()
        {
            _qosProviderFactory
                .Verify(x => x.Get(It.IsAny<ReRoute>()), Times.Once);
        }

        private void ThenTheQosProviderHouseIsCalledCorrectly()
        {
            _qosProviderHouse
                .Verify(x => x.Add(It.IsAny<string>(), _qosProvider.Object), Times.Once);
        }
    }
}
