namespace Ocelot.UnitTests.DownstreamUrlCreator
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Moq;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Ocelot.DownstreamUrlCreator.Middleware;
    using Ocelot.DownstreamUrlCreator.UrlTemplateReplacer;
    using Ocelot.Logging;
    using Ocelot.Responses;
    using Ocelot.Values;
    using TestStack.BDDfy;
    using Xunit;
    using Shouldly;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Request.Middleware;
    using Ocelot.Configuration;
    using Ocelot.Middleware;

    public class DownstreamUrlCreatorMiddlewareTests
    {
        private readonly Mock<IDownstreamPathPlaceholderReplacer> _downstreamUrlTemplateVariableReplacer;
        private OkResponse<DownstreamPath> _downstreamPath;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private DownstreamUrlCreatorMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private readonly OcelotRequestDelegate _next;
        private readonly HttpRequestMessage _request;

        public DownstreamUrlCreatorMiddlewareTests()
        {
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<DownstreamUrlCreatorMiddleware>()).Returns(_logger.Object);
            _downstreamUrlTemplateVariableReplacer = new Mock<IDownstreamPathPlaceholderReplacer>();
            _request = new HttpRequestMessage(HttpMethod.Get, "https://my.url/abc/?q=123");
            _downstreamContext.DownstreamRequest = new DownstreamRequest(_request);
            _next = context => Task.CompletedTask;
        }

        [Fact]
        public void should_replace_scheme_and_path()
        {
            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamPathTemplate("any old string")
                .WithUpstreamHttpMethod(new List<string> {"Get"})
                .WithDownstreamScheme("https")
                .Build();

            var config = new ServiceProviderConfigurationBuilder()
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(
                    new DownstreamRoute(
                    new List<PlaceholderNameAndValue>(), 
                    new ReRouteBuilder()
                        .WithDownstreamReRoute(downstreamReRoute)
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
        public void should_not_create_service_fabric_url()
        {
            var downstreamReRoute = new DownstreamReRouteBuilder()
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
                    new DownstreamRoute(
                        new List<PlaceholderNameAndValue>(),
                        new ReRouteBuilder()
                            .WithDownstreamReRoute(downstreamReRoute)
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
        public void should_create_service_fabric_url()
        {
            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamScheme("http")
                .WithServiceName("Ocelot/OcelotApp")
                .WithUseServiceDiscovery(true)
                .Build();

            var downstreamRoute = new DownstreamRoute(
                new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamReRoute(downstreamReRoute)
                    .Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:19081"))
                .And(x => x.GivenTheUrlReplacerWillReturn("/api/products/1"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1?cmd=instance"))
                .BDDfy();
        }

        [Fact]
        public void should_create_service_fabric_url_with_query_string_for_stateless_service()
        {
            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamScheme("http")
                .WithServiceName("Ocelot/OcelotApp")
                .WithUseServiceDiscovery(true)
                .Build();

            var downstreamRoute = new DownstreamRoute(
                new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamReRoute(downstreamReRoute)
                    .Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:19081?Tom=test&laura=1"))
                .And(x => x.GivenTheUrlReplacerWillReturn("/api/products/1"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1?Tom=test&laura=1&cmd=instance"))
                .BDDfy();
        }

        [Fact]
        public void should_create_service_fabric_url_with_query_string_for_stateful_service()
        {
            var downstreamReRoute = new DownstreamReRouteBuilder()
                .WithDownstreamScheme("http")
                .WithServiceName("Ocelot/OcelotApp")
                .WithUseServiceDiscovery(true)
                .Build();

            var downstreamRoute = new DownstreamRoute(
                new List<PlaceholderNameAndValue>(),
                new ReRouteBuilder()
                    .WithDownstreamReRoute(downstreamReRoute)
                    .Build());

            var config = new ServiceProviderConfigurationBuilder()
                .WithType("ServiceFabric")
                .WithHost("localhost")
                .WithPort(19081)
                .Build();

            this.Given(x => x.GivenTheDownStreamRouteIs(downstreamRoute))
                .And(x => GivenTheServiceProviderConfigIs(config))
                .And(x => x.GivenTheDownstreamRequestUriIs("http://localhost:19081?PartitionKind=test&PartitionKey=1"))
                .And(x => x.GivenTheUrlReplacerWillReturn("/api/products/1"))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheDownstreamRequestUriIs("http://localhost:19081/Ocelot/OcelotApp/api/products/1?PartitionKind=test&PartitionKey=1"))
                .BDDfy();
        }

        private void GivenTheServiceProviderConfigIs(ServiceProviderConfiguration config)
        {
            var configuration = new InternalConfiguration(null, null, config, null, null, null, null, null);
            _downstreamContext.Configuration = configuration;
        }

        private void WhenICallTheMiddleware()
        {
            _middleware = new DownstreamUrlCreatorMiddleware(_next, _loggerFactory.Object, _downstreamUrlTemplateVariableReplacer.Object);
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamContext.TemplatePlaceholderNameAndValues = downstreamRoute.TemplatePlaceholderNameAndValues;
            _downstreamContext.DownstreamReRoute = downstreamRoute.ReRoute.DownstreamReRoute[0];
        }

        private void GivenTheDownstreamRequestUriIs(string uri)
        {
            _request.RequestUri = new Uri(uri);
            _downstreamContext.DownstreamRequest = new DownstreamRequest(_request);
        }

        private void GivenTheUrlReplacerWillReturn(string path)
        {
            _downstreamPath = new OkResponse<DownstreamPath>(new DownstreamPath(path));
            _downstreamUrlTemplateVariableReplacer
                .Setup(x => x.Replace(It.IsAny<PathTemplate>(), It.IsAny<List<PlaceholderNameAndValue>>()))
                .Returns(_downstreamPath);
        }

        private void ThenTheDownstreamRequestUriIs(string expectedUri)
        {
            _downstreamContext.DownstreamRequest.ToHttpRequestMessage().RequestUri.OriginalString.ShouldBe(expectedUri);
        }
    }
}
