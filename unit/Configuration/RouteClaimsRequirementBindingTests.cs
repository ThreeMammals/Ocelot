using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Authorization;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

[Trait("Bug", "679")] // https://github.com/ThreeMammals/Ocelot/issues/679
[Trait("PR", "2414")] // https://github.com/ThreeMammals/Ocelot/pull/2414
public class RouteClaimsRequirementBindingTests
{
    [Fact]
    public void PostConfigure_NullOptions_DoesNothing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var sut = new RouteClaimsRequirementPostConfigureOptions(configuration);

        // Should not throw
        sut.PostConfigure(null, null);
    }

    [Fact]
    public void PostConfigure_RouteWithoutClaimsRequirement_LeavesRouteUntouched()
    {
        // Arrange
        const string json = """
        {
          "Routes": [
            {
              "DownstreamPathTemplate": "/",
              "DownstreamScheme": "http",
              "UpstreamPathTemplate": "/"
            }
          ]
        }
        """;
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();
        var services = new ServiceCollection();
        services.Configure<FileConfiguration>(configuration);
        services.AddSingleton<IPostConfigureOptions<FileConfiguration>>(
            new RouteClaimsRequirementPostConfigureOptions(configuration));
        var provider = services.BuildServiceProvider();

        // Act
        var fileConfig = provider.GetRequiredService<IOptions<FileConfiguration>>().Value;

        // Assert
        Assert.Empty(fileConfig.Routes[0].RouteClaimsRequirement);
    }

    [Fact]
    public void ShouldPreserveColonInRouteClaimsRequirementKey()
    {
        // Arrange
        const string json = """
        {
          "Routes": [
            {
              "DownstreamPathTemplate": "/",
              "DownstreamScheme": "http",
              "UpstreamPathTemplate": "/",
              "RouteClaimsRequirement": {
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin"
              }
            }
          ]
        }
        """;
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();
        var services = new ServiceCollection();
        services.Configure<FileConfiguration>(configuration);
        services.AddSingleton<IPostConfigureOptions<FileConfiguration>>(
            new RouteClaimsRequirementPostConfigureOptions(configuration));
        var provider = services.BuildServiceProvider();

        // Act
        var fileConfig = provider.GetRequiredService<IOptions<FileConfiguration>>().Value;
        var requirement = fileConfig.Routes[0].RouteClaimsRequirement;

        // Assert
        Assert.True(requirement.ContainsKey("http://schemas.microsoft.com/ws/2008/06/identity/claims/role"));
        Assert.Equal("Admin", requirement["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]);
    }

    [Fact]
    public void PostConfigure_NullRoutes_InitializesRoutes()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var sut = new RouteClaimsRequirementPostConfigureOptions(configuration);
        var fileConfig = new FileConfiguration
        {
            Routes = null,
        };

        // Act
        sut.PostConfigure(null, fileConfig);

        // Assert
        Assert.NotNull(fileConfig.Routes);
        Assert.Empty(fileConfig.Routes);
    }
}
