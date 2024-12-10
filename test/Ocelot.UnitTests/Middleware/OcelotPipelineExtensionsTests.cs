using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.WebSockets;

namespace Ocelot.UnitTests.Middleware;

public class OcelotPipelineExtensionsTests : UnitTest
{
    private ApplicationBuilder _builder;
    private RequestDelegate _handlers;

    [Fact]
    public void Should_set_up_pipeline()
    {
        GivenTheDepedenciesAreSetUp();
        WhenIBuild();
        ThenThePipelineIsBuilt();
    }

    [Fact]
    public void Should_expand_pipeline()
    {
        GivenTheDepedenciesAreSetUp();
        WhenIExpandBuild();
        ThenThePipelineIsBuilt();
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
        var configuration = new OcelotPipelineConfiguration();
        configuration.MapWhenOcelotPipeline.Add((httpContext) => httpContext.WebSockets.IsWebSocketRequest, app =>
        {
            app.UseDownstreamRouteFinderMiddleware();
            app.UseDownstreamRequestInitialiser();
            app.UseLoadBalancingMiddleware();
            app.UseDownstreamUrlCreatorMiddleware();
            app.UseWebSocketsProxyMiddleware();
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
        var provider = services.BuildServiceProvider(true);
        _builder = new ApplicationBuilder(provider);
    }
}
