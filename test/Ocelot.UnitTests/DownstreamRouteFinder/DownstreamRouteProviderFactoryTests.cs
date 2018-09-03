namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Builder;
    using Ocelot.DownstreamRouteFinder.Finder;
    using Ocelot.DownstreamRouteFinder.UrlMatcher;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using Ocelot.Configuration.Creator;
    using Ocelot.Logging;

    public class DownstreamRouteProviderFactoryTests
    {
        private readonly DownstreamRouteProviderFactory _factory;
        private IInternalConfiguration _config;
        private IDownstreamRouteProvider _result;
        private Mock<IOcelotLogger> _logger;
        private Mock<IOcelotLoggerFactory> _loggerFactory;

        public DownstreamRouteProviderFactoryTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
            services.AddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            services.AddSingleton<IQoSOptionsCreator, QoSOptionsCreator>();
            services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteFinder>();
            services.AddSingleton<IDownstreamRouteProvider, DownstreamRouteCreator>();
            var provider = services.BuildServiceProvider();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _loggerFactory.Setup(x => x.CreateLogger<DownstreamRouteProviderFactory>()).Returns(_logger.Object);
            _factory = new DownstreamRouteProviderFactory(provider, _loggerFactory.Object);
        }

        [Fact]
        public void should_return_downstream_route_finder()
        {
            var reRoutes = new List<ReRoute>
            {
                new ReRouteBuilder().Build()
            };

            this.Given(_ => GivenTheReRoutes(reRoutes))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<DownstreamRouteFinder>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_finder_when_not_dynamic_re_route_and_service_discovery_on()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("test").WithPort(50).WithType("test").Build();
            var reRoutes = new List<ReRoute>
            {
                new ReRouteBuilder().WithUpstreamPathTemplate(new UpstreamPathTemplateBuilder().WithOriginalValue("woot").Build()).Build()
            };

            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<DownstreamRouteFinder>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_finder_as_no_service_discovery_given_no_host()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("").WithPort(50).Build();
            var reRoutes = new List<ReRoute>();

            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<DownstreamRouteFinder>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_finder_given_no_service_discovery_port()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("localhost").WithPort(0).Build();
            var reRoutes = new List<ReRoute>();

            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<DownstreamRouteFinder>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_finder_given_no_service_discovery_type()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("localhost").WithPort(50).WithType("").Build();
            var reRoutes = new List<ReRoute>();

            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<DownstreamRouteFinder>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_creator()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("test").WithPort(50).WithType("test").Build();
            var reRoutes = new List<ReRoute>();

            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<DownstreamRouteCreator>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_creator_with_dynamic_re_route()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("test").WithPort(50).WithType("test").Build();
            var reRoutes = new List<ReRoute>
            {
                new ReRouteBuilder().Build()
            };

            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<DownstreamRouteCreator>())
                .BDDfy();
        }

        private void ThenTheResultShouldBe<T>()
        {
            _result.ShouldBeOfType<T>();
        }

        private void WhenIGet()
        {
            _result = _factory.Get(_config);
        }

        private void GivenTheReRoutes(List<ReRoute> reRoutes)
        {
            _config = new InternalConfiguration(reRoutes, "", null, "", new LoadBalancerOptionsBuilder().Build(), "", new QoSOptionsBuilder().Build(), new HttpHandlerOptionsBuilder().Build());
        }

        private void GivenTheReRoutes(List<ReRoute> reRoutes, ServiceProviderConfiguration config)
        {
            _config = new InternalConfiguration(reRoutes, "", config, "", new LoadBalancerOptionsBuilder().Build(), "", new QoSOptionsBuilder().Build(), new HttpHandlerOptionsBuilder().Build());
        }
    }
}
