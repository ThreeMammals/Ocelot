using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.LoadBalancer;
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
        // Arrange
        GivenTheDepedenciesAreSetUp();

        // Act
        _handlers = _builder.BuildOcelotPipeline(new OcelotPipelineConfiguration());

        // Assert
        _handlers.ShouldNotBeNull();
    }

    [Fact]
    public void Should_expand_pipeline()
    {
        // Arrange
        GivenTheDepedenciesAreSetUp();
        var configuration = new OcelotPipelineConfiguration();
        configuration.MapWhenOcelotPipeline.Add((httpContext) => httpContext.WebSockets.IsWebSocketRequest, app =>
        {
            app.UseMiddleware<DownstreamRouteFinderMiddleware>();
            app.UseMiddleware<DownstreamRequestInitialiserMiddleware>();
            app.UseMiddleware<LoadBalancingMiddleware>();
            app.UseMiddleware<DownstreamUrlCreatorMiddleware>();
            app.UseMiddleware<WebSocketsProxyMiddleware>();
        });

        // Act
        _handlers = _builder.BuildOcelotPipeline(new OcelotPipelineConfiguration());

        // Assert
        _handlers.ShouldNotBeNull();
    }

    private void GivenTheDepedenciesAreSetUp()
    {
        var root = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(root);
        services.AddOcelot();
        var provider = services.BuildServiceProvider(true);
        _builder = new ApplicationBuilder(provider);
    }
}
