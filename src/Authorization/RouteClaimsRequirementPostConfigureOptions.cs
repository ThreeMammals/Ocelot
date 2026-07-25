using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;

namespace Ocelot.Authorization;

public sealed class RouteClaimsRequirementPostConfigureOptions : IPostConfigureOptions<FileConfiguration>
{
	private readonly IConfiguration _configuration;

	public RouteClaimsRequirementPostConfigureOptions(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public void PostConfigure(string name, FileConfiguration options)
	{
        if (options is null)
        {
            return;
        }

        options.Routes ??= [];

        var routes = _configuration.GetSection(nameof(FileConfiguration.Routes));
		for (var i = 0; i < options.Routes.Count; i++)
		{
			var route = options.Routes[i];
			var routeClaims = routes.GetSection(i.ToString())
				.GetSection(nameof(FileRoute.RouteClaimsRequirement));
			var requirements = ReadRequirements(routeClaims);

			if (requirements.Count > 0)
			{
				route.RouteClaimsRequirement = requirements;
			}
		}
	}

	private static Dictionary<string, string> ReadRequirements(IConfigurationSection section)
	{
		var requirements = new Dictionary<string, string>();
		foreach (var child in section.GetChildren())
		{
			ReadRequirements(child, child.Key, requirements);
		}
		return requirements;
	}

	private static void ReadRequirements(IConfigurationSection section, string key, Dictionary<string, string> requirements)
	{
		if (section.Value != null)
		{
			requirements[key] = section.Value;
		}

		foreach (var child in section.GetChildren())
		{
			ReadRequirements(child, ConfigurationPath.Combine(key, child.Key), requirements);
		}
	}
}
