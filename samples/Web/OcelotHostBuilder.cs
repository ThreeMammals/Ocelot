using Microsoft.AspNetCore;

namespace Ocelot.Samples.Web;

public sealed class OcelotHostBuilder : WebHostBuilder
{
    public static IWebHostBuilder Create() => WebHost
        .CreateDefaultBuilder()
        .UseDefaultServiceProvider(WithEnabledValidateScopes);
    public static IWebHostBuilder Create(Action<ServiceProviderOptions> configure) => WebHost
        .CreateDefaultBuilder()
        .UseDefaultServiceProvider(configure + WithEnabledValidateScopes);

    public static IWebHostBuilder Create(string[] args) => WebHost
        .CreateDefaultBuilder(args)
        .UseDefaultServiceProvider(WithEnabledValidateScopes);

    public static IWebHostBuilder Create(string[] args, Action<ServiceProviderOptions> configure) => WebHost
        .CreateDefaultBuilder(args)
        .UseDefaultServiceProvider(configure + WithEnabledValidateScopes);

    public static void WithEnabledValidateScopes(ServiceProviderOptions options)
        => options.ValidateScopes = true;

    // TODO Add more standard Ocelot setup
    public static IWebHostBuilder BasicSetup() => Create(); // in CreateDefaultBuilder() implicitly calls -> .UseKestrel().UseContentRoot(Directory.GetCurrentDirectory());
}
