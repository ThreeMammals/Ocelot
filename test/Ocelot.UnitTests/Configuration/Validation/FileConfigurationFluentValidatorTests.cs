namespace Ocelot.UnitTests.Configuration.Validation
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Validator;
    using Ocelot.Requester;
    using Ocelot.Responses;
    using Ocelot.ServiceDiscovery;
    using Ocelot.ServiceDiscovery.Providers;
    using Ocelot.Values;
    using Requester;
    using Shouldly;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class FileConfigurationFluentValidatorTests
    {
        private IConfigurationValidator _configurationValidator;
        private FileConfiguration _fileConfiguration;
        private Response<ConfigurationValidationResult> _result;
        private readonly Mock<IAuthenticationSchemeProvider> _authProvider;
        private readonly string _clusterOneId = "cluster1";
        private readonly string _clusterTwoId = "cluster2";

        public FileConfigurationFluentValidatorTests()
        {
            _authProvider = new Mock<IAuthenticationSchemeProvider>();
            var provider = new ServiceCollection()
                .BuildServiceProvider();
            // Todo - replace with mocks
            _configurationValidator = new FileConfigurationFluentValidator(provider, new RouteFluentValidator(_authProvider.Object, new FileQoSOptionsFluentValidator(provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(provider)), new ClusterValidator(new DestinationValidator()));
        }

        [Fact]
        public void configuration_is_valid_if_service_discovery_options_specified_and_has_service_fabric_as_option()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test",
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "localhost",
                        Type = "ServiceFabric",
                        Port = 8500,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_service_discovery_options_specified_and_has_service_discovery_handler()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test",
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .And(x => x.GivenAServiceDiscoveryHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_service_discovery_options_specified_dynamically_and_has_service_discovery_handler()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .And(x => x.GivenAServiceDiscoveryHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_service_discovery_options_specified_but_no_service_discovery_handler()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test",
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_service_discovery_options_specified_dynamically_but_service_discovery_handler()
        {
            var configuration = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_service_discovery_options_specified_but_no_service_discovery_handler_with_matching_name()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test",
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Host = "localhost",
                        Type = "consul",
                        Port = 8500,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .And(x => x.GivenAServiceDiscoveryHandler())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_qos_options_specified_and_has_qos_handler()
        {

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                        QoSOptions = new FileQoSOptions
                        {
                            TimeoutValue = 1,
                            ExceptionsAllowedBeforeBreaking = 1,
                        },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .And(x => x.GivenAQoSHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_qos_options_specified_globally_and_has_qos_handler()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    QoSOptions = new FileQoSOptions
                    {
                        TimeoutValue = 1,
                        ExceptionsAllowedBeforeBreaking = 1,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .And(x => x.GivenAQoSHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_qos_options_specified_but_no_qos_handler()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                        QoSOptions = new FileQoSOptions
                        {
                            TimeoutValue = 1,
                            ExceptionsAllowedBeforeBreaking = 1,
                        },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_qos_options_specified_globally_but_no_qos_handler()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    QoSOptions = new FileQoSOptions
                    {
                        TimeoutValue = 1,
                        ExceptionsAllowedBeforeBreaking = 1,
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_aggregates_are_valid()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterTwoId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/tom",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Tom",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                    {_clusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51880",
                                    }
                                },
                            },
                        }
                    },
                },
                Aggregates = new List<FileAggregateRoute>
                {
                    new FileAggregateRoute
                    {
                        UpstreamPathTemplate = "/",
                        UpstreamHost = "localhost",
                        RouteIds = new List<string>
                        {
                            "Tom",
                            "Laura",
                        },
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_aggregates_are_duplicate_of_re_routes()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterTwoId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/tom",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Tom",
                        UpstreamHost = "localhost",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                    {_clusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51880",
                                    }
                                },
                            },
                        }
                    },
                },
                Aggregates = new List<FileAggregateRoute>
                {
                    new FileAggregateRoute
                    {
                        UpstreamPathTemplate = "/tom",
                        UpstreamHost = "localhost",
                        RouteIds = new List<string>
                        {
                            "Tom",
                            "Laura",
                        },
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /tom has duplicate aggregate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_aggregates_are_not_duplicate_of_re_routes()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/tom",
                        UpstreamHttpMethod = new List<string> { "Post" },
                        RouteId = "Tom",
                        UpstreamHost = "localhost",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                    {_clusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51880",
                                    }
                                },
                            },
                        }
                    },
                },
                Aggregates = new List<FileAggregateRoute>
                {
                    new FileAggregateRoute
                    {
                        UpstreamPathTemplate = "/tom",
                        UpstreamHost = "localhost",
                        RouteIds = new List<string>
                        {
                            "Tom",
                            "Laura",
                        },
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_aggregates_are_duplicate_of_aggregates()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterTwoId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/lol",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Tom",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                    {_clusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51880",
                                    }
                                },
                            },
                        }
                    },
                },
                Aggregates = new List<FileAggregateRoute>
                {
                    new FileAggregateRoute
                    {
                        UpstreamPathTemplate = "/tom",
                        UpstreamHost = "localhost",
                        RouteIds = new List<string>
                        {
                            "Tom",
                            "Laura",
                        },
                    },
                    new FileAggregateRoute
                    {
                        UpstreamPathTemplate = "/tom",
                        UpstreamHost = "localhost",
                        RouteIds = new List<string>
                        {
                            "Tom",
                            "Laura",
                        },
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "aggregate /tom has duplicate aggregate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_re_routes_dont_exist_for_aggregate()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                },
                Aggregates = new List<FileAggregateRoute>
                {
                    new FileAggregateRoute
                    {
                        UpstreamPathTemplate = "/",
                        UpstreamHost = "localhost",
                        RouteIds = new List<string>
                        {
                            "Tom",
                            "Laura",
                        },
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Routes for aggregateRoute / either do not exist or do not have correct ServiceName property"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_aggregate_has_re_routes_with_specific_request_id_keys()
        {
            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RouteId = "Laura",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterTwoId,
                        DownstreamPathTemplate = "/",
                        UpstreamPathTemplate = "/tom",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        RequestIdKey = "should_fail",
                        RouteId = "Tom",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51878",
                                    }
                                },
                            },
                        }
                    },
                    {_clusterTwoId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterTwoId}/destination1", new FileDestination
                                    {
                                        Address = "http://localhost:51880",
                                    }
                                },
                            },
                        }
                    },
                },
                Aggregates = new List<FileAggregateRoute>
                {
                    new FileAggregateRoute
                    {
                        UpstreamPathTemplate = "/",
                        UpstreamHost = "localhost",
                        RouteIds = new List<string>
                        {
                            "Tom",
                            "Laura",
                        },
                    },
                },
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "aggregateRoute / contains Route with specific RequestIdKey, this is not possible with Aggregates"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_scheme_in_downstream_or_upstream_template()
        {
            //TODO: This test is in the wrong place should be RouteValidationTests
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    { 
                        ClusterId = "Dave",
                        DownstreamPathTemplate = "http://www.bbc.co.uk/api/products/{productId}",
                        UpstreamPathTemplate = "http://asdf.com",
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .Then(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Downstream Path Template http://www.bbc.co.uk/api/products/{productId} doesnt start with forward slash"))
                .And(x => x.ThenTheErrorMessageAtPositionIs(1, "Downstream Path Template http://www.bbc.co.uk/api/products/{productId} contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature."))
                .And(x => x.ThenTheErrorMessageAtPositionIs(2, "Downstream Path Template http://www.bbc.co.uk/api/products/{productId} contains scheme"))

                .And(x => x.ThenTheErrorMessageAtPositionIs(3, "Upstream Path Template http://asdf.com contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature."))
                .And(x => x.ThenTheErrorMessageAtPositionIs(4, "Upstream Path Template http://asdf.com doesnt start with forward slash"))
                .And(x => x.ThenTheErrorMessageAtPositionIs(5, "Upstream Path Template http://asdf.com contains scheme"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_one_route()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk:80",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_without_slash_prefix_downstream_path_template()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "api/products/",
                        UpstreamPathTemplate = "/asdf/",
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Downstream Path Template api/products/ doesnt start with forward slash"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_without_slash_prefix_upstream_path_template()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "api/prod/",
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Upstream Path Template api/prod/ doesnt start with forward slash"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_upstream_url_contains_forward_slash_then_another_forward_slash()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "//api/prod/",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk:80",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Upstream Path Template //api/prod/ contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature."))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_downstream_url_contains_forward_slash_then_another_forward_slash()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "//api/products/",
                        UpstreamPathTemplate = "/api/prod/",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk:80",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Downstream Path Template //api/products/ contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature."))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_valid_authentication_provider()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        AuthenticationOptions = new FileAuthenticationOptions()
                        {
                            AuthenticationProviderKey = "Test",
                        },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk:80",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .And(x => x.GivenTheAuthSchemeExists("Test"))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_with_invalid_authentication_provider()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        AuthenticationOptions = new FileAuthenticationOptions()
                        {
                            AuthenticationProviderKey = "Test",
                        },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk:80",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Authentication Options AuthenticationProviderKey:Test,AllowedScopes:[] is unsupported authentication provider"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_with_duplicate_routes_all_verbs()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bb.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_duplicate_routes_all_verbs_but_different_hosts()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHost = "host1",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHost = "host2",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bb.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_with_duplicate_routes_specific_verbs()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_duplicate_routes_different_verbs()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Post"},
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_with_duplicate_routes_with_duplicated_upstreamhosts()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string>(),
                        UpstreamHost = "upstreamhost",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string>(),
                        UpstreamHost = "upstreamhost",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_duplicate_routes_but_different_upstreamhosts()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string>(),
                        UpstreamHost = "upstreamhost111",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string>(),
                        UpstreamHost = "upstreamhost222",
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_duplicate_routes_but_one_upstreamhost_is_not_set()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string>(),
                        UpstreamHost = "upstreamhost",
                    },
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string>(),
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }
        
        [Fact]
        public void configuration_is_invalid_with_invalid_rate_limit_configuration()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        RateLimitOptions = new FileRateLimitRule
                        {
                            Period = "1x",
                            EnableRateLimiting = true,
                        },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "RateLimitOptions.Period does not contain integer then s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_valid_rate_limit_configuration()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        RateLimitOptions = new FileRateLimitRule
                        {
                            Period = "1d",
                            EnableRateLimiting = true,
                        },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_using_service_discovery_and_service_name()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        ServiceName = "Test",
                    },
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Scheme = "https",
                        Type = "servicefabric",
                        Host = "localhost",
                        Port = 1234,
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void configuration_is_invalid_when_not_using_service_discovery_and_host(string downstreamHost)
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = downstreamHost,
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Address cannot be empty"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_when_not_using_service_discovery_and_host_is_set()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://bbc.co.uk",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_when_no_downstream_but_has_host_and_port()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://test",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_when_no_destination()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "When not using service discovery Cluster.Destinations must be set or Ocelot cannot find your service!"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_when_destination_address_is_empty()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Address cannot be empty"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_when_placeholder_is_used_twice_in_upstream_path_template()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        ClusterId = _clusterOneId,
                        DownstreamPathTemplate = "/bar/{everything}",
                        UpstreamPathTemplate = "/foo/bar/{everything}/{everything}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                    },
                },
                Clusters = new Dictionary<string, FileCluster>
                {
                    {_clusterOneId, new FileCluster
                        {
                            Destinations = new Dictionary<string, FileDestination>
                            {
                                {$"{_clusterOneId}/destination1", new FileDestination
                                    {
                                        Address = "http://a.b.cd",
                                    }
                                },
                            },
                        }
                    },
                },
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /foo/bar/{everything}/{everything} has duplicated placeholder"))
                .BDDfy();
        }

        private void GivenAConfiguration(FileConfiguration fileConfiguration)
        {
            _fileConfiguration = fileConfiguration;
        }

        private void WhenIValidateTheConfiguration()
        {
            _result = _configurationValidator.IsValid(_fileConfiguration).Result;
        }

        private void ThenTheResultIsValid()
        {
            _result.Data.IsError.ShouldBeFalse();
        }

        private void ThenTheResultIsNotValid()
        {
            _result.Data.IsError.ShouldBeTrue();
        }

        private void ThenTheErrorIs<T>()
        {
            _result.Data.Errors[0].ShouldBeOfType<T>();
        }

        private void ThenTheErrorMessageAtPositionIs(int index, string expected)
        {
            _result.Data.Errors[index].Message.ShouldBe(expected);
        }

        private void GivenTheAuthSchemeExists(string name)
        {
            _authProvider.Setup(x => x.GetAllSchemesAsync()).ReturnsAsync(new List<AuthenticationScheme>
            {
                new AuthenticationScheme(name, name, typeof(TestHandler)),
            });
        }

        private void GivenAQoSHandler()
        {
            var collection = new ServiceCollection();
            QosDelegatingHandlerDelegate del = (a, b) => new FakeDelegatingHandler();
            collection.AddSingleton<QosDelegatingHandlerDelegate>(del);
            var provider = collection.BuildServiceProvider();
            _configurationValidator = new FileConfigurationFluentValidator(provider, new RouteFluentValidator(_authProvider.Object, new FileQoSOptionsFluentValidator(provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(provider)), new ClusterValidator(new DestinationValidator()));
        }

        private void GivenAServiceDiscoveryHandler()
        {
            var collection = new ServiceCollection();
            ServiceDiscoveryFinderDelegate del = (a, b, c) => new FakeServiceDiscoveryProvider();
            collection.AddSingleton<ServiceDiscoveryFinderDelegate>(del);
            var provider = collection.BuildServiceProvider();
            _configurationValidator = new FileConfigurationFluentValidator(provider, new RouteFluentValidator(_authProvider.Object, new FileQoSOptionsFluentValidator(provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(provider)), new ClusterValidator(new DestinationValidator()));
        }

        private class FakeServiceDiscoveryProvider : IServiceDiscoveryProvider
        {
            public Task<List<Service>> Get()
            {
                throw new System.NotImplementedException();
            }
        }

        private class TestOptions : AuthenticationSchemeOptions
        {
        }

        private class TestHandler : AuthenticationHandler<TestOptions>
        {
            public TestHandler(IOptionsMonitor<TestOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var principal = new ClaimsPrincipal();
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), Scheme.Name)));
            }
        }
    }
}
