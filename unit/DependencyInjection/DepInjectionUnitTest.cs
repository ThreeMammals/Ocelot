using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.UnitTests.DependencyInjection;

public class DepInjectionUnitTest : UnitTest
{
    protected readonly ServiceCollection Services = new();

    protected void AssertSingleton<TService, TImplementation>(IServiceCollection services = null)
        where TImplementation : class, TService
    {
        AssertService<TService, TImplementation>(ServiceLifetime.Singleton, services);
    }

    protected void AssertService<TService, TImplementation>(ServiceLifetime expected, IServiceCollection services = null)
        where TImplementation : class, TService
    {
        services ??= Services;
        var descriptor = services.SingleOrDefault(d =>
            d.ServiceType == typeof(TService) &&
            d.ImplementationType == typeof(TImplementation));

        Assert.NotNull(descriptor);
        Assert.Equal(expected, descriptor.Lifetime);
    }
}
