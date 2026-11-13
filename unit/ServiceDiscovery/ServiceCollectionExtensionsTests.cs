using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.ServiceDiscovery;

namespace Ocelot.UnitTests.ServiceDiscovery;

public class ServiceCollectionExtensionsTests : UnitTest
{
    [Fact]
    public void AddOcelotDynamicRouting_RegistersDefaultImplementation()
    {
        var services = new ServiceCollection()
            .AddHttpContextAccessor();

        var actual = services.AddOcelotDynamicRouting();

        Assert.Same(services, actual);
        Assert.IsType<UrlPathDynamicRouting>(services.BuildServiceProvider().GetRequiredService<IDynamicRouting>());
    }

    [Fact]
    public void AddOcelotDynamicRouting_RegistersCustomImplementation()
    {
        var services = new ServiceCollection();

        services.AddOcelotDynamicRouting<FakeDynamicRouting>();

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
