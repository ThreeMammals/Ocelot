using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using System.Reflection;
using Extensions = Ocelot.DependencyInjection.ServiceCollectionExtensions;

namespace Ocelot.UnitTests.DependencyInjection;

public class ServiceCollectionExtensionsTests : DepInjectionUnitTest
{
    [Fact]
    [Trait("PR", "1986")]
    [Trait("Issue", "1518")]
    public void AddOcelot_NoConfiguration_DefaultConfiguration()
    {
        // Arrange, Act
        var ocelot = Services.AddOcelot();

        // Assert
        ocelot.ShouldNotBeNull()
            .Configuration.ShouldNotBeNull();
    }

    [Theory]
    [Trait("PR", "1986")]
    [Trait("Issue", "1518")]
    [InlineData(false)]
    [InlineData(true)]
    public void FindConfiguration_HasDescriptor_HappyPath(bool hasConfig)
    {
        // Arrange
        IConfiguration config = hasConfig ? new ConfigurationBuilder().Build() : null;
        var descriptor = new ServiceDescriptor(typeof(IConfiguration), (p) => config, ServiceLifetime.Transient);
        Services.Add(descriptor);
        IWebHostEnvironment env = null;

        // Act
        var method = typeof(Extensions).GetMethod("FindConfiguration", BindingFlags.NonPublic | BindingFlags.Static);
        var actual = (IConfiguration)method.Invoke(null, [Services, env]);

        // Assert
        actual.ShouldNotBeNull();
        if (hasConfig)
        {
            actual.Equals(config).ShouldBeTrue();
        }
    }

    [Fact]
    [Trait("PR", "1986")]
    [Trait("Issue", "1518")]
    public void AddOcelotUsingBuilder_NoConfigurationParam_ShouldFindConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        Services.AddSingleton<IConfiguration>(config);

        // Act
        var ocelot = Services.AddOcelotUsingBuilder(null, CustomBuilder);

        // Assert
        AssertConfiguration(ocelot, config);
    }

    [Theory]
    [Trait("PR", "1986")]
    [Trait("Issue", "1518")]
    [InlineData(false)]
    [InlineData(true)]
    public void AddOcelotUsingBuilder_WithConfigurationParam_ShouldFindConfiguration(bool shouldFind)
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        if (shouldFind)
        {
            Services.AddSingleton<IConfiguration>(config);
        }

        // Act
        var ocelot = Services.AddOcelotUsingBuilder(shouldFind ? null : config, CustomBuilder);

        // Assert
        AssertConfiguration(ocelot, config);
    }

    private void AssertConfiguration(IOcelotBuilder ocelot, IConfiguration config)
    {
        ocelot.ShouldNotBeNull();
        var actual = ocelot.Configuration.ShouldNotBeNull();
        actual.Equals(config).ShouldBeTrue(); // check references equality
        actual.ShouldBe(config);
        Assert.Equal(1, _count);
    }

    private int _count;

    private IMvcCoreBuilder CustomBuilder(IMvcCoreBuilder builder, Assembly assembly)
    {
        _count++;
        return builder;
    }
}
