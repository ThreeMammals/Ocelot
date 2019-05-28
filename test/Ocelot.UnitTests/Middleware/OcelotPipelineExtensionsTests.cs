namespace Ocelot.UnitTests.Middleware
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.DependencyInjection;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.DownstreamUrlCreator.Middleware;
    using Ocelot.LoadBalancer.Middleware;
    using Ocelot.Middleware;
    using Ocelot.Middleware.Pipeline;
    using Ocelot.Request.Middleware;
    using Ocelot.WebSockets.Middleware;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class OcelotPipelineExtensionsTests
    {
        private OcelotPipelineBuilder _builder;
        private OcelotRequestDelegate _handlers;

        [Fact]
        public void should_set_up_pipeline()
        {
            this.Given(_ => GivenTheDepedenciesAreSetUp())
                 .When(_ => WhenIBuild())
                 .Then(_ => ThenThePipelineIsBuilt())
                 .BDDfy();
        }

        [Fact]
        public void should_expand_pipeline()
        {
            this.Given(_ => GivenTheDepedenciesAreSetUp())
                 .When(_ => WhenIExpandBuild())
                 .Then(_ => ThenThePipelineIsBuilt())
                 .BDDfy();
        }

        private void ThenThePipelineIsBuilt()
        {
            _handlers.ShouldNotBeNull();
        }

        private void WhenIBuild()
        {
            _handlers = _builder.BuildOcelotPipeline(new OcelotPipelineConfiguration());
        }

        private void WhenIExpandBuild()
        {
            OcelotPipelineConfiguration configuration = new OcelotPipelineConfiguration();
            configuration.MapWhenOcelotPipeline.Add((app) =>
            {
                app.UseDownstreamRouteFinderMiddleware();
                app.UseDownstreamRequestInitialiser();
                app.UseLoadBalancingMiddleware();
                app.UseDownstreamUrlCreatorMiddleware();
                app.UseWebSocketsProxyMiddleware();

                return context => context.HttpContext.WebSockets.IsWebSocketRequest;
            });
            _handlers = _builder.BuildOcelotPipeline(new OcelotPipelineConfiguration());
        }

        private void GivenTheDepedenciesAreSetUp()
        {
            IConfigurationBuilder test = new ConfigurationBuilder();
            var root = test.Build();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(root);
            services.AddOcelot();
            var provider = services.BuildServiceProvider();
            _builder = new OcelotPipelineBuilder(provider);
        }
    }
}
