using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Requester;
using Ocelot.Responses;
using Ocelot.UnitTests.DependencyInjection;

namespace Ocelot.UnitTests.Requester;

public class ServiceCollectionExtensionsTests : DepInjectionUnitTest
{
    [Fact]
    public void AddOcelotRequester_Default_RegistersDefaultImplementations()
    {
        // Arrange, Act
        Services.AddOcelotRequester();

        // Assert
        AssertSingleton<IHttpRequester, MessageInvokerHttpRequester>();
        AssertSingleton<IMessageInvokerPool, MessageInvokerPool>();
    }

    [Fact]
    public void AddOcelotRequester_GenericPool_RegistersCustomPoolAndDefaultRequester()
    {
        // Arrange, Act
        Services.AddOcelotRequester<FakeMessageInvokerPool>();

        // Assert
        AssertSingleton<IHttpRequester, MessageInvokerHttpRequester>();
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool>();
    }

    [Fact]
    public void AddOcelotRequester_GenericBoth_RegistersCustomTypes()
    {
        // Arrange, Act
        Services.AddOcelotRequester<FakeHttpRequester, FakeMessageInvokerPool>();

        // Assert
        AssertSingleton<IHttpRequester, FakeHttpRequester>();
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool>();
    }

    [Fact]
    public void AddOcelotRequester_GenericPool_RemovesExistingRegistrations()
    {
        // Arrange
        Services.AddSingleton<IHttpRequester, FakeHttpRequester>();
        Services.AddSingleton<IMessageInvokerPool, FakeMessageInvokerPool>();

        // Act
        Services.AddOcelotRequester<FakeMessageInvokerPool2>();

        // Assert
        Assert.Single(Services.Where(d => d.ServiceType == typeof(IHttpRequester)));
        Assert.Single(Services.Where(d => d.ServiceType == typeof(IMessageInvokerPool)));

        AssertSingleton<IHttpRequester, MessageInvokerHttpRequester>();
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool2>();
    }

    [Fact]
    public void AddOcelotRequester_GenericBoth_RemovesExistingRegistrations()
    {
        // Arrange
        Services.AddSingleton<IHttpRequester, FakeHttpRequester>();
        Services.AddSingleton<IMessageInvokerPool, FakeMessageInvokerPool>();

        // Act
        Services.AddOcelotRequester<FakeHttpRequester2, FakeMessageInvokerPool2>();

        // Assert
        Assert.Single(Services.Where(d => d.ServiceType == typeof(IHttpRequester)));
        Assert.Single(Services.Where(d => d.ServiceType == typeof(IMessageInvokerPool)));

        AssertSingleton<IHttpRequester, FakeHttpRequester2>();
        AssertSingleton<IMessageInvokerPool, FakeMessageInvokerPool2>();
    }

    [Fact]
    public void AddOcelotRequester_ReturnsSameServiceCollection()
    {
        // Arrange, Act
        var result1 = Services.AddOcelotRequester();
        var result2 = Services.AddOcelotRequester<FakeMessageInvokerPool>();
        var result3 = Services.AddOcelotRequester<FakeHttpRequester, FakeMessageInvokerPool>();

        // Assert
        Assert.Same(Services, result1);
        Assert.Same(Services, result2);
        Assert.Same(Services, result3);
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
