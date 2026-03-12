using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Testing;

public static class TestWebBuilder
{
    public static void WithEnabledValidateScopes(ServiceProviderOptions options)
        => options.ValidateScopes = true;

    public static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(WithEnabledValidateScopes);
        return builder;
    }
    public static WebApplicationBuilder Create(Action<ServiceProviderOptions> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(configure + WithEnabledValidateScopes);
        return builder;
    }

    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(WithEnabledValidateScopes);
        return builder;
    }

    public static WebApplicationBuilder CreateSlimBuilder()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseDefaultServiceProvider(WithEnabledValidateScopes);
        return builder;
    }
}
