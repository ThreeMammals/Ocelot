using Microsoft.Extensions.Configuration;
using Ocelot.Authorization;
using Ocelot.Configuration.File;
using System.Security.Claims;

namespace Ocelot.UnitTests.Authorization;

/// <summary>
/// Unit tests for <see cref="RouteClaimsRequirementPostConfigureOptions"/>.
///
/// The SUT copies <c>RouteClaimsRequirement</c> from raw <see cref="IConfiguration"/> onto already-bound <see cref="FileConfiguration"/> routes.
/// That is required because the default options binder cannot round-trip dictionary keys that contain ':'
/// (typical for claim type URIs such as <c>http://schemas.microsoft.com/ws/2008/06/identity/claims/role</c>).
///
/// These tests use a real in-memory configuration so they exercise the same section-walking / flattening logic that runs in production.
/// They assume <c>IConfiguration.OcelotRoutes()</c> returns the <c>Routes</c> section (Ocelot.DependencyInjection).
/// </summary>
[Trait("Bug", "679")] // https://github.com/ThreeMammals/Ocelot/issues/679
[Trait("PR", "2414")] // https://github.com/ThreeMammals/Ocelot/pull/2414
[Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
public sealed class RouteClaimsRequirementPostConfigureOptionsTests
{
    private const string DefaultName = "unused";

    [Fact]
    public void PostConfigure_NullOptions_DoesNotThrow()
    {
        var sut = CreateSut([]);

        var exception = Record.Exception(() => sut.PostConfigure(DefaultName, options: null));

        Assert.Null(exception);
    }

    [Fact]
    public void PostConfigure_NullRoutes_InitializesEmptyCollection()
    {
        var sut = CreateSut([]);
        var options = new FileConfiguration { Routes = null! };

        sut.PostConfigure(DefaultName, options);

        Assert.NotNull(options.Routes);
        Assert.Empty(options.Routes);
    }

    [Fact]
    public void PostConfigure_EmptyRoutes_LeavesCollectionEmpty()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:UserType"] = "registered"
        });
        var options = new FileConfiguration { Routes = [] };

        sut.PostConfigure(DefaultName, options);

        Assert.Empty(options.Routes);
    }

    [Fact]
    public void PostConfigure_RouteWithoutClaimsSection_DoesNotAssignRequirements()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:UpstreamPathTemplate"] = "/api/public"
        });
        var route = new FileRoute { UpstreamPathTemplate = "/api/public" };
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.True(route.RouteClaimsRequirement is null || route.RouteClaimsRequirement.Count == 0);
    }

    [Fact]
    public void PostConfigure_EmptyClaimsSection_DoesNotAssignRequirements()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement"] = ""
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.True(route.RouteClaimsRequirement is null || route.RouteClaimsRequirement.Count == 0);
    }

    [Fact]
    public void PostConfigure_SimpleFlatClaim_AssignsRequirement()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:UserType"] = "registered"
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.NotNull(route.RouteClaimsRequirement);
        Assert.Equal(1, route.RouteClaimsRequirement.Count);
        Assert.Equal("registered", route.RouteClaimsRequirement["UserType"]);
    }

    [Fact]
    public void PostConfigure_MultipleFlatClaimsOnSameRoute_AssignsAll()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:UserType"] = "registered",
            ["Routes:0:RouteClaimsRequirement:Role"] = "Administrator"
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.Equal(2, route.RouteClaimsRequirement.Count);
        Assert.Equal("registered", route.RouteClaimsRequirement["UserType"]);
        Assert.Equal("Administrator", route.RouteClaimsRequirement["Role"]);
    }

    [Fact]
    public void PostConfigure_NestedClaimKey_IsFlattenedWithColonSeparator()
    {
        // A claim type URI contains ':'. IConfiguration treats ':' as a path separator, so the key is stored as nested sections.
        // The SUT must reconstruct the original key via ConfigurationPath.Combine.
        var sut = CreateSut(new Dictionary<string, string>
        {
            [$"Routes:0:RouteClaimsRequirement:{ClaimTypes.Role}"] = "Administrator"
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.NotNull(route.RouteClaimsRequirement);
        Assert.True(route.RouteClaimsRequirement.ContainsKey(ClaimTypes.Role));
        Assert.Equal("Administrator", route.RouteClaimsRequirement[ClaimTypes.Role]);
        Assert.False(route.RouteClaimsRequirement.ContainsKey("http"));
    }

    [Fact]
    public void PostConfigure_DeeplyNestedKey_IsFullyFlattened()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:outer:inner:leaf"] = "value"
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.Equal("value", route.RouteClaimsRequirement["outer:inner:leaf"]);
        Assert.Single(route.RouteClaimsRequirement);
    }

    [Fact]
    public void PostConfigure_MultipleRoutes_AppliesRequirementsByIndex()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:Role"] = "Admin",
            ["Routes:2:RouteClaimsRequirement:UserType"] = "registered"
        });

        var route0 = new FileRoute { UpstreamPathTemplate = "/admin" };
        var route1 = new FileRoute { UpstreamPathTemplate = "/public" };
        var route2 = new FileRoute { UpstreamPathTemplate = "/me" };
        var options = new FileConfiguration { Routes = [route0, route1, route2] };

        sut.PostConfigure(DefaultName, options);

        Assert.Equal("Admin", route0.RouteClaimsRequirement["Role"]);
        Assert.True(route1.RouteClaimsRequirement is null || route1.RouteClaimsRequirement.Count == 0);
        Assert.Equal("registered", route2.RouteClaimsRequirement["UserType"]);
    }

    [Fact]
    public void PostConfigure_ReplacesExistingRouteClaimsRequirement_WhenConfigHasValues()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:Role"] = "from-config"
        });
        var route = new FileRoute
        {
            RouteClaimsRequirement = new Dictionary<string, string>
            {
                ["Role"] = "pre-bound",
                ["Extra"] = "should-be-replaced"
            }
        };
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.Equal(1, route.RouteClaimsRequirement.Count);
        Assert.Equal("from-config", route.RouteClaimsRequirement["Role"]);
        Assert.False(route.RouteClaimsRequirement.ContainsKey("Extra"));
    }

    [Fact]
    public void PostConfigure_DoesNotReplaceExistingRequirements_WhenConfigSectionIsEmpty()
    {
        var existing = new Dictionary<string, string> { ["Role"] = "pre-bound" };
        var sut = CreateSut([]);
        var route = new FileRoute { RouteClaimsRequirement = existing };
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.Same(existing, route.RouteClaimsRequirement);
        Assert.Equal("pre-bound", route.RouteClaimsRequirement["Role"]);
    }

    [Fact]
    public void PostConfigure_MoreBoundRoutesThanConfigEntries_DoesNotThrow()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:Role"] = "Admin"
        });
        var options = new FileConfiguration
        {
            Routes = [new FileRoute(), new FileRoute(), new FileRoute()]
        };

        var exception = Record.Exception(() => sut.PostConfigure(DefaultName, options));

        Assert.Null(exception);
        Assert.Equal("Admin", options.Routes[0].RouteClaimsRequirement["Role"]);
        Assert.True(options.Routes[1].RouteClaimsRequirement is null || options.Routes[1].RouteClaimsRequirement.Count == 0);
        Assert.True(options.Routes[2].RouteClaimsRequirement is null || options.Routes[2].RouteClaimsRequirement.Count == 0);
    }

    [Fact]
    public void PostConfigure_NameParameter_IsIgnored()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:UserType"] = "registered"
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(name: "any-options-name", options);

        Assert.Equal("registered", route.RouteClaimsRequirement["UserType"]);
    }

    [Fact]
    public void PostConfigure_DoesNotMutateUnrelatedRouteProperties()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:Role"] = "Admin"
        });
        var route = new FileRoute
        {
            Key = "R1",
            UpstreamPathTemplate = "/secure",
            DownstreamPathTemplate = "/api/secure"
        };
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.Equal("R1", route.Key);
        Assert.Equal("/secure", route.UpstreamPathTemplate);
        Assert.Equal("/api/secure", route.DownstreamPathTemplate);
        Assert.Equal("Admin", route.RouteClaimsRequirement["Role"]);
    }

    [Theory]
    [InlineData("UserType", "registered")]
    [InlineData("scope", "api.read")]
    [InlineData("Role", "Admin,Manager")]
    public void PostConfigure_CommonClaimShapes_ArePreserved(string claimType, string claimValue)
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            [$"Routes:0:RouteClaimsRequirement:{claimType}"] = claimValue
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);

        Assert.Equal(claimValue, route.RouteClaimsRequirement[claimType]);
    }

    [Fact]
    public void PostConfigure_CanBeInvokedMoreThanOnce_IdempotentForSameConfig()
    {
        var sut = CreateSut(new Dictionary<string, string>
        {
            ["Routes:0:RouteClaimsRequirement:Role"] = "Admin"
        });
        var route = new FileRoute();
        var options = new FileConfiguration { Routes = [route] };

        sut.PostConfigure(DefaultName, options);
        sut.PostConfigure(DefaultName, options);

        Assert.Single(route.RouteClaimsRequirement);
        Assert.Equal("Admin", route.RouteClaimsRequirement["Role"]);
    }

    private static RouteClaimsRequirementPostConfigureOptions CreateSut(IEnumerable<KeyValuePair<string, string>> values)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new RouteClaimsRequirementPostConfigureOptions(configuration);
    }
}
