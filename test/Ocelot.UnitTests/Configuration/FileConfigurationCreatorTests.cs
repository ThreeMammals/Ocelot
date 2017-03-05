using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
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
        private readonly Mock<ILogger<FileOcelotConfigurationCreator>> _logger;
        private readonly FileOcelotConfigurationCreator _ocelotConfigurationCreator;
        private readonly Mock<ILoadBalancerFactory> _loadBalancerFactory;
        private readonly Mock<ILoadBalancerHouse> _loadBalancerHouse;
        private readonly Mock<ILoadBalancer> _loadBalancer;
        private readonly Mock<IQoSProviderFactory> _qosProviderFactory;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly Mock<IQoSProvider> _qosProvider;
        private Mock<IClaimsToThingCreator> _claimsToThingCreator;
        private Mock<IAuthenticationOptionsCreator> _authOptionsCreator;
        private Mock<IUpstreamTemplatePatternCreator> _upstreamTemplatePatternCreator;
        private Mock<IRequestIdKeyCreator> _requestIdKeyCreator;
        private Mock<IServiceProviderConfigurationCreator> _serviceProviderConfigCreator;
        private Mock<IQoSOptionsCreator> _qosOptionsCreator;
        private Mock<IReRouteOptionsCreator> _fileReRouteOptionsCreator;
        private Mock<IRateLimitOptionsCreator> _rateLimitOptions;

        public FileConfigurationCreatorTests()
        {
            _qosProviderFactory = new Mock<IQoSProviderFactory>();
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _qosProvider = new Mock<IQoSProvider>();
            _logger = new Mock<ILogger<FileOcelotConfigurationCreator>>();
            _validator = new Mock<IConfigurationValidator>();
            _fileConfig = new Mock<IOptions<FileConfiguration>>();
            _loadBalancerFactory = new Mock<ILoadBalancerFactory>();
            _loadBalancerHouse = new Mock<ILoadBalancerHouse>();
            _loadBalancer = new Mock<ILoadBalancer>();
            _claimsToThingCreator = new Mock<IClaimsToThingCreator>();
            _authOptionsCreator = new Mock<IAuthenticationOptionsCreator>();
            _upstreamTemplatePatternCreator = new Mock<IUpstreamTemplatePatternCreator>();
            _requestIdKeyCreator = new Mock<IRequestIdKeyCreator>();
            _serviceProviderConfigCreator = new Mock<IServiceProviderConfigurationCreator>();
            _qosOptionsCreator = new Mock<IQoSOptionsCreator>();
            _fileReRouteOptionsCreator = new Mock<IReRouteOptionsCreator>();
            _rateLimitOptions = new Mock<IRateLimitOptionsCreator>();

            _ocelotConfigurationCreator = new FileOcelotConfigurationCreator( 
                _fileConfig.Object, _validator.Object, _logger.Object,
                _loadBalancerFactory.Object, _loadBalancerHouse.Object, 
                _qosProviderFactory.Object, _qosProviderHouse.Object, _claimsToThingCreator.Object,
                _authOptionsCreator.Object, _upstreamTemplatePatternCreator.Object, _requestIdKeyCreator.Object,
                _serviceProviderConfigCreator.Object, _qosOptionsCreator.Object, _fileReRouteOptionsCreator.Object,
                _rateLimitOptions.Object);
        }

        [Fact]
        public void should_call_rate_limit_options_creator()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

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
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheRateLimitOptionsCreatorIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_call_qos_options_creator()
        {
            var expected = new QoSOptionsBuilder()
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .WithTimeoutValue(1)
                .Build();

            var serviceOptions = new ReRouteOptionsBuilder()
                .WithIsQos(true)
                .Build();

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
                .And(x => x.GivenTheFollowingOptionsAreReturned(serviceOptions))
                .And(x => x.GivenTheQosProviderFactoryReturns())
                .And(x => x.GivenTheQosOptionsCreatorReturns(expected))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheQosOptionsAre(expected))
                .And(x => x.TheQosProviderFactoryIsCalledCorrectly())
                .And(x => x.ThenTheQosProviderHouseIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_create_load_balancer()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

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
                                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                                .And(x => x.GivenTheLoadBalancerFactoryReturns())
                                .When(x => x.WhenICreateTheConfig())
                                .Then(x => x.TheLoadBalancerFactoryIsCalledCorrectly())
                                .And(x => x.ThenTheLoadBalancerHouseIsCalledCorrectly())
                    .BDDfy();
        }

        [Fact]
        public void should_use_downstream_host()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

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
                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamHost("127.0.0.1")
                                    .WithDownstreamPathTemplate("/products/{productId}")
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod("Get")
                                    .Build()
                            }))
                .BDDfy();
        }

        [Fact]
        public void should_use_downstream_scheme()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

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
                                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                                            .When(x => x.WhenICreateTheConfig())
                                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                                            {
                                                new ReRouteBuilder()
                                                    .WithDownstreamScheme("https")
                                                    .WithDownstreamPathTemplate("/products/{productId}")
                                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                                    .WithUpstreamHttpMethod("Get")
                                                    .Build()
                                            }))
                                .BDDfy();
        }

        [Fact]
        public void should_use_service_discovery_for_downstream_service_host()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

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
                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamPathTemplate("/products/{productId}")
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod("Get")
                                    .WithServiceProviderConfiguraion(new ServiceProviderConfigurationBuilder()
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
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();
                
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
                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamPathTemplate("/products/{productId}")
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod("Get")
                                    .WithServiceProviderConfiguraion(new ServiceProviderConfigurationBuilder()
                                        .WithUseServiceDiscovery(false)
                                        .Build())
                                    .Build()
                            }))
                            .BDDfy();
        }

        [Fact]
        public void should_call_template_pattern_creator_correctly()
        {
             var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();
                
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
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheUpstreamTemplatePatternCreatorReturns("(?i)/api/products/.*/$"))
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
        public void should_call_request_id_creator()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

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
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheRequestIdCreatorReturns("blahhhh"))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamPathTemplate("/products/{productId}")
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod("Get")
                        .WithRequestIdKey("blahhhh")
                        .Build()
                }))
                .And(x => x.ThenTheRequestIdKeyCreatorIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_create_with_headers_to_extract()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(true)
                .Build();

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
                .And(x => x.GivenTheAuthOptionsCreatorReturns(authenticationOptions))
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheClaimsToThingCreatorReturns(new List<ClaimToThing>{new ClaimToThing("CustomerId", "CustomerId", "", 0)}))
                .And(x => x.GivenTheLoadBalancerFactoryReturns())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .And(x => x.ThenTheAuthOptionsCreatorIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_create_with_authentication_properties()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(true)
                .Build();

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
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheAuthOptionsCreatorReturns(authenticationOptions))
                .And(x => x.GivenTheLoadBalancerFactoryReturns())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .And(x => x.ThenTheAuthOptionsCreatorIsCalledCorrectly())
                .BDDfy();
        }

        private void GivenTheFollowingOptionsAreReturned(ReRouteOptions fileReRouteOptions)
        {
            _fileReRouteOptionsCreator
                .Setup(x => x.Create(It.IsAny<FileReRoute>()))
                .Returns(fileReRouteOptions);
        }

        private void ThenTheRateLimitOptionsCreatorIsCalledCorrectly()
        {
            _rateLimitOptions
                .Verify(x => x.Create(It.IsAny<FileReRoute>(), It.IsAny<FileGlobalConfiguration>(), It.IsAny<bool>()), Times.Once);
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
                result.ClaimsToClaims.Count.ShouldBe(expected.ClaimsToClaims.Count);
                result.ClaimsToHeaders.Count.ShouldBe(expected.ClaimsToHeaders.Count);
                result.ClaimsToQueries.Count.ShouldBe(expected.ClaimsToQueries.Count);
                result.RequestIdKey.ShouldBe(expected.RequestIdKey);
            
            }
        }

        private void ThenTheServiceConfigurationIs(ServiceProviderConfiguration expected)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i];
                result.ServiceProviderConfiguraion.DownstreamHost.ShouldBe(expected.DownstreamHost);
                result.ServiceProviderConfiguraion.DownstreamPort.ShouldBe(expected.DownstreamPort);
                result.ServiceProviderConfiguraion.ServiceDiscoveryProvider.ShouldBe(expected.ServiceDiscoveryProvider);
                result.ServiceProviderConfiguraion.ServiceName.ShouldBe(expected.ServiceName);
                result.ServiceProviderConfiguraion.ServiceProviderHost.ShouldBe(expected.ServiceProviderHost);
                result.ServiceProviderConfiguraion.ServiceProviderPort.ShouldBe(expected.ServiceProviderPort);
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

        private void GivenTheClaimsToThingCreatorReturns(List<ClaimToThing> claimsToThing)
        {
            _claimsToThingCreator
                .Setup(x => x.Create(_fileConfiguration.ReRoutes[0].AddHeadersToRequest))
                .Returns(claimsToThing);
        }

        private void GivenTheAuthOptionsCreatorReturns(AuthenticationOptions authOptions)
        {
            _authOptionsCreator
                .Setup(x => x.Create(It.IsAny<FileReRoute>()))
                .Returns(authOptions);
        }

        private void ThenTheAuthOptionsCreatorIsCalledCorrectly()
        {
            _authOptionsCreator
                .Verify(x => x.Create(_fileConfiguration.ReRoutes[0]), Times.Once);
        }

        private void GivenTheUpstreamTemplatePatternCreatorReturns(string pattern)
        {
            _upstreamTemplatePatternCreator
                .Setup(x => x.Create(It.IsAny<FileReRoute>()))
                .Returns(pattern);
        }

        private void ThenTheRequestIdKeyCreatorIsCalledCorrectly()
        {
            _requestIdKeyCreator
                .Verify(x => x.Create(_fileConfiguration.ReRoutes[0], _fileConfiguration.GlobalConfiguration), Times.Once);
        }

        private void GivenTheRequestIdCreatorReturns(string requestId)
        {
            _requestIdKeyCreator
                .Setup(x => x.Create(It.IsAny<FileReRoute>(), It.IsAny<FileGlobalConfiguration>()))
                .Returns(requestId);
        }

        private void GivenTheQosOptionsCreatorReturns(QoSOptions qosOptions)
        {
            _qosOptionsCreator
                .Setup(x => x.Create(_fileConfiguration.ReRoutes[0]))
                .Returns(qosOptions);
        }

        private void ThenTheQosOptionsAre(QoSOptions qosOptions)
        {
            _config.Data.ReRoutes[0].QosOptions.DurationOfBreak.ShouldBe(qosOptions.DurationOfBreak);

            _config.Data.ReRoutes[0].QosOptions.ExceptionsAllowedBeforeBreaking.ShouldBe(qosOptions.ExceptionsAllowedBeforeBreaking);
            _config.Data.ReRoutes[0].QosOptions.TimeoutValue.ShouldBe(qosOptions.TimeoutValue);
        }
    }
}
