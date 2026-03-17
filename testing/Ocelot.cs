using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Ocelot.Testing;

internal class Ocelot
{
    private static Assembly? ocelotAssembly;
    public static Assembly OcelotAssembly { get => ocelotAssembly ??= Assembly.Load(new AssemblyName("Ocelot")); }

    public static object? CreateFileRoute(out Type type)
    {
        type = OcelotAssembly.GetType("Ocelot.Configuration.File.FileRoute")!;
        return Activator.CreateInstance(type);
    }

    public static object? CreateFileConfiguration(out Type type)
    {
        type = OcelotAssembly.GetType("Ocelot.Configuration.File.FileConfiguration")!;
        return Activator.CreateInstance(type);
    }

    public static object? CreateFileHostAndPort(string host, int port, out Type type)
    {
        type = OcelotAssembly.GetType("Ocelot.Configuration.File.FileHostAndPort")!;
        return Activator.CreateInstance(type, host, port);
    }

    public static object? AddOcelot(IServiceCollection services)
    {
        var type = OcelotAssembly.GetType("Ocelot.DependencyInjection.ServiceCollectionExtensions")!;
        var method = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m =>
                m.Name == "AddOcelot" &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(IServiceCollection));
        return method?.Invoke(null, [services]);
    }

    public static Task<IApplicationBuilder> UseOcelot(IApplicationBuilder builder)
    {
        var type = OcelotAssembly.GetType("Ocelot.Middleware.OcelotMiddlewareExtensions")!;
        var method = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m =>
                m.Name == "UseOcelot" &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(IApplicationBuilder));
        return method?.Invoke(null, [builder]) as Task<IApplicationBuilder> ?? Task.FromResult(builder);
    }
}
