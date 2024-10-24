using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Kubernetes;
using System.Reflection;

namespace Ocelot.UnitTests.Kubernetes;

public class OcelotBuilderExtensionsTests : UnitTest // No Chinese tests now!
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configRoot;
    private IOcelotBuilder _ocelotBuilder;

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
    [Trait("Feat", "345")]
    public void AddKubernetes_NoExceptions_ShouldSetUpKubernetes()
    {
        var addOcelot = () => _ocelotBuilder = _services.AddOcelot(_configRoot);
        addOcelot.ShouldNotThrow();

        var addKubernetes = () => _ocelotBuilder.AddKubernetes();
        addKubernetes.ShouldNotThrow();
    }
}
