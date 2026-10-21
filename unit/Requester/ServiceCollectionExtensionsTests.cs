using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Requester;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Requester;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOcelotRequester_Default_RegistersDefaultImplementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOcelotRequester();

        // Assert
        AssertSingleton<IHttpRequester, MessageInvokerHttpRequester>(services);
        AssertSingleton<IMessageInvokerPool, MessageInvokerPool>(services);
    }

    [Fact]
    public void AddOcelotRequester_GenericPool_RegistersCustomPoolAndDefaultRequester()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOcelotRequester<FakeMessageInvokerPool>();

        // Assert
        AssertSingleton<IHttpRequester, MessageInvokerHttpRequester>(services);
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool>(services);
    }

    [Fact]
    public void AddOcelotRequester_GenericBoth_RegistersCustomTypes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOcelotRequester<FakeHttpRequester, FakeMessageInvokerPool>();

        // Assert
        AssertSingleton<IHttpRequester, FakeHttpRequester>(services);
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool>(services);
    }

    [Fact]
    public void AddOcelotRequester_GenericPool_RemovesExistingRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpRequester, FakeHttpRequester>();
        services.AddSingleton<IMessageInvokerPool, FakeMessageInvokerPool>();

        // Act
        services.AddOcelotRequester<FakeMessageInvokerPool2>();

        // Assert
        Assert.Single(services.Where(d => d.ServiceType == typeof(IHttpRequester)));
        Assert.Single(services.Where(d => d.ServiceType == typeof(IMessageInvokerPool)));

        AssertSingleton<IHttpRequester, MessageInvokerHttpRequester>(services);
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool2>(services);
    }

    [Fact]
    public void AddOcelotRequester_GenericBoth_RemovesExistingRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpRequester, FakeHttpRequester>();
        services.AddSingleton<IMessageInvokerPool, FakeMessageInvokerPool>();

        // Act
        services.AddOcelotRequester<FakeHttpRequester2, FakeMessageInvokerPool2>();

        // Assert
        Assert.Single(services.Where(d => d.ServiceType == typeof(IHttpRequester)));
        Assert.Single(services.Where(d => d.ServiceType == typeof(IMessageInvokerPool)));

        AssertSingleton<IHttpRequester, FakeHttpRequester2>(services);
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool2>(services);
    }

    [Fact]
    public void AddOcelotRequester_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result1 = services.AddOcelotRequester();
        var result2 = services.AddOcelotRequester<FakeMessageInvokerPool>();
        var result3 = services.AddOcelotRequester<FakeHttpRequester, FakeMessageInvokerPool>();

        // Assert
        Assert.Same(services, result1);
        Assert.Same(services, result2);
        Assert.Same(services, result3);
    }

    private static void AssertSingleton<TService, TImplementation>(IServiceCollection services)
        where TImplementation : class, TService
    {
        var descriptor = services.SingleOrDefault(d =>
            d.ServiceType == typeof(TService) &&
            d.ImplementationType == typeof(TImplementation));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    // Minimal stub implementations (only used for registration checks)
    private sealed class FakeHttpRequester : IHttpRequester
    {
        public Task<Response<HttpResponseMessage>> GetResponse(HttpContext context) =>
            throw new NotImplementedException();
    }

    private sealed class FakeHttpRequester2 : IHttpRequester
    {
        public Task<Response<HttpResponseMessage>> GetResponse(HttpContext context) =>
            throw new NotImplementedException();
    }

    private sealed class FakeMessageInvokerPool : IMessageInvokerPool
    {
        public HttpMessageInvoker Get(DownstreamRoute downstreamRoute) =>
            throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();
    }

    private sealed class FakeMessageInvokerPool2 : IMessageInvokerPool
    {
        public HttpMessageInvoker Get(DownstreamRoute downstreamRoute) =>
            throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();
    }
}
