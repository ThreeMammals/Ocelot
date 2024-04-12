using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Values;

namespace Ocelot.UnitTests.DownstreamUrlCreator
{
    public class DownstreamUrlCreatorMiddlewareTests : UnitTest
    {
        private readonly Mock<IDownstreamPathPlaceholderReplacer> _downstreamUrlTemplateVariableReplacer;
        private OkResponse<DownstreamPath> _downstreamPath;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private DownstreamUrlCreatorMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly HttpRequestMessage _request;
        private readonly HttpContext _httpContext;
        private readonly Mock<IRequestScopedDataRepository> _repo;

        public DownstreamUrlCreatorMiddlewareTests()
        {
            _repo = new Mock<IRequestScopedDataRepository>();
            _httpContext = new DefaultHttpContext();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<DownstreamUrlCreatorMiddleware>()).Returns(_logger.Object);
            _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamPathPlaceholderReplacer>();
            _request = new HttpRequestMessage(HttpMethod.Get, "https://my.url/abc/?q=123");
            _next = context => Task.CompletedTask;
        }

        [Fact]
        public void Should_replace_scheme_and_path()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithDownstreamScheme("https")
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                    new List<PlaceholderNameAndValue>(),
                    new RouteBuilder()
                        .WithDownstreamRoute(downstreamRoute)
                        .WithUpstreamHttpMethod(new List<string> { "Get" })
                        .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://my.url/abc?q=123"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("/api/products/1"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://my.url:80/api/products/1?q=123"))
                .And(x => ThenTheQueryStringIs("?q=123"))
                .BDDfy();
        }

        [Fact]
        public void Should_replace_query_string()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithDownstreamScheme("https")
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new("{subscriptionId}", "1"),
                            new("{unitId}", "2"),
                        },
                        new RouteBuilder()
                            .WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?unitId=2"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("api/units/1/2/updates"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates"))
                .And(x => ThenTheQueryStringIs(string.Empty))
                .BDDfy();
        }

        [Fact]
        public void Should_replace_query_string_but_leave_non_placeholder_queries()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithDownstreamScheme("https")
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new("{subscriptionId}", "1"),
                            new("{unitId}", "2"),
                        },
                        new RouteBuilder()
                            .WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?unitId=2&productId=2")) // unitId is the first
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("api/units/1/2/updates"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates?productId=2"))
                .And(x => ThenTheQueryStringIs("?productId=2"))
                .BDDfy();
        }

        [Fact]
        public void Should_replace_query_string_but_leave_non_placeholder_queries_2()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithDownstreamScheme("https")
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new PlaceholderNameAndValue("{subscriptionId}", "1"),
                            new PlaceholderNameAndValue("{unitId}", "2"),
                        },
                        new RouteBuilder()
                            .WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?productId=2&unitId=2")) // unitId is the second
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("api/units/1/2/updates"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates?productId=2"))
                .And(x => ThenTheQueryStringIs("?productId=2"))
                .BDDfy();
        }

        [Fact]
        public void Should_replace_query_string_exact_match()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamPathTemplate("/api/units/{subscriptionId}/{unitId}/updates/{unitIdIty}")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithDownstreamScheme("https")
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new("{subscriptionId}", "1"),
                            new("{unitId}", "2"),
                            new("{unitIdIty}", "3"),
                        },
                        new RouteBuilder()
                            .WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:5000/api/subscriptions/1/updates?unitId=2?unitIdIty=3"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("api/units/1/2/updates/3"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://localhost:5000/api/units/1/2/updates/3"))
                .And(x => ThenTheQueryStringIs(string.Empty))
                .BDDfy();
        }

        [Fact]
        public void Should_not_create_service_fabric_url()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .WithDownstreamScheme("https")
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>(),
                        new RouteBuilder()
                            .WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://my.url/abc?q=123"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("/api/products/1"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://my.url:80/api/products/1?q=123"))
                .BDDfy();
        }

        [Fact]
        public void Should_create_service_fabric_url()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamScheme("http")
                .WithServiceName("Ocelot/OcelotApp")
                .WithUseServiceDiscovery(true)
                .Build();

            var downstreamRouteHolder = new DownstreamRouteHolder(
                new List<PlaceholderNameAndValue>(),
                new RouteBuilder()
                    .WithDownstreamRoute(downstreamRoute)
                    .Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRouteHolder))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:19081"))
                .And(x => x.GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1"))
                .BDDfy();
        }

        [Fact]
        public void Should_create_service_fabric_url_with_query_string_for_stateless_service()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamScheme("http")
                .WithServiceName("Ocelot/OcelotApp")
                .WithUseServiceDiscovery(true)
                .Build();

            var downstreamRouteHolder = new DownstreamRouteHolder(
                new List<PlaceholderNameAndValue>(),
                new RouteBuilder()
                    .WithDownstreamRoute(downstreamRoute)
                    .Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRouteHolder))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:19081?Tom=test&laura=1"))
                .And(x => x.GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1?Tom=test&laura=1"))
                .BDDfy();
        }

        [Fact]
        public void Should_create_service_fabric_url_with_query_string_for_stateful_service()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamScheme("http")
                .WithServiceName("Ocelot/OcelotApp")
                .WithUseServiceDiscovery(true)
                .Build();

            var downstreamRouteHolder = new DownstreamRouteHolder(
                new List<PlaceholderNameAndValue>(),
                new RouteBuilder()
                    .WithDownstreamRoute(downstreamRoute)
                    .Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRouteHolder))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:19081?PartitionKind=test&PartitionKey=1"))
                .And(x => x.GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1?PartitionKind=test&PartitionKey=1"))
                .BDDfy();
        }

        [Fact]
        public void Should_create_service_fabric_url_with_version_from_upstream_path_template()
        {
            var downstreamRoute = new DownstreamRouteHolder(
                new List<PlaceholderNameAndValue>(),
                new RouteBuilder().WithDownstreamRoute(
                        new DownstreamRouteBuilder()
                            .WithDownstreamScheme("http")
                            .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue("/products").Build())
                            .WithUseServiceDiscovery(true)
                            .Build()
                    ).Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:19081?PartitionKind=test&PartitionKey=1"))
                .And(x => x.GivenTheUrlReplacerWillReturnSequence("/products", "Service_1.0/Api"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:19081/Service_1.0/Api/products?PartitionKind=test&PartitionKey=1"))
                .BDDfy();
        }

        [Fact(DisplayName = "473: " + nameof(Should_not_remove_additional_query_parameter_when_placeholder_and_parameter_names_are_different))]
        public void Should_not_remove_additional_query_parameter_when_placeholder_and_parameter_names_are_different()
        {
            var methods = new List<string> { "Post", "Get" };
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                    .WithOriginalValue("/uc/Authorized/{servak}/{action}").Build())
                .WithDownstreamPathTemplate("/Authorized/{action}?server={servak}")
                .WithUpstreamHttpMethod(methods)
                .WithDownstreamScheme(Uri.UriSchemeHttp)
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new("{action}", "1"),
                            new("{servak}", "2"),
                        },
                        new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(methods)
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:5000/uc/Authorized/2/1/refresh?refreshToken=123456789"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("/Authorized/1?server=2"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:5000/Authorized/1?refreshToken=123456789&server=2"))
                .And(x => ThenTheQueryStringIs("?refreshToken=123456789&server=2"))
                .BDDfy();
        }

        [Fact]
        public void Should_not_replace_by_empty_scheme()
        {
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithDownstreamScheme(string.Empty)
                .WithServiceName("Ocelot/OcelotApp")
                .WithUseServiceDiscovery(true)
                .Build();

            var downstreamRouteHolder = new DownstreamRouteHolder(
                new List<PlaceholderNameAndValue>(),
                new RouteBuilder()
                    .WithDownstreamRoute(downstreamRoute)
                    .Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRouteHolder))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("https://localhost:19081?PartitionKind=test&PartitionKey=1"))
                .And(x => x.GivenTheUrlReplacerWillReturnSequence("/api/products/1", "Ocelot/OcelotApp"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("https://localhost:19081/Ocelot/OcelotApp/api/products/1?PartitionKind=test&PartitionKey=1"))
                .BDDfy();
        }

        [Fact(DisplayName = "952: " + nameof(Should_map_query_parameters_with_different_names))]
        public void Should_map_query_parameters_with_different_names()
        {
            var methods = new List<string> { "Post", "Get" };
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                    .WithOriginalValue("/users?userId={userId}").Build())
                .WithDownstreamPathTemplate("/persons?personId={userId}")
                .WithUpstreamHttpMethod(methods)
                .WithDownstreamScheme(Uri.UriSchemeHttp)
                .Build();
            var config = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new("{userId}", "webley"),
                        },
                        new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(methods)
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs($"http://localhost:5000/users?userId=webley"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("/persons?personId=webley"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs($"http://localhost:5000/persons?personId=webley"))
                .And(x => ThenTheQueryStringIs($"?personId=webley"))
                .BDDfy();
        }

        [Fact(DisplayName = "952: " + nameof(Should_map_query_parameters_with_different_names_and_save_old_param_if_placeholder_and_param_names_differ))]
        public void Should_map_query_parameters_with_different_names_and_save_old_param_if_placeholder_and_param_names_differ()
        {
            var methods = new List<string> { "Post", "Get" };
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                    .WithOriginalValue("/users?userId={uid}").Build())
                .WithDownstreamPathTemplate("/persons?personId={uid}")
                .WithUpstreamHttpMethod(methods)
                .WithDownstreamScheme(Uri.UriSchemeHttp)
                .Build();
            var config = new ServiceProviderConfigurationBuilder().Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new("{uid}", "webley"),
                        },
                        new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(methods)
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs($"http://localhost:5000/users?userId=webley"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn("/persons?personId=webley"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs($"http://localhost:5000/persons?personId=webley&userId=webley"))
                .And(x => ThenTheQueryStringIs($"?personId=webley&userId=webley"))
                .BDDfy();
        }

        [Theory(DisplayName = "1174: " + nameof(Should_forward_query_parameters_without_duplicates))]
        [InlineData("projectNumber=45&startDate=2019-12-12&endDate=2019-12-12", "endDate=2019-12-12&projectNumber=45&startDate=2019-12-12")]
        [InlineData("$filter=ProjectNumber eq 45 and DateOfSale ge 2020-03-01T00:00:00z and DateOfSale le 2020-03-15T00:00:00z", "$filter=ProjectNumber eq 45 and DateOfSale ge 2020-03-01T00:00:00z and DateOfSale le 2020-03-15T00:00:00z")]
        public void Should_forward_query_parameters_without_duplicates(string everythingelse, string expectedOrdered)
        {
            var methods = new List<string> { "Get" };
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                    .WithOriginalValue("/contracts?{everythingelse}").Build())
                .WithDownstreamPathTemplate("/api/contracts?{everythingelse}")
                .WithUpstreamHttpMethod(methods)
                .WithDownstreamScheme(Uri.UriSchemeHttp)
                .Build();
            var config = new ServiceProviderConfigurationBuilder().Build();
            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new("{everythingelse}", everythingelse),
                        },
                        new RouteBuilder().WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(methods)
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs($"http://localhost:5000//contracts?{everythingelse}"))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn($"/api/contracts?{everythingelse}"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs($"http://localhost:5000/api/contracts?{expectedOrdered}"))
                .And(x => ThenTheQueryStringIs($"?{expectedOrdered}"))
                .BDDfy();
        }

        [Theory]
        [Trait("Bug", "748")]
        [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/123", "{url}", "123", "/api/v1/test/123", "")]
        [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/123?query=1", "{url}", "123", "/api/v1/test/123?query=1", "?query=1")]
        [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/?query=1", "{url}", "", "/api/v1/test/?query=1", "?query=1")]
        [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1?query=1", "{url}", "", "/api/v1/test?query=1", "?query=1")]
        [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1/", "{url}", "", "/api/v1/test/", "")]
        [InlineData("/test/{version}/{url}", "/api/{version}/test/{url}", "/test/v1", "{url}", "", "/api/v1/test", "")]
        public void should_fix_issue_748(string upstreamTemplate, string downstreamTemplate, string requestURL, string placeholderName, string placeholderValue, string downstreamURI, string queryString)
        {
            var methods = new List<string> { "Get" };
            var downstreamRoute = new DownstreamRouteBuilder()
                .WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder()
                    .WithOriginalValue(upstreamTemplate).Build())
                .WithDownstreamPathTemplate(downstreamTemplate)
                .WithUpstreamHttpMethod(methods)
                .WithDownstreamScheme(Uri.UriSchemeHttp)
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRouteHolder(
                        new List<PlaceholderNameAndValue>
                        {
                            new(placeholderName, placeholderValue),
                            new("{version}", "v1"),
                        },
                        new RouteBuilder()
                            .WithDownstreamRoute(downstreamRoute)
                            .WithUpstreamHttpMethod(new List<string> { "Get" })
                            .Build())))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:5000" + requestURL))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheUrlReplacerWillReturn(downstreamURI))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:5000" + downstreamURI))
                .And(x => ThenTheQueryStringIs(queryString))
                .BDDfy();
        }

        private void GivenTheServiceProviderConfigIs(ServiceProviderConfiguration config)
        {
            var configuration = new InternalConfiguration(null, null, config, null, null, null, null, null, null);
            _httpContext.Items.SetIInternalConfiguration(configuration);
        }

        private void WhenICallTheMiddleware()
        {
            _middleware = new DownstreamUrlCreatorMiddleware(_next, _loggerFactory.Object, _downstreamUrlTemplateVariableReplacer.Object);
            _middleware.Invoke(_httpContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRouteHolder downstreamRoute)
        {
            _httpContext.Items.UpsertTemplatePlaceholderNameAndValues(downstreamRoute.TemplatePlaceholderNameAndValues);

            _httpContext.Items.UpsertDownstreamRoute(downstreamRoute.Route.DownstreamRoute[0]);
        }

        private void GivenTheDownstreamRequestUriIs(string uri)
        {
            _request.RequestUri = new Uri(uri);
            _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(_request));
        }

        private void GivenTheUrlReplacerWillReturnSequence(params string[] paths)
        {
            var setup = _downstreamUrlTemplateVariableReplacer
                .SetupSequence(x => x.Replace(It.IsAny<string>(), It.IsAny<List<PlaceholderNameAndValue>>()));
            foreach (var path in paths)
            {
                var response = new OkResponse<DownstreamPath>(new DownstreamPath(path));
                setup.Returns(response);
            }
        }

        private void GivenTheUrlReplacerWillReturn(string path)
        {
            _downstreamPath = new OkResponse<DownstreamPath>(new DownstreamPath(path));
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.Replace(It.IsAny<string>(), It.IsAny<List<PlaceholderNameAndValue>>()))
                .Returns(_downstreamPath);
        }

        private void ThenTheDownstreamRequestUriIs(string expectedUri)
        {
            _httpContext.Items.DownstreamRequest().ToHttpRequestMessage().RequestUri.OriginalString.ShouldBe(expectedUri);
        }

        private void ThenTheQueryStringIs(string queryString)
        {
            _httpContext.Items.DownstreamRequest().Query.ShouldBe(queryString);
        }
    }
}
