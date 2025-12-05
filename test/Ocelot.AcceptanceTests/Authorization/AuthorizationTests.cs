using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Configuration.File;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Ocelot.AcceptanceTests.Authorization;

public sealed class AuthorizationTests : AuthenticationSteps
{
    private List<KeyValuePair<string, string>> _claims;
    private const string RequireRedevelopment = "TODO: Requires redevelopment of this test of old integration testing project.";

    [Fact(Skip = RequireRedevelopment)]
    public void Should_return_response_200_authorizing_route()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        Localhost(port),
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Get"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = JwtBearerDefaults.AuthenticationScheme, //"Test",
                    },
                    AddHeadersToRequest =
                    {
                        {"CustomerId", "Claims[CustomerId] > value"},
                        {"LocationId", "Claims[LocationId] > value"},
                        {"UserType", "Claims[sub] > value[0] > |"},
                        {"UserId", "Claims[sub] > value[1] > |"},
                    },
                    AddClaimsToRequest =
                    {
                        {"CustomerId", "Claims[CustomerId] > value"},
                        {"UserType", "Claims[sub] > value[0] > |"},
                        {"UserId", "Claims[sub] > value[1] > |"},
                    },
                    RouteClaimsRequirement =
                    {
                        {"UserType", "registered"},
                    },
                },
            },
        };
        var testName = TestName();
        Dictionary<string, string> claims = new()
        {
            {"CustomerId", "1122"},
            {"LocationId", "2233"},
        };
        this.Given(x => GivenThereIsExternalJwtSigningService())
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact(Skip = RequireRedevelopment)]
    public void Should_return_response_403_authorizing_route()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Get"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = "Test",
                    },
                    AddHeadersToRequest =
                    {
                        {"CustomerId", "Claims[CustomerId] > value"},
                        {"LocationId", "Claims[LocationId] > value"},
                        {"UserType", "Claims[sub] > value[0] > |"},
                        {"UserId", "Claims[sub] > value[1] > |"},
                    },
                    AddClaimsToRequest =
                    {
                        {"CustomerId", "Claims[CustomerId] > value"},
                        {"UserId", "Claims[sub] > value[1] > |"},
                    },
                    RouteClaimsRequirement =
                    {
                        {"UserType", "registered"},
                    },
                },
            },
        };
        var testName = TestName();
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIHaveAToken(testName))
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .BDDfy();
    }

    [Fact]
    public async Task Should_return_response_200_using_identity_server_with_allowed_scope()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AllowedScopes = [ "api", "api.readOnly", "openid", "offline_access" ];
        var configuration = GivenConfiguration(route);
        await GivenThereIsExternalJwtSigningService(route.AuthenticationOptions.AllowedScopes.ToArray());
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);

        await GivenIHaveAToken(scope: "api.readOnly");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
    }

    [Fact]
    public void Should_return_response_403_using_identity_server_with_scope_not_allowed()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        Localhost(port),
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Get"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = JwtBearerDefaults.AuthenticationScheme, // "Test",
                        AllowedScopes = new List<string>{ "api", "openid", "offline_access" },
                    },
                },
            },
        };
        var testName = TestName();
        List<string> allScopes = new(configuration.Routes[0].AuthenticationOptions.AllowedScopes)
        {
            "api.readOnly",
        };
        this.Given(x => GivenThereIsExternalJwtSigningService(allScopes.ToArray()))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithScope("api.readOnly", testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .BDDfy();
    }

    /// <summary>
    /// In ASP.NET Core, the <see cref="Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider"/>  (used for <c>appsettings.json</c> and similar) does not behave like a plain <see cref="Dictionary{K,V}"/>.
    /// It applies normalization rules to keys when loading configuration.
    /// That's why keys starting with "http://" or "https://" don't deserialize as you expect.
    /// </summary>
    /// <remarks>AI search:
    /// C# ASP.NET JsonConfigurationProvider Keys with "http://" prefix are not deserialized into dictionary.</remarks>
    [Fact(DisplayName = "TODO " + nameof(Should_fix_issue_240))]
    [Trait("Bug", "240")] // https://github.com/ThreeMammals/Ocelot/issues/240
    public void Should_fix_issue_240()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        Localhost(port),
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = ["Get"],
                    AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKey = JwtBearerDefaults.AuthenticationScheme, // "Test",
                    },
                    RouteClaimsRequirement = // TODO this is dictionary which doesn't support multiple keys of the same value
                    {
                        {ClaimTypes.Role, "User"}, // TODO Such a claim types in a form of URL (aka http://*) are not supported by JsonConfigurationProvider
                        {nameof(ClaimTypes.Role), "User"}, // this key is Ok because it is not an URL containing proto delimiter aka '://'
                    },
                },
            },
        };
        var claims = _claims = new()
        {
            new(nameof(ClaimTypes.Role), "AdminUser"),
            new(nameof(ClaimTypes.Role), "User"),
        };
        var testName = TestName();
        this.Given(x => GivenThereIsExternalJwtSigningService())
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact]
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public async Task Should_return_200_OK_with_global_allowed_scopes()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AuthenticationProviderKeys = []; // no route auth!
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration(allowedScopes: ["api", "apiGlobal"]);
        GivenThereIsAConfiguration(configuration);

        await GivenThereIsExternalJwtSigningService(configuration.GlobalConfiguration.AuthenticationOptions.AllowedScopes.ToArray());
        GivenThereIsAServiceRunningOn(port);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveAToken(scope: "apiGlobal");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOK();
        await ThenTheResponseBodyAsync();
    }

    [Fact]
    [Trait("Feature", "Space-separated scopes")]
    public async Task Should_return_200_OK_with_space_separated_scope_single_match()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AllowedScopes = ["api", "api.read", "api.write"];
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);

        await GivenThereIsExternalJwtSigningService(route.AuthenticationOptions.AllowedScopes.ToArray());
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from space-separated test");

        GivenOcelotIsRunning(WithJwtBearerAuthentication);

        // Generate token with space-separated scopes
        await GivenIHaveAToken(scope: "api.read api.write openid");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOK();
        await ThenTheResponseBodyShouldBeAsync("Hello from space-separated test");
    }

    [Fact]
    [Trait("Feature", "Space-separated scopes")]
    public async Task Should_return_200_OK_with_space_separated_scope_multiple_matches()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AllowedScopes = ["api.read", "api.write"];
        var configuration = GivenConfiguration(route);
        await GivenThereIsExternalJwtSigningService(route.AuthenticationOptions.AllowedScopes.ToArray());
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Multiple scopes matched");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);

        // Generate token with space-separated scopes that includes both allowed scopes
        await GivenIHaveAToken(scope: "api.read api.write openid offline_access");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Multiple scopes matched");
    }

    [Fact]
    [Trait("Feature", "Space-separated scopes")]
    public async Task Should_return_403_with_space_separated_scope_no_match()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AllowedScopes = ["admin", "superuser"];
        var configuration = GivenConfiguration(route);
        await GivenThereIsExternalJwtSigningService(route.AuthenticationOptions.AllowedScopes.ToArray());
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Should not reach here");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);

        // Generate token with space-separated scopes that don't match allowed scopes
        await GivenIHaveAToken(scope: "api.read api.write openid");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Feature", "Space-separated scopes")]
    public async Task Should_return_200_OK_with_space_separated_scope_with_extra_spaces()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AllowedScopes = ["api", "api.read"];
        var configuration = GivenConfiguration(route);
        await GivenThereIsExternalJwtSigningService(route.AuthenticationOptions.AllowedScopes.ToArray());
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Extra spaces handled");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);

        // Generate token with space-separated scopes that have extra spaces
        await GivenIHaveAToken(scope: "  api.read   api.write  ");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Extra spaces handled");
    }

    [Fact]
    [Trait("Feature", "Space-separated scopes")]
    public async Task Should_return_200_OK_with_single_scope_without_spaces()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AllowedScopes = ["api", "api.read"];
        var configuration = GivenConfiguration(route);
        await GivenThereIsExternalJwtSigningService(route.AuthenticationOptions.AllowedScopes.ToArray());
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Single scope no spaces");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);

        // Generate token with single scope (no spaces) - should not be affected
        await GivenIHaveAToken(scope: "api.read");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Single scope no spaces");
    }

    private static void Void() { }

    private async Task GivenIHaveATokenWithScope(string scope, [CallerMemberName] string testName = "")
        => await GivenIHaveAToken(scope, null, JwtSigningServerUrl, null, testName);
    private async Task GivenIHaveATokenWithClaims(IEnumerable<KeyValuePair<string, string>> claims, [CallerMemberName] string testName = "")
        => await GivenIHaveAToken(OcelotScopes.Api, claims, JwtSigningServerUrl, null, testName);
}
