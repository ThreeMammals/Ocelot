using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.Logging;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Configuration
{
    using Ocelot.Errors;
    using Ocelot.UnitTests.TestData;

    public class FileConfigurationCreatorTests
    {
        private readonly Mock<IOptions<FileConfiguration>> _fileConfig;
        private readonly Mock<IConfigurationValidator> _validator;
        private Response<IOcelotConfiguration> _config;
        private FileConfiguration _fileConfiguration;
        private readonly Mock<IOcelotLoggerFactory> _logger;
        private readonly FileOcelotConfigurationCreator _ocelotConfigurationCreator;
        private Mock<IClaimsToThingCreator> _claimsToThingCreator;
        private Mock<IAuthenticationOptionsCreator> _authOptionsCreator;
        private Mock<IUpstreamTemplatePatternCreator> _upstreamTemplatePatternCreator;
        private Mock<IRequestIdKeyCreator> _requestIdKeyCreator;
        private Mock<IServiceProviderConfigurationCreator> _serviceProviderConfigCreator;
        private Mock<IQoSOptionsCreator> _qosOptionsCreator;
        private Mock<IReRouteOptionsCreator> _fileReRouteOptionsCreator;
        private Mock<IRateLimitOptionsCreator> _rateLimitOptions;
        private Mock<IRegionCreator> _regionCreator;
        private Mock<IHttpHandlerOptionsCreator> _httpHandlerOptionsCreator;

        public FileConfigurationCreatorTests()
        {
            _logger = new Mock<IOcelotLoggerFactory>();
            _validator = new Mock<IConfigurationValidator>();
            _fileConfig = new Mock<IOptions<FileConfiguration>>();
            _claimsToThingCreator = new Mock<IClaimsToThingCreator>();
            _authOptionsCreator = new Mock<IAuthenticationOptionsCreator>();
            _upstreamTemplatePatternCreator = new Mock<IUpstreamTemplatePatternCreator>();
            _requestIdKeyCreator = new Mock<IRequestIdKeyCreator>();
            _serviceProviderConfigCreator = new Mock<IServiceProviderConfigurationCreator>();
            _qosOptionsCreator = new Mock<IQoSOptionsCreator>();
            _fileReRouteOptionsCreator = new Mock<IReRouteOptionsCreator>();
            _rateLimitOptions = new Mock<IRateLimitOptionsCreator>();
            _regionCreator = new Mock<IRegionCreator>();
            _httpHandlerOptionsCreator = new Mock<IHttpHandlerOptionsCreator>();

            _ocelotConfigurationCreator = new FileOcelotConfigurationCreator( 
                _fileConfig.Object, _validator.Object, _logger.Object,
                _claimsToThingCreator.Object,
                _authOptionsCreator.Object, _upstreamTemplatePatternCreator.Object, _requestIdKeyCreator.Object,
                _serviceProviderConfigCreator.Object, _qosOptionsCreator.Object, _fileReRouteOptionsCreator.Object,
                _rateLimitOptions.Object, _regionCreator.Object, _httpHandlerOptionsCreator.Object);
        }

        [Fact]
        public void should_call_service_provider_config_creator()
        {
            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();
                
            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Host = "localhost",
                        Port = 8500,
                    }
                }
            }))
                .And(x => x.GivenTheFollowingIsReturned(serviceProviderConfig))
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheServiceProviderCreatorIsCalledCorrectly())
                .BDDfy();  
        }

        [Fact]
        public void should_call_region_creator()
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
                                    UpstreamHttpMethod = new List<string> { "Get" },
                                    FileCacheOptions = new FileCacheOptions
                                    {
                                        Region = "region"
                                    }
                                }
                            },
            }))
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheFollowingRegionIsReturned("region"))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheRegionCreatorIsCalledCorrectly("region"))
                .BDDfy();
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
                                    UpstreamHttpMethod = new List<string> { "Get" },
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
                        UpstreamHttpMethod = new List<string> { "Get" },
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
                .And(x => x.GivenTheQosOptionsCreatorReturns(expected))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheQosOptionsAre(expected))
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
                                    UpstreamHttpMethod = new List<string> { "Get" },
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
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
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
                                                    UpstreamHttpMethod = new List<string> { "Get" },
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
                                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
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
                                    UpstreamHttpMethod = new List<string> { "Get" },
                                    ReRouteIsCaseSensitive = false,
                                    ServiceName = "ProductService"
                                }
                            },
                            GlobalConfiguration = new FileGlobalConfiguration
                            {
                                ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                                {
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
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .WithUseServiceDiscovery(true)
                                    .WithServiceName("ProductService")
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
                                    UpstreamHttpMethod = new List<string> { "Get" },
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
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .WithUseServiceDiscovery(false)
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
                        UpstreamHttpMethod = new List<string> { "Get" },
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
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
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
                        UpstreamHttpMethod = new List<string> { "Get" },
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
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithRequestIdKey("blahhhh")
                        .Build()
                }))
                .And(x => x.ThenTheRequestIdKeyCreatorIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_call_httpHandler_creator()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();
            var httpHandlerOptions = new HttpHandlerOptions(true, true);

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                            {
                                new FileReRoute
                                {
                                    DownstreamHost = "127.0.0.1",
                                    UpstreamPathTemplate = "/api/products/{productId}",
                                    DownstreamPathTemplate = "/products/{productId}",
                                    UpstreamHttpMethod = new List<string> { "Get" }
                                }
                            },
            }))
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheFollowingHttpHandlerOptionsAreReturned(httpHandlerOptions))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheHttpHandlerOptionsCreatorIsCalledCorrectly())
                .BDDfy();
        }

        [Theory]
        [MemberData(nameof(AuthenticationConfigTestData.GetAuthenticationData), MemberType = typeof(AuthenticationConfigTestData))]
        public void should_create_with_headers_to_extract(FileConfiguration fileConfig)
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(true)
                .Build();

            var authenticationOptions = new AuthenticationOptionsBuilder()
                    .WithAllowedScopes(new List<string>())
                    .Build();

            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("/products/{productId}")
                    .WithUpstreamPathTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .WithAuthenticationOptions(authenticationOptions)
                    .WithClaimsToHeaders(new List<ClaimToThing>
                    {
                        new ClaimToThing("CustomerId", "CustomerId", "", 0),
                    })
                    .Build()
            };

            this.Given(x => x.GivenTheConfigIs(fileConfig))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheAuthOptionsCreatorReturns(authenticationOptions))
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheClaimsToThingCreatorReturns(new List<ClaimToThing> { new ClaimToThing("CustomerId", "CustomerId", "", 0) }))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .And(x => x.ThenTheAuthOptionsCreatorIsCalledCorrectly())
                .BDDfy();
        }

        [Theory]
        [MemberData(nameof(AuthenticationConfigTestData.GetAuthenticationData), MemberType = typeof(AuthenticationConfigTestData))]
        public void should_create_with_authentication_properties(FileConfiguration fileConfig)
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .WithIsAuthenticated(true)
                .Build();

            var authenticationOptions = new AuthenticationOptionsBuilder()
                   .WithAllowedScopes(new List<string>())
                   .Build();

            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamPathTemplate("/products/{productId}")
                    .WithUpstreamPathTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .WithAuthenticationOptions(authenticationOptions)
                    .Build()
            };

            this.Given(x => x.GivenTheConfigIs(fileConfig))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheAuthOptionsCreatorReturns(authenticationOptions))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .And(x => x.ThenTheAuthenticationOptionsAre(expected))
                .And(x => x.ThenTheAuthOptionsCreatorIsCalledCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_return_validation_errors()
        {
            var errors = new List<Error> {new PathTemplateDoesntStartWithForwardSlash("some message")};

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration()))
                .And(x => x.GivenTheConfigIsInvalid(errors))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheErrorsAreReturned(errors))
                .BDDfy();
        }

        private void GivenTheConfigIsInvalid(List<Error> errors)
        {
            _validator
                .Setup(x => x.IsValid(It.IsAny<FileConfiguration>()))
                .ReturnsAsync(new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(true, errors)));
        }

        private void ThenTheErrorsAreReturned(List<Error> errors)
        {
            _config.IsError.ShouldBeTrue();
            _config.Errors[0].ShouldBe(errors[0]);
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
                .ReturnsAsync(new OkResponse<ConfigurationValidationResult>(new ConfigurationValidationResult(false)));
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
            _config = _ocelotConfigurationCreator.Create(_fileConfiguration).Result;
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

        private void ThenTheAuthenticationOptionsAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i].AuthenticationOptions;
                var expected = expectedReRoutes[i].AuthenticationOptions;
                result.AllowedScopes.ShouldBe(expected.AllowedScopes);
            }
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
            _config.Data.ReRoutes[0].QosOptionsOptions.DurationOfBreak.ShouldBe(qosOptions.DurationOfBreak);

            _config.Data.ReRoutes[0].QosOptionsOptions.ExceptionsAllowedBeforeBreaking.ShouldBe(qosOptions.ExceptionsAllowedBeforeBreaking);
            _config.Data.ReRoutes[0].QosOptionsOptions.TimeoutValue.ShouldBe(qosOptions.TimeoutValue);
        }

        private void ThenTheServiceProviderCreatorIsCalledCorrectly()
        {
            _serviceProviderConfigCreator
                .Verify(x => x.Create(_fileConfiguration.GlobalConfiguration), Times.Once);
        }

        private void GivenTheFollowingIsReturned(ServiceProviderConfiguration serviceProviderConfiguration)
        {
            _serviceProviderConfigCreator
                .Setup(x => x.Create(It.IsAny<FileGlobalConfiguration>())).Returns(serviceProviderConfiguration);
        }

        
        private void GivenTheFollowingRegionIsReturned(string region)
        {
            _regionCreator
                .Setup(x => x.Create(It.IsAny<FileReRoute>()))
                .Returns(region);
        }

        private void ThenTheRegionCreatorIsCalledCorrectly(string expected)
        {
            _regionCreator
                .Verify(x => x.Create(_fileConfiguration.ReRoutes[0]), Times.Once);
        }
        
        private void GivenTheFollowingHttpHandlerOptionsAreReturned(HttpHandlerOptions httpHandlerOptions)
        {
            _httpHandlerOptionsCreator.Setup(x => x.Create(It.IsAny<FileReRoute>()))
                .Returns(httpHandlerOptions);
        }

        private void ThenTheHttpHandlerOptionsCreatorIsCalledCorrectly()
        {
            _httpHandlerOptionsCreator.Verify(x => x.Create(_fileConfiguration.ReRoutes[0]), Times.Once());
        }
    }
}
