using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Validator;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.UnitTests.Requester;
using Ocelot.Values;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Ocelot.UnitTests.Configuration.Validation
{
    public class FileConfigurationFluentValidatorTests : UnitTest
    {
        private IConfigurationValidator _configurationValidator;
        private FileConfiguration _fileConfiguration;
        private Response<ConfigurationValidationResult> _result;
        private IServiceProvider _provider;
        private readonly ServiceCollection _services;
        private readonly Mock<IAuthenticationSchemeProvider> _authProvider;

        public FileConfigurationFluentValidatorTests()
        {
            _services = new ServiceCollection();
            _authProvider = new Mock<IAuthenticationSchemeProvider>();
            _provider = _services.BuildServiceProvider();

            // TODO Replace with mocks
            _configurationValidator = new FileConfigurationFluentValidator(_provider, new RouteFluentValidator(_authProvider.Object, new HostAndPortValidator(), new FileQoSOptionsFluentValidator(_provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(_provider)));
        }

        [Fact]
        public void Configuration_is_valid_if_service_discovery_options_specified_and_has_service_fabric_as_option()
        {
            var route = GivenServiceDiscoveryRoute();
            var configuration = GivenAConfiguration(route);
            configuration.GlobalConfiguration.ServiceDiscoveryProvider = GivenDefaultServiceDiscoveryProvider();
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_if_service_discovery_options_specified_and_has_service_discovery_handler()
        {
            var route = GivenServiceDiscoveryRoute();
            var configuration = GivenAConfiguration(route);
            configuration.GlobalConfiguration.ServiceDiscoveryProvider = GivenDefaultServiceDiscoveryProvider();
            configuration.GlobalConfiguration.ServiceDiscoveryProvider.Type = "FakeServiceDiscoveryProvider";
            this.Given(x => x.GivenAConfiguration(configuration))
                .And(x => x.GivenAServiceDiscoveryHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_if_service_discovery_options_specified_dynamically_and_has_service_discovery_handler()
        {
            var configuration = new FileConfiguration();
            configuration.GlobalConfiguration.ServiceDiscoveryProvider = GivenDefaultServiceDiscoveryProvider();
            configuration.GlobalConfiguration.ServiceDiscoveryProvider.Type = "FakeServiceDiscoveryProvider";
            this.Given(x => x.GivenAConfiguration(configuration))
                .And(x => x.GivenAServiceDiscoveryHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_service_discovery_options_specified_but_no_service_discovery_handler()
        {
            var route = GivenServiceDiscoveryRoute();
            var configuration = GivenAConfiguration(route);
            configuration.GlobalConfiguration.ServiceDiscoveryProvider = GivenDefaultServiceDiscoveryProvider();
            configuration.GlobalConfiguration.ServiceDiscoveryProvider.Type = "FakeServiceDiscoveryProvider";
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_service_discovery_options_specified_dynamically_but_service_discovery_handler()
        {
            var configuration = new FileConfiguration();
            configuration.GlobalConfiguration.ServiceDiscoveryProvider = GivenDefaultServiceDiscoveryProvider();
            configuration.GlobalConfiguration.ServiceDiscoveryProvider.Type = "FakeServiceDiscoveryProvider";
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_service_discovery_options_specified_but_no_service_discovery_handler_with_matching_name()
        {
            var route = GivenServiceDiscoveryRoute();
            var configuration = GivenAConfiguration(route);
            configuration.GlobalConfiguration.ServiceDiscoveryProvider = GivenDefaultServiceDiscoveryProvider();
            configuration.GlobalConfiguration.ServiceDiscoveryProvider.Type = "consul";
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .And(x => x.GivenAServiceDiscoveryHandler())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot, errors are: Unable to start Ocelot because either a Route or GlobalConfiguration are using ServiceDiscoveryOptions but no ServiceDiscoveryFinderDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Consul and services.AddConsul() or Ocelot.Provider.Eureka and services.AddEureka()?"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_if_qos_options_specified_and_has_qos_handler()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            route.QoSOptions = new FileQoSOptions
            {
                TimeoutValue = 1,
                ExceptionsAllowedBeforeBreaking = 1,
            };
            this.Given(x => x.GivenAConfiguration(route))
                .And(x => x.GivenAQoSHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_if_qos_options_specified_globally_and_has_qos_handler()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var configuration = GivenAConfiguration(route);
            configuration.GlobalConfiguration.QoSOptions = new FileQoSOptions
            {
                TimeoutValue = 1,
                ExceptionsAllowedBeforeBreaking = 1,
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .And(x => x.GivenAQoSHandler())
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_qos_options_specified_but_no_qos_handler()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            route.QoSOptions = new FileQoSOptions
            {
                TimeoutValue = 1,
                ExceptionsAllowedBeforeBreaking = 1,
            };
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_qos_options_specified_globally_but_no_qos_handler()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var configuration = GivenAConfiguration(route);
            configuration.GlobalConfiguration.QoSOptions = new FileQoSOptions
            {
                TimeoutValue = 1,
                ExceptionsAllowedBeforeBreaking = 1,
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorIs<FileValidationFailedError>())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered in dependency injection container. Are you missing a package like Ocelot.Provider.Polly and services.AddPolly()?"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_if_aggregates_are_valid()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var route2 = GivenDefaultRoute("/tom", "/");
            route2.Key = "Tom";
            var configuration = GivenAConfiguration(route, route2);
            configuration.Aggregates = new()
            {
                new()
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = new() { "Tom", "Laura" },
                },
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_aggregates_are_duplicate_of_routes()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var route2 = GivenDefaultRoute("/tom", "/");
            route2.Key = "Tom";
            route2.UpstreamHost = "localhost";
            var configuration = GivenAConfiguration(route, route2);
            configuration.Aggregates = new()
            {
                new()
                {
                    UpstreamPathTemplate = "/tom",
                    UpstreamHost = "localhost",
                    RouteKeys = new() { "Tom", "Laura" },
                },
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /tom has duplicate aggregate"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_if_aggregates_are_not_duplicate_of_routes()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var route2 = GivenDefaultRoute("/tom", "/");
            route2.Key = "Tom";
            route2.UpstreamHttpMethod = new() { "Post" };
            var configuration = GivenAConfiguration(route, route2);
            configuration.Aggregates = new()
            {
                new()
                {
                    UpstreamPathTemplate = "/tom",
                    UpstreamHost = "localhost",
                    RouteKeys = new() { "Tom", "Laura" },
                },
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_aggregates_are_duplicate_of_aggregates()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var route2 = GivenDefaultRoute("/lol", "/");
            route2.Key = "Tom";
            var configuration = GivenAConfiguration(route, route2);
            configuration.Aggregates = new()
            {
                new()
                {
                    UpstreamPathTemplate = "/tom",
                    UpstreamHost = "localhost",
                    RouteKeys = new() { "Tom", "Laura" },
                },
                new()
                {
                    UpstreamPathTemplate = "/tom",
                    UpstreamHost = "localhost",
                    RouteKeys = new() { "Tom", "Laura" },
                },
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "aggregate /tom has duplicate aggregate"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_routes_dont_exist_for_aggregate()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var configuration = GivenAConfiguration(route);
            configuration.Aggregates = new()
            {
                new()
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = new() { "Tom", "Laura" },
                },
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Routes for aggregateRoute / either do not exist or do not have correct ServiceName property"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_aggregate_has_routes_with_specific_request_id_keys()
        {
            var route = GivenDefaultRoute("/laura", "/");
            route.Key = "Laura";
            var route2 = GivenDefaultRoute("/tom", "/");
            route2.Key = "Tom";
            route2.RequestIdKey = "should_fail";
            var configuration = GivenAConfiguration(route, route2);
            configuration.Aggregates = new()
            {
                new()
                {
                    UpstreamPathTemplate = "/",
                    UpstreamHost = "localhost",
                    RouteKeys = new() { "Tom", "Laura" },
                },
            };
            this.Given(x => x.GivenAConfiguration(configuration))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "aggregateRoute / contains Route with specific RequestIdKey, this is not possible with Aggregates"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_scheme_in_downstream_or_upstream_template()
        {
            this.Given(x => x.GivenAConfiguration(GivenDefaultRoute("http://asdf.com", "http://www.bbc.co.uk/api/products/{productId}")))
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
        public void Configuration_is_valid_with_one_route()
        {
            this.Given(x => x.GivenAConfiguration(GivenDefaultRoute()))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_without_slash_prefix_downstream_path_template()
        {
            this.Given(x => x.GivenAConfiguration(GivenDefaultRoute("/asdf/", "api/products/")))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Downstream Path Template api/products/ doesnt start with forward slash"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_without_slash_prefix_upstream_path_template()
        {
            this.Given(x => x.GivenAConfiguration(GivenDefaultRoute("api/prod/", "/api/products/")))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Upstream Path Template api/prod/ doesnt start with forward slash"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_upstream_url_contains_forward_slash_then_another_forward_slash()
        {
            this.Given(x => x.GivenAConfiguration(GivenDefaultRoute("//api/prod/", "/api/products/")))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Upstream Path Template //api/prod/ contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature."))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_if_downstream_url_contains_forward_slash_then_another_forward_slash()
        {
            this.Given(x => x.GivenAConfiguration(GivenDefaultRoute("/api/prod/", "//api/products/")))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Downstream Path Template //api/products/ contains double forward slash, Ocelot does not support this at the moment. Please raise an issue in GitHib if you need this feature."))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_with_valid_authentication_provider()
        {
            var route = GivenDefaultRoute();
            route.AuthenticationOptions.AuthenticationProviderKey = "Test";
            this.Given(x => x.GivenAConfiguration(route))
                .And(x => x.GivenTheAuthSchemeExists("Test"))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_with_invalid_authentication_provider()
        {
            var route = GivenDefaultRoute();
            route.AuthenticationOptions = new FileAuthenticationOptions()
            {
                AuthenticationProviderKey = "Test",
                AuthenticationProviderKeys = new string[] { "Test #1", "Test #2" },
            };
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "Authentication Options AuthenticationProviderKey:'Test',AuthenticationProviderKeys:['Test #1','Test #2'],AllowedScopes:[] is unsupported authentication provider"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_not_valid_with_duplicate_routes_all_verbs()
        {
            var route = GivenDefaultRoute();
            var duplicate = GivenDefaultRoute();
            duplicate.DownstreamPathTemplate = "/www/test/";
            this.Given(x => x.GivenAConfiguration(route, duplicate))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_with_duplicate_routes_all_verbs_but_different_hosts()
        {
            var route = GivenDefaultRoute();
            route.UpstreamHost = "host1";
            var duplicate = GivenDefaultRoute(null, "/www/test/");
            duplicate.UpstreamHost = "host2";
            this.Given(x => x.GivenAConfiguration(route, duplicate))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_not_valid_with_duplicate_routes_specific_verbs()
        {
            var route = GivenDefaultRoute();
            var duplicate = GivenDefaultRoute(null, "/www/test/");
            duplicate.UpstreamHttpMethod = new() { "Get" };
            this.Given(x => x.GivenAConfiguration(route, duplicate))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_with_duplicate_routes_different_verbs()
        {
            var route = GivenDefaultRoute(); // "Get" verb is inside
            var duplicate = GivenDefaultRoute(null, "/www/test/");
            duplicate.UpstreamHttpMethod = new() { "Post" };
            this.Given(x => x.GivenAConfiguration(route, duplicate))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_not_valid_with_duplicate_routes_with_duplicated_upstreamhosts()
        {
            var route = GivenDefaultRoute();
            route.UpstreamHttpMethod = new();
            route.UpstreamHost = "upstreamhost";

            var duplicate = GivenDefaultRoute(null, "/www/test/");
            duplicate.UpstreamHttpMethod = new();
            duplicate.UpstreamHost = "upstreamhost";

            this.Given(x => x.GivenAConfiguration(route, duplicate))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                 .And(x => x.ThenTheErrorMessageAtPositionIs(0, "route /asdf/ has duplicate"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_with_duplicate_routes_but_different_upstreamhosts()
        {
            var route = GivenDefaultRoute();
            route.UpstreamHttpMethod = new();
            route.UpstreamHost = "upstreamhost111";

            var duplicate = GivenDefaultRoute(null, "/www/test/");
            duplicate.UpstreamHttpMethod = new();
            duplicate.UpstreamHost = "upstreamhost222";

            this.Given(x => x.GivenAConfiguration(route, duplicate))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_with_duplicate_routes_but_one_upstreamhost_is_not_set()
        {
            var route = GivenDefaultRoute();
            route.UpstreamHttpMethod = new();
            route.UpstreamHost = "upstreamhost";

            var duplicate = GivenDefaultRoute(null, "/www/test/");
            duplicate.UpstreamHttpMethod = new();

            this.Given(x => x.GivenAConfiguration(route, duplicate))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_invalid_with_invalid_rate_limit_configuration()
        {
            var route = GivenDefaultRoute();
            route.RateLimitOptions = new FileRateLimitRule
            {
                Period = "1x",
                EnableRateLimiting = true,
            };
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "RateLimitOptions.Period does not contain integer then s (second), m (minute), h (hour), d (day) e.g. 1m for 1 minute period"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_with_valid_rate_limit_configuration()
        {
            var route = GivenDefaultRoute();
            route.RateLimitOptions = new FileRateLimitRule
            {
                Period = "1d",
                EnableRateLimiting = true,
            };
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_with_using_service_discovery_and_service_name()
        {
            var route = GivenServiceDiscoveryRoute();
            var config = GivenAConfiguration(route);
            config.GlobalConfiguration.ServiceDiscoveryProvider = GivenDefaultServiceDiscoveryProvider();
            this.Given(x => x.GivenAConfiguration(config))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        private const string Empty = "";

        [Theory]
        [InlineData(null)]
        [InlineData(Empty)]
        public void Configuration_is_invalid_when_not_using_service_discovery_and_host(string downstreamHost)
        {
            var route = GivenDefaultRoute();
            route.DownstreamHostAndPorts[0].Host = downstreamHost;
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "When not using service discovery Host must be set on DownstreamHostAndPorts if you are not using Route.Host or Ocelot cannot find your service!"))
                .BDDfy();
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(Empty, true)]
        [InlineData("Test", false)]
        public async Task HaveServiceDiscoveryProviderRegistered_RouteServiceName_Validated(string serviceName, bool valid)
        {
            // Arrange
            var route = GivenDefaultRoute();
            route.ServiceName = serviceName;
            var config = GivenAConfiguration(route);
            config.GlobalConfiguration.ServiceDiscoveryProvider = null;

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            _result.Data.IsError.ShouldNotBe(valid);
            _result.Data.Errors.Count.ShouldBe(valid ? 0 : 1);
        }

        [Theory]
        [InlineData(false, null, false)]
        [InlineData(true, null, false)]
        [InlineData(true, "type", false)]
        [InlineData(true, "servicefabric", true)]
        public async Task HaveServiceDiscoveryProviderRegistered_ServiceDiscoveryProvider_Validated(bool create, string type, bool valid)
        {
            // Arrange
            var route = GivenServiceDiscoveryRoute();
            var config = GivenAConfiguration(route);
            var provider = create ? GivenDefaultServiceDiscoveryProvider() : null;
            config.GlobalConfiguration.ServiceDiscoveryProvider = provider;
            if (create && provider != null)
            {
                provider.Type = type;
            }

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            _result.Data.IsError.ShouldNotBe(valid);
            _result.Data.Errors.Count.ShouldBeGreaterThanOrEqualTo(valid ? 0 : 1);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task HaveServiceDiscoveryProviderRegistered_ServiceDiscoveryFinderDelegates_Validated(bool hasDelegate)
        {
            // Arrange
            var valid = hasDelegate;
            var route = GivenServiceDiscoveryRoute();
            var config = GivenAConfiguration(route);
            config.GlobalConfiguration.ServiceDiscoveryProvider = null;
            if (hasDelegate)
            {
                GivenAServiceDiscoveryHandler();
            }

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            _result.Data.IsError.ShouldNotBe(valid);
            _result.Data.Errors.Count.ShouldBe(valid ? 0 : 1);
        }

        [Fact]
        public void Configuration_is_valid_when_not_using_service_discovery_and_host_is_set()
        {
            var route = GivenDefaultRoute();
            route.DownstreamHostAndPorts = new()
            {
                new("bbc.co.uk", 123),
            };
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_valid_when_no_downstream_but_has_host_and_port()
        {
            var route = GivenDefaultRoute();
            route.DownstreamHostAndPorts = new()
            {
                new("test", 123),
            };
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsValid())
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_not_valid_when_no_host_and_port()
        {
            var route = GivenDefaultRoute();
            route.DownstreamHostAndPorts = new();
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "When not using service discovery DownstreamHostAndPorts must be set and not empty or Ocelot cannot find your service!"))
                .BDDfy();
        }

        [Fact]
        public void Configuration_is_not_valid_when_host_and_port_is_empty()
        {
            var route = GivenDefaultRoute();
            route.DownstreamHostAndPorts = new()
            {
                new(),
            };
            this.Given(x => x.GivenAConfiguration(route))
                .When(x => x.WhenIValidateTheConfiguration())
                .Then(x => x.ThenTheResultIsNotValid())
                .And(x => x.ThenTheErrorMessageAtPositionIs(0, "When not using service discovery Host must be set on DownstreamHostAndPorts if you are not using Route.Host or Ocelot cannot find your service!"))
                .BDDfy();
        }

        [Fact]
        [Trait("PR", "1312")]
        [Trait("Feat", "360")]
        public async Task Configuration_is_not_valid_when_upstream_headers_the_same()
        {
            // Arrange
            var route1 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/api/products/", new()
                            {
                                { "header1", "value1" },
                                { "header2", "value2" },
                            });
            var route2 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/www/test/", new()
                            {
                                { "header2", "value2" },
                                { "header1", "value1" },
                            });
            GivenAConfiguration(route1, route2);

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            ThenTheResultIsNotValid();
            ThenTheErrorMessageAtPositionIs(0, "route /asdf/ has duplicate");
        }

        [Fact]
        [Trait("PR", "1312")]
        [Trait("Feat", "360")]
        public async Task Configuration_is_valid_when_upstream_headers_not_the_same()
        {
            // Arrange
            var route1 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/api/products/", new()
                            {
                                { "header1", "value1" },
                                { "header2", "value2" },
                            });
            var route2 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/www/test/", new()
                            {
                                { "header2", "value2" },
                                { "header1", "valueDIFFERENT" },
                            });
            GivenAConfiguration(route1, route2);

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            ThenTheResultIsValid();
        }

        [Fact]
        [Trait("PR", "1312")]
        [Trait("Feat", "360")]
        public async Task Configuration_is_valid_when_upstream_headers_count_not_the_same()
        {
            // Arrange
            var route1 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/api/products/", new()
                            {
                                { "header1", "value1" },
                                { "header2", "value2" },
                            });
            var route2 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/www/test/", new()
                            {
                                { "header2", "value2" },
                            });
            GivenAConfiguration(route1, route2);

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            ThenTheResultIsValid();
        }

        [Fact]
        [Trait("PR", "1312")]
        [Trait("Feat", "360")]
        public async Task Configuration_is_valid_when_one_upstream_headers_empty_and_other_not_empty()
        {
            // Arrange
            var route1 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/api/products/", new()
                            {
                                { "header1", "value1" },
                                { "header2", "value2" },
                            });
            var route2 = GivenRouteWithUpstreamHeaderTemplates("/asdf/", "/www/test/", new());
            GivenAConfiguration(route1, route2);

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            ThenTheResultIsValid();
        }

        [Theory]
        [Trait("PR", "1927")]
        [InlineData("/foo/{bar}/foo", "/yahoo/foo/{bar}")] // valid
        [InlineData("/foo/{bar}/{foo}", "/yahoo/{foo}/{bar}")] // valid
        [InlineData("/foo/{bar}/{bar}", "/yahoo/foo/{bar}", "UpstreamPathTemplate '/foo/{bar}/{bar}' has duplicated placeholder")] // invalid
        [InlineData("/foo/{bar}/{bar}", "/yahoo/{foo}/{bar}", "UpstreamPathTemplate '/foo/{bar}/{bar}' has duplicated placeholder")] // invalid
        [InlineData("/yahoo/foo/{bar}", "/foo/{bar}/foo")] // valid
        [InlineData("/yahoo/{foo}/{bar}", "/foo/{bar}/{foo}")] // valid
        [InlineData("/yahoo/foo/{bar}", "/foo/{bar}/{bar}", "DownstreamPathTemplate '/foo/{bar}/{bar}' has duplicated placeholder")] // invalid
        [InlineData("/yahoo/{foo}/{bar}", "/foo/{bar}/{bar}", "DownstreamPathTemplate '/foo/{bar}/{bar}' has duplicated placeholder")] // invalid
        public async Task IsPlaceholderNotDuplicatedIn_RuleForFileRoute_PathTemplatePlaceholdersAreValidated(string upstream, string downstream, params string[] expected)
        {
            // Arrange
            var route = GivenDefaultRoute(upstream, downstream);
            GivenAConfiguration(route);

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            ThenThereAreErrors(expected.Length > 0);
            ThenTheErrorMessagesAre(expected);
        }

        [Theory]
        [Trait("PR", "1927")]
        [Trait("Bug", "683")]
        [InlineData("/foo/bar/{everything}/{everything}", "/bar/{everything}", "foo", "UpstreamPathTemplate '/foo/bar/{everything}/{everything}' has duplicated placeholder")]
        [InlineData("/foo/bar/{everything}/{everything}", "/bar/{everything}/{everything}", "foo", "UpstreamPathTemplate '/foo/bar/{everything}/{everything}' has duplicated placeholder", "DownstreamPathTemplate '/bar/{everything}/{everything}' has duplicated placeholder")]
        public async Task Configuration_is_invalid_when_placeholder_is_used_twice_in_upstream_path_template(string upstream, string downstream, string host, params string[] expected)
        {
            // Arrange
            var route = GivenDefaultRoute(upstream, downstream, host);
            GivenAConfiguration(route);

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            ThenTheResultIsNotValid();
            ThenTheErrorMessagesAre(expected);
        }

        [Theory]
        [Trait("PR", "1927")]
        [Trait("Bug", "683")]
        [InlineData("/foo/bar/{everything}", "/bar/{everything}/{everything}", "foo", "DownstreamPathTemplate '/bar/{everything}/{everything}' has duplicated placeholder")]
        [InlineData("/foo/bar/{everything}/{everything}", "/bar/{everything}/{everything}", "foo", "UpstreamPathTemplate '/foo/bar/{everything}/{everything}' has duplicated placeholder", "DownstreamPathTemplate '/bar/{everything}/{everything}' has duplicated placeholder")]
        public async Task Configuration_is_invalid_when_placeholder_is_used_twice_in_downstream_path_template(string upstream, string downstream, string host, params string[] expected)
        {
            // Arrange
            var route = GivenDefaultRoute(upstream, downstream, host);
            GivenAConfiguration(route);

            // Act
            await WhenIValidateTheConfiguration();

            // Assert
            ThenTheResultIsNotValid();
            ThenTheErrorMessagesAre(expected);
        }

        private static FileRoute GivenDefaultRoute() => GivenDefaultRoute(null, null, null);
        private static FileRoute GivenDefaultRoute(string upstream, string downstream) => GivenDefaultRoute(upstream, downstream, null);

        private static FileRoute GivenDefaultRoute(string upstream, string downstream, string host) => new()
        {
            UpstreamHttpMethod = new() { HttpMethods.Get },
            UpstreamPathTemplate = upstream ?? "/asdf/",
            DownstreamPathTemplate = downstream ?? "/api/products/",
            DownstreamHostAndPorts = new()
            {
                new(host ?? "bbc.co.uk", 12345),
            },
            DownstreamScheme = Uri.UriSchemeHttp,
        };

        private static FileRoute GivenServiceDiscoveryRoute() => new()
        {
            UpstreamHttpMethod = new() { HttpMethods.Get },
            UpstreamPathTemplate = "/laura",
            DownstreamPathTemplate = "/",
            DownstreamScheme = Uri.UriSchemeHttp,
            ServiceName = "test",
        };

        private static FileRoute GivenRouteWithUpstreamHeaderTemplates(string upstream, string downstream, Dictionary<string, string> templates) => new()
        {
            UpstreamPathTemplate = upstream,
            DownstreamPathTemplate = downstream,
            DownstreamHostAndPorts = new()
            {
                new("bbc.co.uk", 123),
            },
            UpstreamHttpMethod = new() { HttpMethods.Get },
            UpstreamHeaderTemplates = templates,
        };

        private void GivenAConfiguration(FileConfiguration fileConfiguration) => _fileConfiguration = fileConfiguration;

        private FileConfiguration GivenAConfiguration(params FileRoute[] routes)
        {
            var config = new FileConfiguration();
            config.Routes.AddRange(routes);
            _fileConfiguration = config;
            return config;
        }

        private static FileServiceDiscoveryProvider GivenDefaultServiceDiscoveryProvider() => new()
        {
            Scheme = Uri.UriSchemeHttps,
            Host = "localhost",
            Type = "ServiceFabric",
            Port = 8500,
        };

        private async Task WhenIValidateTheConfiguration()
            => _result = await _configurationValidator.IsValid(_fileConfiguration);

        private void ThenTheResultIsValid()
            => _result.Data.IsError.ShouldBeFalse();

        private void ThenTheResultIsNotValid()
            => _result.Data.IsError.ShouldBeTrue();

        private void ThenTheErrorIs<T>()
            => _result.Data.Errors[0].ShouldBeOfType<T>();

        private void ThenTheErrorMessageAtPositionIs(int index, string expected)
            => _result.Data.Errors[index].Message.ShouldBe(expected);

        private void ThenThereAreErrors(bool isError)
            => _result.Data.IsError.ShouldBe(isError);

        private void ThenTheErrorMessagesAre(IEnumerable<string> messages)
        {
            _result.Data.Errors.Count.ShouldBe(messages.Count());

            foreach (var msg in messages)
            {
                _result.Data.Errors.ShouldContain(e => e.Message == msg);
            }
        }

        private void GivenTheAuthSchemeExists(string name)
        {
            _authProvider.Setup(x => x.GetAllSchemesAsync()).ReturnsAsync(new List<AuthenticationScheme>
            {
                new(name, name, typeof(TestHandler)),
            });
        }

        private void GivenAQoSHandler()
        {
            DelegatingHandler Del(DownstreamRoute a, IHttpContextAccessor b, IOcelotLoggerFactory c) => new FakeDelegatingHandler();
            _services.AddSingleton((QosDelegatingHandlerDelegate)Del);
            _provider = _services.BuildServiceProvider();
            _configurationValidator = new FileConfigurationFluentValidator(_provider, new RouteFluentValidator(_authProvider.Object, new HostAndPortValidator(), new FileQoSOptionsFluentValidator(_provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(_provider)));
        }

        private void GivenAServiceDiscoveryHandler()
        {
            ServiceDiscoveryFinderDelegate del = (a, b, c) => new FakeServiceDiscoveryProvider();
            _services.AddSingleton(del);
            _provider = _services.BuildServiceProvider();
            _configurationValidator = new FileConfigurationFluentValidator(_provider, new RouteFluentValidator(_authProvider.Object, new HostAndPortValidator(), new FileQoSOptionsFluentValidator(_provider)), new FileGlobalConfigurationFluentValidator(new FileQoSOptionsFluentValidator(_provider)));
        }

        private class FakeServiceDiscoveryProvider : IServiceDiscoveryProvider
        {
            public Task<List<Service>> GetAsync() => Task.FromResult<List<Service>>(new());
        }

        private class TestOptions : AuthenticationSchemeOptions { }

        private class TestHandler : AuthenticationHandler<TestOptions>
        {
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/8.0/isystemclock-obsolete
            // .NET 8.0: TimeProvider is now a settable property on the Options classes for the authentication and identity components.
            // It can be set directly or by registering a provider in the dependency injection container.
#if NET8_0_OR_GREATER
            public TestHandler(IOptionsMonitor<TestOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
            { }
#else
            public TestHandler(IOptionsMonitor<TestOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
            { }
#endif

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var principal = new ClaimsPrincipal();
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), Scheme.Name)));
            }
        }
    }
}
