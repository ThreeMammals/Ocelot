using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Authorization;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.UnitTests.Authorization;

[Trait("Bug", "679")] // https://github.com/ThreeMammals/Ocelot/issues/679
[Trait("PR", "2414")] // https://github.com/ThreeMammals/Ocelot/pull/2414
public class RouteClaimsRequirementBindingTests : UnitTest
{
    private readonly RouteClaimsRequirementPostConfigureOptions _sut;

    public RouteClaimsRequirementBindingTests()
    {
        _sut = new RouteClaimsRequirementPostConfigureOptions(new ConfigurationBuilder().Build());
    }

    [Fact]
    public void PostConfigure_NullOptions_DoesNothing()
    {
        _sut.PostConfigure(null, null);
    }

    [Fact]
    public void PostConfigure_NullRoutes_InitializesRoutes()
    {
        var fileConfig = new FileConfiguration { Routes = null };
        _sut.PostConfigure(null, fileConfig);
        Assert.NotNull(fileConfig.Routes);
        Assert.Empty(fileConfig.Routes);
    }

    [Fact]
    public void PostConfigure_RouteWithoutClaimsRequirement_LeavesRouteUntouched()
    {
        // Arrange
        var route = new FileRoute();
        var config = new FileConfiguration();
        config.Routes.Add(route);
        var configuration = new ConfigurationBuilder()
            .AddOcelot(config, null, MergeOcelotJson.ToMemory)
            .Build();
        var services = new ServiceCollection();
        services.AddOcelot(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        var actual = provider.GetRequiredService<IOptions<FileConfiguration>>().Value;

        // Assert
        Assert.Empty(actual.Routes[0].RouteClaimsRequirement);
    }

    [Fact]
    public void ShouldPreserveColonInRouteClaimsRequirementKey()
    {
        // Arrange
        var route = new FileRoute();
        route.RouteClaimsRequirement.Add(ClaimTypes.Role, "Admin"); // URL-shaped key
        var config = new FileConfiguration();
        config.Routes.Add(route);

        var configuration = new ConfigurationBuilder()
            .AddOcelot(config, null, MergeOcelotJson.ToMemory)
            .Build();

        var services = new ServiceCollection();
        services.AddOcelot(configuration);
        var provider = services.BuildServiceProvider();

        // Act
        var actual = provider.GetRequiredService<IOptions<FileConfiguration>>().Value;
        var requirement = actual.Routes[0].RouteClaimsRequirement;

        // Assert
        Assert.Contains(ClaimTypes.Role, requirement.Keys);
        Assert.Equal("Admin", requirement[ClaimTypes.Role]);
    }
}
