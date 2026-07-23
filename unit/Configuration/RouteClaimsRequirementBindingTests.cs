using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class RouteClaimsRequirementBindingTests
{
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
		var provider = services.BuildServiceProvider();

		// Act
		var fileConfig = provider.GetRequiredService<IOptions<FileConfiguration>>().Value;
		var requirement = fileConfig.Routes[0].RouteClaimsRequirement;

		// Assert — this SHOULD pass, but fails today, proving the bug		Assert.True(requirement.ContainsKey("http://schemas.microsoft.com/ws/2008/06/identity/claims/role"));
		Assert.Equal("Admin", requirement["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]);
	}
}