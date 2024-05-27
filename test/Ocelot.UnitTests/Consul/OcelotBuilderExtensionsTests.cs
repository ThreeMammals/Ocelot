using Consul;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.Values;
using System.Reflection;

namespace Ocelot.UnitTests.Consul;

public class OcelotBuilderExtensionsTests : UnitTest
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configRoot;

    public OcelotBuilderExtensionsTests()
    {
        _configRoot = new ConfigurationRoot(new List<IConfigurationProvider>());
        _services = new ServiceCollection();
        _services.AddSingleton(GetHostingEnvironment());
        _services.AddSingleton(_configRoot);
    }

    private static IWebHostEnvironment GetHostingEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.ApplicationName)
            .Returns(typeof(OcelotBuilderExtensionsTests).GetTypeInfo().Assembly.GetName().Name);
        return environment.Object;
    }

    [Fact]
    public void AddConsul_ShouldSetUpConsul()
    {
        // Arrange
        Exception ex = null;
        try
        {
            // Act
            var builder = _services.AddOcelot(_configRoot);
            builder.AddConsul();
        }
        catch (Exception e)
        {
            ex = e;
        }

        // Assert
        ex.ShouldBeNull();
    }

    [Fact]
    public void AddConfigStoredInConsul_ShouldSetUpConsul()
    {
        // Arrange
        Exception ex = null;
        try
        {
            // Act
            var builder = _services.AddOcelot(_configRoot);
            builder.AddConsul().AddConfigStoredInConsul();
        }
        catch (Exception e)
        {
            ex = e;
        }

        // Assert
        ex.ShouldBeNull();
    }

    [Fact]
    public void AddConsulGeneric_TServiceBuilder_ShouldSetUpConsul()
    {
        // Arrange
        Exception ex = null;
        IOcelotBuilder builder = null;
        try
        {
            // Act
            builder = _services
                .AddOcelot(_configRoot)
                .AddConsul<FakeConsulServiceBuilder>();
        }
        catch (Exception e)
        {
            ex = e;
        }

        // Assert
        ex.ShouldBeNull();
        builder.ShouldNotBeNull();
        builder.Services.SingleOrDefault(s => s.ServiceType == typeof(IConsulServiceBuilder)).ShouldNotBeNull();
    }
}

internal class FakeConsulServiceBuilder : IConsulServiceBuilder
{
    public ConsulRegistryConfiguration Configuration => throw new NotImplementedException();
    public IEnumerable<Service> BuildServices(ServiceEntry[] entries, Node[] nodes) => throw new NotImplementedException();
    public Service CreateService(ServiceEntry serviceEntry, Node serviceNode) => throw new NotImplementedException();
    public bool IsValid(ServiceEntry entry) => throw new NotImplementedException();
}
