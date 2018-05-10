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
    public class DownstreamRouteProviderFactoryTests
    {
        private DownstreamRouteProviderFactory _factory;

        public DownstreamRouteProviderFactoryTests()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IPlaceholderNameAndValueFinder, UrlPathPlaceholderNameAndValueFinder>();
            services.AddSingleton<IUrlPathToUrlTemplateMatcher, RegExUrlMatcher>();
            services.AddSingleton<IDownstreamRouteProvider, Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteProvider>();
            services.AddSingleton<IDownstreamRouteProvider, Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteCreator>();
            var provider = services.BuildServiceProvider();
            _factory = new DownstreamRouteProviderFactory(provider);
        }

        //todo - bddfy

        [Fact]
        public void should_return_downstream_route_finder()
        {
            var reRoutes = new List<ReRoute>
            {
                new ReRouteBuilder().Build()
            };
            IInternalConfiguration config = new InternalConfiguration(reRoutes, "", null, "");
            var result = _factory.Get(config);
            result.ShouldBeOfType<Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteProvider>();
        }

        [Fact]
        public void should_return_downstream_route_finder_as_no_service_discovery()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().Build();
            var reRoutes = new List<ReRoute>
            {
            };
            IInternalConfiguration config = new InternalConfiguration(reRoutes, "", spConfig, "");
            var result = _factory.Get(config);
            result.ShouldBeOfType<Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteProvider>();
        }

        [Fact]
        public void should_return_downstream_route_creator()
        {
            var spConfig = new ServiceProviderConfigurationBuilder().WithHost("test").WithPort(50).Build();
            var reRoutes = new List<ReRoute>
            {
            };
            IInternalConfiguration config = new InternalConfiguration(reRoutes, "", spConfig, "");
            var result = _factory.Get(config);
            result.ShouldBeOfType<Ocelot.DownstreamRouteFinder.Finder.DownstreamRouteCreator>();
        }
    }
}
