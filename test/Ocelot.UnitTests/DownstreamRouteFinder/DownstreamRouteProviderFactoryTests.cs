using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.Finder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Responses;
using Ocelot.Values;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    using Ocelot.Configuration.Creator;

    public class DownstreamRouteProviderFactoryTests
    {
        private readonly DownstreamRouteProviderFactory _factory;
        private IInternalConfiguration _config;
        private IDownstreamRouteProvider _result;

        public DownstreamRouteProviderFactoryTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
            services.AddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            services.AddSingleton<IQoSOptionsCreator, QoSOptionsCreator>();
            services.AddSingleton<IDownstreamRouteProvider, Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder>();
            services.AddSingleton<IDownstreamRouteProvider, Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteCreator>();
            var provider = services.BuildServiceProvider();
            _factory = new DownstreamRouteProviderFactory(provider);
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
                .Then(_ => ThenTheResultShouldBe<Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_finder_as_no_service_discovery()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().Build();
            var reRoutes = new List<ReRoute>
            {
            };

            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteFinder>())
                .BDDfy();
        }

        [Fact]
        public void should_return_downstream_route_creator()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("test").WithPort(50).Build();
            var reRoutes = new List<ReRoute>
            {
            };
            this.Given(_ => GivenTheReRoutes(reRoutes, spConfig))
                .When(_ => WhenIGet())
                .Then(_ => ThenTheResultShouldBe<Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteCreator>())
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
