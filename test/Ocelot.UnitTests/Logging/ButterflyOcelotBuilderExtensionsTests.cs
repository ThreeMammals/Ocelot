using AspectCore.Injector;
using Butterfly.Client;
using Butterfly.Client.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Tracing.Butterfly;
using Shouldly;
using System;
using System.Management;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Ocelot.UnitTests.Logging;

public class ButterflyOcelotBuilderExtensionsTests : UnitTest
{
    private static IHostingEnvironment GetHostingEnvironment([CallerMemberName] string testName = null)
    {
        var environment = new Mock<IHostingEnvironment>();
        environment.Setup(e => e.ApplicationName).Returns(testName);
        environment.Setup(e => e.EnvironmentName).Returns(testName);
        return environment.Object;
    }

    [Fact]
    public void AddButterfly_IOcelotBuilder()
    {
        // Arrange
        IConfiguration configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(GetHostingEnvironment());
        services.AddSingleton(configRoot);
        IOcelotBuilder builder = services.AddOcelot(configRoot);
        ButterflyOptions options = new();
        static void settings(ButterflyOptions o) => o.CollectorUrl = "https://ocelot.net";

        // Act
        var actual = builder.AddButterfly(settings);

        // Assert
        Assert.Equal(builder, actual);
        services.Single(x => x.ServiceType == typeof(IOcelotTracer))
            .Lifetime.ShouldBe(ServiceLifetime.Singleton);
        services.Single(x => x.ServiceType == typeof(IButterflyDispatcherProvider))
            .Lifetime.ShouldBe(ServiceLifetime.Singleton);
        services.Single(x => x.ServiceType == typeof(IConfigureOptions<ButterflyOptions>))
            .Lifetime.ShouldBe(ServiceLifetime.Singleton);
        var provider = services.BuildServiceProvider(true);
        var actualTracer = provider.GetService<IOcelotTracer>()
            .ShouldNotBeNull().ShouldBeOfType<ButterflyTracer>();
        var actualProvider = provider.GetService<IButterflyDispatcherProvider>()
            .ShouldNotBeNull().ShouldBeOfType<ButterflyDispatcherProvider>();
        var actualOptions = provider.GetService<IConfigureOptions<ButterflyOptions>>()
            .ShouldNotBeNull().ShouldBeOfType<ConfigureNamedOptions<ButterflyOptions>>();
    }
}
