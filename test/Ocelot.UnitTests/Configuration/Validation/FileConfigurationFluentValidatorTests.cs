﻿namespace Ocelot.UnitTests.Configuration.Validation
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Validator;
    using Ocelot.Responses;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Requester;
    using Requester;
    using Ocelot.ServiceDiscovery.Providers;
    using Ocelot.Values;
    using Ocelot.ServiceDiscovery;

    public class FileConfigurationFluentValidatorTests
    {
        private IConfigurationValidator _configurationValidator;
        private FileConfiguration _fileConfiguration;
        private Response<ConfigurationValidationResult> _result;
        private readonly Mock<IAuthenticationSchemeProvider> _authProvider;

        public FileConfigurationFluentValidatorTests()
        {
            _authProvider = new Mock<IAuthenticationSchemeProvider>();
            var provider = new ServiceCollection()
                .BuildServiceProvider();
            // Todo - replace with mocks
            _configurationValidator = new FileConfigurationFluentValidator(provider, new ReRouteFluentValidator(_authProvider.Object, new HostAndPortValidator(), new FileQoSOptionsFluentValidator(provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(provider)));
        }

        [Fact]
        public void configuration_is_valid_if_service_discovery_options_specified_and_has_service_fabric_as_option()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test"
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Host = "localhost",
                        Type = "ServiceFabric",
                        Port = 8500
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test"
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500
                    }
                }
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
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test"
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500
                    }
                }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
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
                        Host = "localhost",
                        Type = "FakeServiceDiscoveryProvider",
                        Port = 8500
                    }
                }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_service_discovery_options_specified_but_no_service_discovery_handler_with_matching_name()
        {
            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/laura",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        ServiceName = "test"
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Host = "localhost",
                        Type = "consul",
                        Port = 8500
                    }
                }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .And(x => x.GivenAServiceDiscoveryHandler())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_qos_options_specified_and_has_qos_handler()
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
                        QoSOptions = new FileQoSOptions
                        {
                            TimeoutValue = 1,
                            ExceptionsAllowedBeforeBreaking = 1
                        }
                    }
                }
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
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    QoSOptions = new FileQoSOptions
                    {
                        TimeoutValue = 1,
                        ExceptionsAllowedBeforeBreaking = 1
                    }
                }
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
                        QoSOptions = new FileQoSOptions
                        {
                            TimeoutValue = 1,
                            ExceptionsAllowedBeforeBreaking = 1
                        }
                    }
                }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_qos_options_specified_globally_but_no_qos_handler()
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
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    QoSOptions = new FileQoSOptions
                    {
                        TimeoutValue = 1,
                        ExceptionsAllowedBeforeBreaking = 1
                    }
                }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot because either a ReRoute or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_aggregates_are_valid()
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
                            Key = "Laura"
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
                            Key = "Tom"
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
                            }
                        }
                    }
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
                            Key = "Laura"
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
                            UpstreamHost = "localhost"
                        }
                    },
                Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/tom",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            },
                        }
                    }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "reRoute /tom has duplicate aggregate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_if_aggregates_are_not_duplicate_of_re_routes()
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
                            Key = "Laura"
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
                            UpstreamHttpMethod = new List<string> { "Post" },
                            Key = "Tom",
                            UpstreamHost = "localhost"
                        }
                    },
                Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/tom",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            },
                        }
                    }
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
                            Key = "Laura"
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
                            UpstreamPathTemplate = "/lol",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            Key = "Tom"
                        }
                    },
                Aggregates = new List<FileAggregateReRoute>
                    {
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/tom",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            }
                        },
                        new FileAggregateReRoute
                        {
                            UpstreamPathTemplate = "/tom",
                            UpstreamHost = "localhost",
                            ReRouteKeys = new List<string>
                            {
                                "Tom",
                                "Laura"
                            }
                        }
                    }
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
                            Key = "Laura"
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
                            }
                        }
                    }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "ReRoutes for aggregateReRoute / either do not exist or do not have correct ServiceName property"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_aggregate_has_re_routes_with_specific_request_id_keys()
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
                            Key = "Laura"
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
                            RequestIdKey = "should_fail",
                            Key = "Tom"
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
                            }
                        }
                    }
            };

            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "aggregateReRoute / contains ReRoute with specific RequestIdKey, this is not possible with Aggregates"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_invalid_if_scheme_in_downstream_or_upstream_template()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "http://www.bbc.co.uk/api/products/{productId}",
                        UpstreamPathTemplate = "http://asdf.com"
                    }
                }
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
        public void configuration_is_valid_with_one_reroute()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk"
                            }
                        },
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "api/products/",
                        UpstreamPathTemplate = "/asdf/"
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "api/prod/",
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "//api/prod/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                                Port = 80
                            }
                        },
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "//api/products/",
                        UpstreamPathTemplate = "/api/prod/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                                Port = 80
                            }
                        },
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                            }
                        },
                        AuthenticationOptions = new FileAuthenticationOptions()
                        {
                            AuthenticationProviderKey = "Test"
                        }
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        AuthenticationOptions = new FileAuthenticationOptions()
                        {
                            AuthenticationProviderKey = "Test"
                        }
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Authentication Options AuthenticationProviderKey:Test,AllowedScopes:[] is unsupported authentication provider"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_with_duplicate_reroutes_all_verbs()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bb.co.uk"
                            }
                        },
                    },
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bb.co.uk"
                            }
                        },
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "reRoute /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_duplicate_reroutes_all_verbs_but_different_hosts()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/api/products/",
                            UpstreamPathTemplate = "/asdf/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "bb.co.uk"
                                }
                            },
                            UpstreamHost = "host1"
                        },
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/www/test/",
                            UpstreamPathTemplate = "/asdf/",
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new FileHostAndPort
                                {
                                    Host = "bb.co.uk"
                                }
                            },
                            UpstreamHost = "host1"
                        }
                    }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_with_duplicate_reroutes_specific_verbs()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                            }
                        },
                        UpstreamHttpMethod = new List<string> {"Get"}
                    },
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                            }
                        },
                        UpstreamHttpMethod = new List<string> {"Get"}
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "reRoute /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_with_duplicate_reroutes_different_verbs()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                            }
                        },
                    },
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/www/test/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Post"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                            }
                        },
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                            }
                        },
                        RateLimitOptions = new FileRateLimitRule
                        {
                            Period = "1x",
                            EnableRateLimiting = true
                        }
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk",
                            }
                        },
                        RateLimitOptions = new FileRateLimitRule
                        {
                            Period = "1d",
                            EnableRateLimiting = true
                        }
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        ServiceName = "Test"
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    ServiceDiscoveryProvider = new FileServiceDiscoveryProvider
                    {
                        Type = "servicefabric",
                        Host = "localhost",
                        Port = 1234
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = downstreamHost,
                            }
                        },
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "When not using service discovery Host must be set on DownstreamHostAndPorts if you are not using ReRoute.Host or Ocelot cannot find your service!"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_valid_when_not_using_service_discovery_and_host_is_set()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "bbc.co.uk"
                            }
                        },
                    }
                }
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
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort
                            {
                                Host = "test"
                            }
                        }
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_when_no_host_and_port()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                        }
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!"))
                .BDDfy();
        }

        [Fact]
        public void configuration_is_not_valid_when_host_and_port_is_empty()
        {
            this.Given(x => x.GivenAConfiguration(new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                {
                    new FileReRoute
                    {
                        DownstreamPathTemplate = "/api/products/",
                        UpstreamPathTemplate = "/asdf/",
                        UpstreamHttpMethod = new List<string> {"Get"},
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new FileHostAndPort()
                        }
                    }
                }
            }))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "When not using service discovery Host must be set on DownstreamHostAndPorts if you are not using ReRoute.Host or Ocelot cannot find your service!"))
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
                new AuthenticationScheme(name, name, typeof(TestHandler))
            });
        }

        private void GivenAQoSHandler()
        {
            var collection = new ServiceCollection();
            QosDelegatingHandlerDelegate del = (a,b) => new FakeDelegatingHandler();
            collection.AddSingleton<QosDelegatingHandlerDelegate>(del);
            var provider = collection.BuildServiceProvider();
            _configurationValidator = new FileConfigurationFluentValidator(provider, new ReRouteFluentValidator(_authProvider.Object, new HostAndPortValidator(), new FileQoSOptionsFluentValidator(provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(provider)));
        }

        private void GivenAServiceDiscoveryHandler()
        {
            var collection = new ServiceCollection();
            ServiceDiscoveryFinderDelegate del = (a,b,c) => new FakeServiceDiscoveryProvider();
            collection.AddSingleton<ServiceDiscoveryFinderDelegate>(del);
            var provider = collection.BuildServiceProvider();
            _configurationValidator = new FileConfigurationFluentValidator(provider, new ReRouteFluentValidator(_authProvider.Object, new HostAndPortValidator(), new FileQoSOptionsFluentValidator(provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(provider)));
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
