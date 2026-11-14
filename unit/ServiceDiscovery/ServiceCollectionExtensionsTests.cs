using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.ServiceDiscovery;

[Trait("PR", "1198")]
[Trait("Feat", "1188")]
public class ServiceCollectionExtensionsTests : UnitTest
{
    [Fact]
    public void AddOcelotDynamicRouting_RegistersDefaultImplementation()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddHttpContextAccessor();

        // Act
        var actual = services.AddOcelotDynamicRouting();

        // Assert
        Assert.Same(services, actual);
        Assert.IsType<UrlPathDynamicRouting>(services.BuildServiceProvider().GetRequiredService<IDynamicRouting>());
    }

    [Fact]
    public void AddOcelotDynamicRouting_RegistersCustomImplementation()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOcelotDynamicRouting<FakeDynamicRouting>();

        // Assert
        Assert.IsType<FakeDynamicRouting>(services.BuildServiceProvider().GetRequiredService<IDynamicRouting>());
    }

    private sealed class FakeDynamicRouting : IDynamicRouting
    {
        public HttpRequest Request => null;
        public string GetDownstreamPath() => string.Empty;
        public ServiceInfo GetServiceName() => new(string.Empty);
        public ServiceInfo GetServiceName(string upstreamUrlPath) => new(string.Empty);
        public object SetServiceName(object state, ServiceInfo service) => state;
    }
}
