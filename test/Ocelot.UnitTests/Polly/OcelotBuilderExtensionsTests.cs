using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DependencyInjection;
using Ocelot.Errors.QoS;
using Ocelot.Logging;
using Ocelot.Provider.Polly;
using Ocelot.Provider.Polly.Interfaces;
using Ocelot.Requester;
using Polly;

namespace Ocelot.UnitTests.Polly;

public class OcelotBuilderExtensionsTests
{
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory = new();
    private readonly Mock<IHttpContextAccessor> _contextAccessor = new();

    [Fact]
    public void DefaultErrorMapping_CallCreateRequestTimedOutError_IsTypeOfRequestTimedOutError()
    {
        foreach (var kv in OcelotBuilderExtensions.DefaultErrorMapping)
        {
            // Arrange
            var type = kv.Key;
            var mappingFunc = kv.Value;
            object[] args = type.IsGenericType ? [new HttpResponseMessage()] : [];
            var argument = (Exception)Activator.CreateInstance(type, args);

            // Act
            var actual = mappingFunc.Invoke(argument);

            // Assert
            Assert.IsType<RequestTimedOutError>(actual);
        }
    }

    [Fact]
    public void AddPolly_NoParams_ShouldBuild()
    {
        // Arrange
        var provider = GivenServiceProvider(
            ob => ob.AddPolly(),
            out var route);

        // Act, Assert
        var del = provider.GetService<QosDelegatingHandlerDelegate>().ShouldNotBeNull();
        var handler = del(route, _contextAccessor.Object, _loggerFactory.Object).ShouldNotBeNull();
        handler.ShouldBeOfType<PollyResiliencePipelineDelegatingHandler>();
    }

    [Fact]
    public void AddPolly_GenericWithoutParams_ShouldBuild()
    {
        // Arrange
        var provider = GivenServiceProvider(
            ob => ob.AddPolly<MyPollyQoSResiliencePipelineProvider>(),
            out var route);

        // Act, Assert
        var del = provider.GetService<QosDelegatingHandlerDelegate>().ShouldNotBeNull();
        var handler = del(route, _contextAccessor.Object, _loggerFactory.Object).ShouldNotBeNull();
        handler.ShouldBeOfType<PollyResiliencePipelineDelegatingHandler>();
    }

    [Fact]
    public void AddPolly_WithErrorMapping_ShouldBuild()
    {
        // Arrange
        var errorMapping = OcelotBuilderExtensions.DefaultErrorMapping;
        var provider = GivenServiceProvider(
            ob => ob.AddPolly<MyPollyQoSResiliencePipelineProvider>(errorMapping),
            out var route);

        // Act, Assert
        var del = provider.GetService<QosDelegatingHandlerDelegate>().ShouldNotBeNull();
        var handler = del(route, _contextAccessor.Object, _loggerFactory.Object).ShouldNotBeNull();
        handler.ShouldBeOfType<PollyResiliencePipelineDelegatingHandler>();
    }

    [Fact]
    public void AddPolly_WithDelegatingHandler_ShouldBuild()
    {
        // Arrange
        var qosDelegatingHandler = new QosDelegatingHandlerDelegate(GetQosDelegatingHandler);
        var provider = GivenServiceProvider(
            ob => ob.AddPolly<MyPollyQoSResiliencePipelineProvider>(qosDelegatingHandler),
            out var route);

        // Act, Assert
        var del = provider.GetService<QosDelegatingHandlerDelegate>().ShouldNotBeNull();
        var handler = del(route, _contextAccessor.Object, _loggerFactory.Object).ShouldNotBeNull();
        handler.ShouldBeOfType<MyQosDelegatingHandlerFor_AddPolly_WithDelegatingHandler_ShouldBuild>();
    }

    private static DelegatingHandler GetQosDelegatingHandler(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new MyQosDelegatingHandlerFor_AddPolly_WithDelegatingHandler_ShouldBuild();

    private class MyQosDelegatingHandlerFor_AddPolly_WithDelegatingHandler_ShouldBuild : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new Exception("Hello from the fake handler!");
    }

    private class MyPollyQoSResiliencePipelineProvider : IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
    {
        public ResiliencePipeline<HttpResponseMessage> GetResiliencePipeline(DownstreamRoute route) => throw new NotImplementedException();
    }

    private static ServiceProvider GivenServiceProvider(Action<IOcelotBuilder> withAddPolly, out DownstreamRoute route)
    {
        var services = new ServiceCollection();
        var options = new QoSOptions(2, 200)
        {
            Timeout = 100,
        };
        route = new DownstreamRouteBuilder()
            .WithQosOptions(options)
            .Build();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .Build();
        var oBuilder = services.AddOcelot(configuration);
        withAddPolly(oBuilder);
        return services.BuildServiceProvider(true);
    }
}
