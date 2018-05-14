namespace Ocelot.UnitTests.Configuration
{
    using System.Collections.Generic;
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
    using Ocelot.DependencyInjection;
    using Ocelot.Errors;
    using Ocelot.UnitTests.TestData;
    using Ocelot.Values;

    public class FileInternalConfigurationCreatorTests
    {
        private readonly Mock<IConfigurationValidator> _validator;
        private Response<IInternalConfiguration> _config;
        private FileConfiguration _fileConfiguration;
        private readonly Mock<IOcelotLoggerFactory> _logger;
        private readonly FileInternalConfigurationCreator _internalConfigurationCreator;
        private readonly Mock<IClaimsToThingCreator> _claimsToThingCreator;
        private readonly Mock<IAuthenticationOptionsCreator> _authOptionsCreator;
        private readonly Mock<IUpstreamTemplatePatternCreator> _upstreamTemplatePatternCreator;
        private readonly Mock<IRequestIdKeyCreator> _requestIdKeyCreator;
        private readonly Mock<IServiceProviderConfigurationCreator> _serviceProviderConfigCreator;
        private readonly Mock<IQoSOptionsCreator> _qosOptionsCreator;
        private readonly Mock<IReRouteOptionsCreator> _fileReRouteOptionsCreator;
        private readonly Mock<IRateLimitOptionsCreator> _rateLimitOptions;
        private readonly Mock<IRegionCreator> _regionCreator;
        private readonly Mock<IHttpHandlerOptionsCreator> _httpHandlerOptionsCreator;
        private readonly Mock<IAdministrationPath> _adminPath;
        private readonly Mock<IHeaderFindAndReplaceCreator> _headerFindAndReplaceCreator;
        private readonly Mock<IDownstreamAddressesCreator> _downstreamAddressesCreator;

        public FileInternalConfigurationCreatorTests()
        {
            _logger = new Mock<IOcelotLoggerFactory>();
            _validator = new Mock<IConfigurationValidator>();
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
            _adminPath = new Mock<IAdministrationPath>();
            _headerFindAndReplaceCreator = new Mock<IHeaderFindAndReplaceCreator>();
            _downstreamAddressesCreator = new Mock<IDownstreamAddressesCreator>();

            _internalConfigurationCreator = new FileInternalConfigurationCreator( 
                _validator.Object, 
                _logger.Object,
                _claimsToThingCreator.Object,
                _authOptionsCreator.Object, 
                _upstreamTemplatePatternCreator.Object,
                _requestIdKeyCreator.Object,
                _serviceProviderConfigCreator.Object,
                _qosOptionsCreator.Object,
                _fileReRouteOptionsCreator.Object,
                _rateLimitOptions.Object,
                _regionCreator.Object,
                _httpHandlerOptionsCreator.Object,
                _adminPath.Object,
                _headerFindAndReplaceCreator.Object,
                _downstreamAddressesCreator.Object);
        }

        [Fact]
        public void should_set_up_sticky_sessions_config()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamAddresses(new List<DownstreamHostAndPort>() { new DownstreamHostAndPort("127.0.0.1", 80) })
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithLoadBalancerKey("CookieStickySessions:sessionid")
                .Build();

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                            {
                                new FileReRoute
                                {
                                    DownstreamHostAndPorts = new List<FileHostAndPort>
                                    {
                                        new FileHostAndPort
                                        {
                                            Host = "127.0.0.1",
                                        }
                                    },
                                    LoadBalancerOptions = new FileLoadBalancerOptions
                                    {
                                        Expiry = 10,
                                        Key = "sessionid",
                                        Type = "CookieStickySessions"
                                    },
                                    UpstreamPathTemplate = "/api/products/{productId}",
                                    DownstreamPathTemplate = "/products/{productId}",
                                    UpstreamHttpMethod = new List<string> { "Get" },
                                }
                            },
            }))
                            .And(x => x.GivenTheConfigIsValid())
                            .And(x => GivenTheDownstreamAddresses())
                            .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamReRoute(downstreamReRoute)
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .Build()
                            }))
                .BDDfy();
        }

        [Fact]
        public void should_set_up_aggregate_re_route()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51878,
                                }
                            },
                            UpstreamPathTemplate = "/laura",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Laura",
                            UpstreamHost = "localhost"
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamScheme = "http",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "localhost",
                                    Port = 51880,
                                }
                            },
                            UpstreamPathTemplate = "/tom",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom",
                            UpstreamHost = "localhost",
                        }
                    },
                Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            },
                            Aggregator = "asdf"
                        }
                    }
            };

            var serviceProviderConfig = new ServiceProviderConfigurationBuilder().Build();

            var expected = new List<ReRoute>();

            var lauraDownstreamReRoute = new DownstreamReRouteBuilder()
                .WithUpstreamHost("localhost")
                .WithKey("Laura")
                .WithDownstreamPathTemplate("/")
                .WithDownstreamScheme("http")
                .WithUpstreamHttpMethod(new List<string>() {"Get"})
                .WithDownstreamAddresses(new List<DownstreamHostAndPort>() {new DownstreamHostAndPort("localhost", 51878)})
                .WithLoadBalancerKey("/laura|Get")
                .Build();

            var lauraReRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod(new List<string>() { "Get" })
                .WithUpstreamHost("localhost")
                .WithUpstreamPathTemplate("/laura")
                .WithDownstreamReRoute(lauraDownstreamReRoute)
                .Build();

            expected.Add(lauraReRoute);

            var tomDownstreamReRoute = new DownstreamReRouteBuilder()
                .WithUpstreamHost("localhost")
                .WithKey("Tom")
                .WithDownstreamPathTemplate("/")
                .WithDownstreamScheme("http")
                .WithUpstreamHttpMethod(new List<string>() { "Get" })
                .WithDownstreamAddresses(new List<DownstreamHostAndPort>() { new DownstreamHostAndPort("localhost", 51878) })
                .WithLoadBalancerKey("/tom|Get")
                .Build();

            var tomReRoute = new ReRouteBuilder()
                .WithUpstreamHttpMethod(new List<string>() { "Get" })
                .WithUpstreamHost("localhost")
                .WithUpstreamPathTemplate("/tom")
                .WithDownstreamReRoute(tomDownstreamReRoute)
                .Build();

            expected.Add(tomReRoute);

            var aggregateReReRoute = new ReRouteBuilder()
                .WithUpstreamPathTemplate("/")
                .WithUpstreamHost("localhost")
                .WithDownstreamReRoute(lauraDownstreamReRoute)
                .WithDownstreamReRoute(tomDownstreamReRoute)
                .WithUpstreamHttpMethod(new List<string>() { "Get" })
                .Build();

            expected.Add(aggregateReReRoute);

            this.Given(x => x.GivenTheConfigIs(configuration))
                .And(x => x.GivenTheFollowingOptionsAreReturned(new ReRouteOptionsBuilder().Build()))
                .And(x => x.GivenTheFollowingIsReturned(serviceProviderConfig))
                .And(x => GivenTheDownstreamAddresses())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                .And(x => x.GivenTheConfigIsValid())
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheServiceProviderCreatorIsCalledCorrectly())
                .Then(x => x.ThenTheReRoutesAre(expected))
                .BDDfy();
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
                .And(x => GivenTheDownstreamAddresses())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
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
                                    DownstreamHostAndPorts = new List<FileHostAndPort>
                                    {
                                        new FileHostAndPort
                                        {
                                            Host = "127.0.0.1",
                                        }
                                    },
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
                .And(x => GivenTheDownstreamAddresses())                
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                .And(x => x.GivenTheConfigIsValid())
                .And(x => x.GivenTheFollowingRegionIsReturned("region"))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheRegionCreatorIsCalledCorrectly())
                .And(x => x.ThenTheHeaderFindAndReplaceCreatorIsCalledCorrectly())
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
                                    DownstreamHostAndPorts = new List<FileHostAndPort>
                                    {
                                        new FileHostAndPort
                                        {
                                            Host = "127.0.0.1",
                                        }
                                    },
                                    UpstreamPathTemplate = "/api/products/{productId}",
                                    DownstreamPathTemplate = "/products/{productId}",
                                    UpstreamHttpMethod = new List<string> { "Get" },
                                }
                            },
            }))
                .And(x => x.GivenTheConfigIsValid())
                .And(x => GivenTheDownstreamAddresses())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
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
                .Build();

             this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "127.0.0.1",
                            }
                        },
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
                .And(x => GivenTheDownstreamAddresses())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
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

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamAddresses(new List<DownstreamHostAndPort>() {new DownstreamHostAndPort("127.0.0.1", 80)})
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithLoadBalancerKey("/api/products/{productId}|Get")
                .Build();

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
                        {
                            ReRoutes = new List<FileReRoute>
                            {
                                new FileReRoute
                                {
                                    DownstreamHostAndPorts = new List<FileHostAndPort>
                                    {
                                        new FileHostAndPort
                                        {
                                            Host = "127.0.0.1",
                                        }
                                    },
                                    UpstreamPathTemplate = "/api/products/{productId}",
                                    DownstreamPathTemplate = "/products/{productId}",
                                    UpstreamHttpMethod = new List<string> { "Get" },
                                }
                            },
                        }))
                            .And(x => x.GivenTheConfigIsValid())
                            .And(x => GivenTheDownstreamAddresses())
                            .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamReRoute(downstreamReRoute)
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

            var handlers = new List<string> {"Polly", "Tracer"};

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamScheme("https")
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithDelegatingHandlers(handlers)
                .WithLoadBalancerKey("/api/products/{productId}|Get")
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
                                                    DelegatingHandlers = handlers
                                                }
                                            },
                                        }))
                                            .And(x => x.GivenTheConfigIsValid())
                                            .And(x => GivenTheDownstreamAddresses())
                                            .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                                            .When(x => x.WhenICreateTheConfig())
                                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                                            {
                                                new ReRouteBuilder()
                                                    .WithDownstreamReRoute(downstreamReRoute)
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

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithUseServiceDiscovery(true)
                .WithServiceName("ProductService")
                .WithLoadBalancerKey("/api/products/{productId}|Get")
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
                            .And(x => GivenTheDownstreamAddresses())
                            .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamReRoute(downstreamReRoute)
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .Build()
                            }))
                            .BDDfy();
        }

         [Fact]
        public void should_not_use_service_discovery_for_downstream_host_url_when_no_service_name()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithUseServiceDiscovery(false)
                .WithLoadBalancerKey("/api/products/{productId}|Get")
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
                            .And(x => GivenTheDownstreamAddresses())
                            .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                            .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                            .When(x => x.WhenICreateTheConfig())
                            .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                            {
                                new ReRouteBuilder()
                                    .WithDownstreamReRoute(downstreamReRoute)
                                    .WithUpstreamPathTemplate("/api/products/{productId}")
                                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                                    .Build()
                            }))
                            .BDDfy();
        }

        [Fact]
        public void should_call_template_pattern_creator_correctly()
        {
             var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithUpstreamTemplatePattern(new UpstreamPathTemplate("(?i)/api/products/.*/$", 1))
                .WithLoadBalancerKey("/api/products/{productId}|Get")
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
                .And(x => GivenTheDownstreamAddresses())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheUpstreamTemplatePatternCreatorReturns("(?i)/api/products/.*/$"))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(downstreamReRoute)
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .WithUpstreamTemplatePattern(new UpstreamPathTemplate("(?i)/api/products/.*/$", 1))
                        .Build()
                }))
                .BDDfy();
        }

        [Fact]
        public void should_call_request_id_creator()
        {
            var reRouteOptions = new ReRouteOptionsBuilder()
                .Build();

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithRequestIdKey("blahhhh")
                .WithLoadBalancerKey("/api/products/{productId}|Get")
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
                .And(x => GivenTheDownstreamAddresses())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => x.GivenTheRequestIdCreatorReturns("blahhhh"))
                .When(x => x.WhenICreateTheConfig())
                .Then(x => x.ThenTheReRoutesAre(new List<ReRoute>
                {
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(downstreamReRoute)
                        .WithUpstreamPathTemplate("/api/products/{productId}")
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
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
            var httpHandlerOptions = new HttpHandlerOptions(true, true,false);

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                            {
                                new FileReRoute
                                {
                                    DownstreamHostAndPorts = new List<FileHostAndPort>
                                    {
                                        new FileHostAndPort
                                        {
                                            Host = "127.0.0.1",
                                        }
                                    },
                                    UpstreamPathTemplate = "/api/products/{productId}",
                                    DownstreamPathTemplate = "/products/{productId}",
                                    UpstreamHttpMethod = new List<string> { "Get" }
                                }
                            },
            }))
                .And(x => x.GivenTheFollowingOptionsAreReturned(reRouteOptions))
                .And(x => GivenTheDownstreamAddresses())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
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

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithAuthenticationOptions(authenticationOptions)
                .WithClaimsToHeaders(new List<ClaimToThing>
                {
                    new ClaimToThing("CustomerId", "CustomerId", "", 0),
                })
                .WithLoadBalancerKey("/api/products/{productId}|Get")
                .Build();

            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamReRoute(downstreamReRoute)
                    .WithUpstreamPathTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build()
            };

            this.Given(x => x.GivenTheConfigIs(fileConfig))
                .And(x => GivenTheDownstreamAddresses())
                .And(x => x.GivenTheConfigIsValid())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
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

            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("/products/{productId}")
                .WithUpstreamPathTemplate("/api/products/{productId}")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithAuthenticationOptions(authenticationOptions)
                .WithLoadBalancerKey("/api/products/{productId}|Get")
                .Build();

            var expected = new List<ReRoute>
            {
                new ReRouteBuilder()
                    .WithDownstreamReRoute(downstreamReRoute)
                    .WithUpstreamPathTemplate("/api/products/{productId}")
                    .WithUpstreamHttpMethod(new List<string> { "Get" })
                    .Build()
            };

            this.Given(x => x.GivenTheConfigIs(fileConfig))
                .And(x => GivenTheDownstreamAddresses())
                .And(x => x.GivenTheConfigIsValid())
                .And(x => GivenTheHeaderFindAndReplaceCreatorReturns())
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
            var errors = new List<Error> {new FileValidationFailedError("some message")};

            this.Given(x => x.GivenTheConfigIs(new FileConfiguration()))
                .And(x => GivenTheDownstreamAddresses())
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
        }

        private void WhenICreateTheConfig()
        {
            _config = _internalConfigurationCreator.Create(_fileConfiguration).Result;
        }

        private void ThenTheReRoutesAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i];
                var expected = expectedReRoutes[i];

                result.DownstreamReRoute.Count.ShouldBe(expected.DownstreamReRoute.Count);

                result.DownstreamReRoute[0].DownstreamPathTemplate.Value.ShouldBe(expected.DownstreamReRoute[0].DownstreamPathTemplate.Value);
                result.UpstreamHttpMethod.ShouldBe(expected.UpstreamHttpMethod);
                result.UpstreamPathTemplate.Value.ShouldBe(expected.UpstreamPathTemplate.Value);
                result.UpstreamTemplatePattern?.Template.ShouldBe(expected.UpstreamTemplatePattern?.Template);
                result.DownstreamReRoute[0].ClaimsToClaims.Count.ShouldBe(expected.DownstreamReRoute[0].ClaimsToClaims.Count);
                result.DownstreamReRoute[0].ClaimsToHeaders.Count.ShouldBe(expected.DownstreamReRoute[0].ClaimsToHeaders.Count);
                result.DownstreamReRoute[0].ClaimsToQueries.Count.ShouldBe(expected.DownstreamReRoute[0].ClaimsToQueries.Count);
                result.DownstreamReRoute[0].RequestIdKey.ShouldBe(expected.DownstreamReRoute[0].RequestIdKey);   
                result.DownstreamReRoute[0].LoadBalancerKey.ShouldBe(expected.DownstreamReRoute[0].LoadBalancerKey);   
                result.DownstreamReRoute[0].DelegatingHandlers.ShouldBe(expected.DownstreamReRoute[0].DelegatingHandlers);      
                result.DownstreamReRoute[0].AddHeadersToDownstream.ShouldBe(expected.DownstreamReRoute[0].AddHeadersToDownstream);           
                result.DownstreamReRoute[0].AddHeadersToUpstream.ShouldBe(expected.DownstreamReRoute[0].AddHeadersToUpstream, "AddHeadersToUpstream should be set");
            }
        }

        private void ThenTheAuthenticationOptionsAre(List<ReRoute> expectedReRoutes)
        {
            for (int i = 0; i < _config.Data.ReRoutes.Count; i++)
            {
                var result = _config.Data.ReRoutes[i].DownstreamReRoute[0].AuthenticationOptions;
                var expected = expectedReRoutes[i].DownstreamReRoute[0].AuthenticationOptions;
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
                .Returns(new UpstreamPathTemplate(pattern, 1));
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
                .Setup(x => x.Create(_fileConfiguration.ReRoutes[0].QoSOptions, It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(qosOptions);
        }

        private void ThenTheQosOptionsAre(QoSOptions qosOptions)
        {
            _config.Data.ReRoutes[0].DownstreamReRoute[0].QosOptions.DurationOfBreak.ShouldBe(qosOptions.DurationOfBreak);
            _config.Data.ReRoutes[0].DownstreamReRoute[0].QosOptions.ExceptionsAllowedBeforeBreaking.ShouldBe(qosOptions.ExceptionsAllowedBeforeBreaking);
            _config.Data.ReRoutes[0].DownstreamReRoute[0].QosOptions.TimeoutValue.ShouldBe(qosOptions.TimeoutValue);
        }

        private void ThenTheServiceProviderCreatorIsCalledCorrectly()
        {
            _serviceProviderConfigCreator
                .Verify(x => x.Create(_fileConfiguration.GlobalConfiguration), Times.Once);
        }

        private void ThenTheHeaderFindAndReplaceCreatorIsCalledCorrectly()
        {
            _headerFindAndReplaceCreator
                .Verify(x => x.Create(It.IsAny<FileReRoute>()), Times.Once);
        }

        private void GivenTheHeaderFindAndReplaceCreatorReturns()
        {
            _headerFindAndReplaceCreator.Setup(x => x.Create(It.IsAny<FileReRoute>())).Returns(new HeaderTransformations(new List<HeaderFindAndReplace>(), new List<HeaderFindAndReplace>(), new List<AddHeader>(), new List<AddHeader>()));
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

        private void ThenTheRegionCreatorIsCalledCorrectly()
        {
            _regionCreator
                .Verify(x => x.Create(_fileConfiguration.ReRoutes[0]), Times.Once);
        }
        
        private void GivenTheFollowingHttpHandlerOptionsAreReturned(HttpHandlerOptions httpHandlerOptions)
        {
            _httpHandlerOptionsCreator.Setup(x => x.Create(It.IsAny<FileHttpHandlerOptions>()))
                .Returns(httpHandlerOptions);
        }

        private void ThenTheHttpHandlerOptionsCreatorIsCalledCorrectly()
        {
            _httpHandlerOptionsCreator.Verify(x => x.Create(_fileConfiguration.ReRoutes[0].HttpHandlerOptions), Times.Once());
        }

        private void GivenTheDownstreamAddresses()
        {
            _downstreamAddressesCreator.Setup(x => x.Create(It.IsAny<FileReRoute>())).Returns(new List<DownstreamHostAndPort>());
        }
    }
}
