using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using System.Security.Claims;
using System.Text;

namespace Ocelot.UnitTests.Authorization;

public sealed class RouteClaimsRequirementPostConfigureOptionsTests : UnitTest
{
    [Fact]
    [Trait("Bug", "679")]
    public void Should_bind_route_claim_requirement_key_containing_url_claim_type()
    {
        // Arrange
        var configuration = GivenConfiguration(new()
        {
            { "Role", "User" },
            { ClaimTypes.Role, "Admin" },
        });
        var services = new ServiceCollection();

        // Act
        services.AddOcelot(configuration);
        using var provider = services.BuildServiceProvider(true);
        var fileConfiguration = provider.GetRequiredService<IOptions<FileConfiguration>>().Value;
        var requirements = fileConfiguration.Routes[0].RouteClaimsRequirement;

        // Assert
        requirements.Count.ShouldBe(2);
        requirements["Role"].ShouldBe("User");
        requirements[ClaimTypes.Role].ShouldBe("Admin");
        requirements.ContainsKey("http").ShouldBeFalse();
    }

    private static IConfiguration GivenConfiguration(Dictionary<string, string> routeClaimsRequirement)
    {
        var fileConfiguration = new FileConfiguration
        {
            Routes = new()
            {
                new()
                {
                    RouteClaimsRequirement = routeClaimsRequirement,
                },
            },
        };
        var json = JsonConvert.SerializeObject(fileConfiguration);
        var bytes = Encoding.UTF8.GetBytes(json);
        return new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(bytes))
            .Build();
    }
}
