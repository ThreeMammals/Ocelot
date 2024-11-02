using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Testing;

public sealed class TestHostBuilder : WebHostBuilder
{
    public static IWebHostBuilder Create()
        => new WebHostBuilder().UseDefaultServiceProvider(WithEnabledValidateScopes);

    public static IWebHostBuilder Create(Action<ServiceProviderOptions> configure)
        => new WebHostBuilder().UseDefaultServiceProvider(configure + WithEnabledValidateScopes);

    public static void WithEnabledValidateScopes(ServiceProviderOptions options)
        => options.ValidateScopes = true;
}
