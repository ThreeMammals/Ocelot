using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Errors.Middleware;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Reflection;

namespace Ocelot.UnitTests.Middleware;

public class OcelotPiplineBuilderTests : UnitTest
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configRoot;
    private int _counter;
    private readonly DefaultHttpContext _httpContext;

    public OcelotPiplineBuilderTests()
    {
        _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
        _services = new ServiceCollection();
        _services.AddSingleton(GetHostingEnvironment());
        _services.AddSingleton(_configRoot);
        _services.AddOcelot();
        _httpContext = new DefaultHttpContext();
    }

    private static IWebHostEnvironment GetHostingEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment
            .Setup(e => e.ApplicationName)
            .Returns(typeof(OcelotPiplineBuilderTests).GetTypeInfo().Assembly.GetName().Name);

        return environment.Object;
    }

    [Fact]
    public void Should_build_generic()
    {
        // Arrange
        var provider = _services.BuildServiceProvider(true);
        IApplicationBuilder builder = new ApplicationBuilder(provider);
        builder = builder.UseMiddleware<ExceptionHandlerMiddleware>();

        // Act
        var del = builder.Build();
        del.Invoke(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    public void Should_build_func()
    {
        // Arrange
        _counter = 0;
        var provider = _services.BuildServiceProvider(true);
        IApplicationBuilder builder = new ApplicationBuilder(provider);
        builder = builder.Use(async (ctx, next) =>
        {
            _counter++;
            await next.Invoke();
        });

        // Act
        var del = builder.Build();
        del.Invoke(_httpContext);

        // Assert
        _counter.ShouldBe(1);
        _httpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void Middleware_Multi_Parameters_Invoke()
    {
        // Arrange
        var provider = _services.BuildServiceProvider(true);
        IApplicationBuilder builder = new ApplicationBuilder(provider);
        builder = builder.UseMiddleware<MultiParametersInvokeMiddleware>();

        // Act, Assert
        var del = builder.Build();
        del.Invoke(_httpContext);
    }

    private class MultiParametersInvokeMiddleware : OcelotMiddleware
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public MultiParametersInvokeMiddleware(RequestDelegate next)
            : base(new FakeLogger()) { }
#pragma warning disable CA1822 // Mark members as static
        public Task Invoke(HttpContext context, IServiceProvider serviceProvider) => Task.CompletedTask;
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter
    }
}

internal class FakeLogger : IOcelotLogger
{
    public void LogCritical(string message, Exception exception) { }
    public void LogCritical(Func<string> messageFactory, Exception exception) { }
    public void LogError(string message, Exception exception) { }
    public void LogError(Func<string> messageFactory, Exception exception) { }
    public void LogDebug(string message) { }
    public void LogDebug(Func<string> messageFactory) { }
    public void LogInformation(string message) { }
    public void LogInformation(Func<string> messageFactory) { }
    public void LogWarning(string message) { }
    public void LogTrace(string message) { }
    public void LogTrace(Func<string> messageFactory) { }
    public void LogWarning(Func<string> messageFactory) { }
}
