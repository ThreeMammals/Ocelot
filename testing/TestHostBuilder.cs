using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ocelot.Testing;

public sealed class TestHostBuilder
#if NET10_0_OR_GREATER
    : HostBuilder
#else
    : WebHostBuilder
#endif
{
#if NET10_0_OR_GREATER
    public static IHostBuilder Create()
        => new HostBuilder().UseDefaultServiceProvider(WithEnabledValidateScopes);
#else
    public static IWebHostBuilder Create()
        => new WebHostBuilder().UseDefaultServiceProvider(WithEnabledValidateScopes);
#endif

#if NET10_0_OR_GREATER
    public static IHostBuilder Create(Action<ServiceProviderOptions> configure)
        => new HostBuilder().UseDefaultServiceProvider(configure + WithEnabledValidateScopes);
#else
    public static IWebHostBuilder Create(Action<ServiceProviderOptions> configure)
        => new WebHostBuilder().UseDefaultServiceProvider(configure + WithEnabledValidateScopes);
#endif

    public static void WithEnabledValidateScopes(ServiceProviderOptions options)
        => options.ValidateScopes = true;

    public static IHostBuilder CreateHost()
        => Host.CreateDefaultBuilder().UseDefaultServiceProvider(WithEnabledValidateScopes);

    public static IHostBuilder CreateHost(Action<ServiceProviderOptions> configure)
        => Host.CreateDefaultBuilder().UseDefaultServiceProvider(configure + WithEnabledValidateScopes);
}
