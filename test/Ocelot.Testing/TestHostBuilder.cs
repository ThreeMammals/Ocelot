using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ocelot.Testing;

public sealed class TestHostBuilder : WebHostBuilder
{
    public static IWebHostBuilder Create()
        => new WebHostBuilder().UseDefaultServiceProvider(WithEnabledValidateScopes);

    public static IWebHostBuilder Create(Action<ServiceProviderOptions> configure)
        => new WebHostBuilder().UseDefaultServiceProvider(configure + WithEnabledValidateScopes);

    public static void WithEnabledValidateScopes(ServiceProviderOptions options)
        => options.ValidateScopes = true;

    public static IHostBuilder CreateHost()
        => Host.CreateDefaultBuilder().UseDefaultServiceProvider(WithEnabledValidateScopes);

    public static IHostBuilder CreateHost(Action<ServiceProviderOptions> configure)
        => Host.CreateDefaultBuilder().UseDefaultServiceProvider(configure + WithEnabledValidateScopes);
}
