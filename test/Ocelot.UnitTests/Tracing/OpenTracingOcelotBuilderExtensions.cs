using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Tracing.OpenTracing;
using OpenTracing;
using OpenTracing.Util;

namespace Ocelot.UnitTests.Tracing;

public class OpenTracingOcelotBuilderExtensions
{
    [Fact]
    public void AddOpenTracing_IOcelotBuilder()
    {
        // Arrange
        ConfigurationRoot configRoot = new(new List<IConfigurationProvider>());
        IServiceCollection services = new ServiceCollection();
        IOcelotBuilder builder = services.AddOcelot(configRoot);

        // Act
        var actual = builder.AddOpenTracing();

        // Assert
        Assert.Equal(builder, actual);
        services.Single(x => x.ServiceType == typeof(IOcelotTracer))
            .Lifetime.ShouldBe(ServiceLifetime.Singleton);
        services.Single(x => x.ServiceType == typeof(ITracer))
            .Lifetime.ShouldBe(ServiceLifetime.Singleton);
        var provider = services.BuildServiceProvider(true);
        var actualTracer = provider.GetService<IOcelotTracer>()
            .ShouldNotBeNull().ShouldBeOfType<OpenTracingTracer>();
        var nativeTracer = provider.GetService<ITracer>()
            .ShouldNotBeNull().ShouldBeOfType<GlobalTracer>();
    }
}
