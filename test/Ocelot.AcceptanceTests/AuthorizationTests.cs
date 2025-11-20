//using IdentityServer4.AccessTokenValidation;
//using IdentityServer4.Models;
//using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Configuration.File;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Policy;

namespace Ocelot.AcceptanceTests;

public sealed class AuthorizationTests : AuthenticationSteps
{
    //private readonly IWebHost _identityServerBuilder;
    //private readonly Action<IdentityServerAuthenticationOptions> _options;
    private readonly string _identityServerRootUrl;

    public AuthorizationTests()
    {
        var identityServerPort = PortFinder.GetRandomPort();
        _identityServerRootUrl = $"http://localhost:{identityServerPort}";

        //_options = o =>
        //{
        //    o.Authority = _identityServerRootUrl;
        //    o.ApiName = "api";
        //    o.RequireHttpsMetadata = false;
        //    o.SupportedTokens = SupportedTokens.Both;
        //    o.ApiSecret = "secret";
        //};
    }

    private string Name([CallerMemberName] string testName = "") => testName;

    [Fact(Skip = "TODO: Requires redevelopment because IdentityServer4 is deprecated")]
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
        var testName = Name();
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIHaveAToken(testName))
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact(Skip = "TODO: Requires redevelopment because IdentityServer4 is deprecated")]
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
        var testName = Name();
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
        await GivenThereIsAnIdentityServer();
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);

        await GivenIHaveAToken(scope: "api.readOnly");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
    }

    [Fact(Skip = "TODO: Requires redevelopment because IdentityServer4 is deprecated")]
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
                        AllowedScopes = new List<string>{ "api", "openid", "offline_access" },
                    },
                },
            },
        };
        var testName = Name();
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIHaveATokenWithScope("api.readOnly", testName))
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .BDDfy();
    }

    [Trait("Bug", "240")]
    [Fact(Skip = "TODO: Requires redevelopment because IdentityServer4 is deprecated")]
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
                    RouteClaimsRequirement =
                    {
                        {"Role", "User"},
                    },
                },
            },
        };

        //var users = new List<TestUser>
        //{
        //    new()
        //    {
        //        Username = "test",
        //        Password = "test",
        //        SubjectId = "registered|1231231",
        //        Claims = new List<Claim>
        //        {
        //            new("Role", "AdminUser"),
        //            new("Role", "User"),
        //        },
        //    },
        //};
        var testName = Name();
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt, users))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIHaveAToken(testName))
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
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

        await GivenThereIsAnIdentityServer();
        GivenThereIsAServiceRunningOn(port);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);
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
        await GivenThereIsAnIdentityServer();
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from space-separated test");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);

        // Generate token with space-separated scopes
        await GivenIHaveAToken(scope: "api.read api.write openid");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
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
        await GivenThereIsAnIdentityServer();
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Multiple scopes matched");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);

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
        await GivenThereIsAnIdentityServer();
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Should not reach here");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);

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
        await GivenThereIsAnIdentityServer();
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Extra spaces handled");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);

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
        await GivenThereIsAnIdentityServer();
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Single scope no spaces");

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);

        // Generate token with single scope (no spaces) - should not be affected
        await GivenIHaveAToken(scope: "api.read");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Single scope no spaces");
    }

    private static void Void() { }

    //private async Task GivenThereIsAnIdentityServerOn(string url, string apiName, AccessTokenType tokenType)
    //{
    //    _identityServerBuilder = TestHostBuilder.Create()
    //        .UseUrls(url)
    //        .UseKestrel()
    //        .UseContentRoot(Directory.GetCurrentDirectory())
    //        .UseIISIntegration()
    //        .UseUrls(url)
    //        .ConfigureServices(services =>
    //        {
    //            services.AddLogging();
    //            services.AddIdentityServer()
    //                .AddDeveloperSigningCredential()
    //                .AddInMemoryApiScopes(new List<ApiScope>
    //                {
    //                    new(apiName, "test"),
    //                    new("openid", "test"),
    //                    new("offline_access", "test"),
    //                    new("api.readOnly", "test"),
    //                })
    //                .AddInMemoryApiResources(new List<ApiResource>
    //                {
    //                    new()
    //                    {
    //                        Name = apiName,
    //                        Description = "My API",
    //                        Enabled = true,
    //                        DisplayName = "test",
    //                        Scopes = new List<string>
    //                        {
    //                            "api",
    //                            "api.readOnly",
    //                            "openid",
    //                            "offline_access",
    //                        },
    //                        ApiSecrets = new List<Secret>
    //                        {
    //                            new()
    //                            {
    //                                Value = "secret".Sha256(),
    //                            },
    //                        },
    //                        UserClaims = new List<string>
    //                        {
    //                            "CustomerId", "LocationId", "UserType", "UserId",
    //                        },
    //                    },
    //                })
    //                .AddInMemoryClients(new List<Client>
    //                {
    //                    new()
    //                    {
    //                        ClientId = "client",
    //                        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
    //                        ClientSecrets = new List<Secret> {new("secret".Sha256())},
    //                        AllowedScopes = new List<string> { apiName, "api.readOnly", "openid", "offline_access" },
    //                        AccessTokenType = tokenType,
    //                        Enabled = true,
    //                        RequireClientSecret = false,
    //                    },
    //                })
    //                .AddTestUsers(new List<TestUser>
    //                {
    //                    new()
    //                    {
    //                        Username = "test",
    //                        Password = "test",
    //                        SubjectId = "registered|1231231",
    //                        Claims = new List<Claim>
    //                        {
    //                           new("CustomerId", "123"),
    //                           new("LocationId", "321"),
    //                        },
    //                    },
    //                });
    //        })
    //        .Configure(app =>
    //        {
    //            app.UseIdentityServer();
    //        })
    //        .Build();

    //    await _identityServerBuilder.StartAsync();

    //    await Steps.VerifyIdentityServerStarted(url);
    //}

    //private async Task GivenThereIsAnIdentityServerOn(string url, string apiName, AccessTokenType tokenType, List<TestUser> users)
    //{
    //    _identityServerBuilder = TestHostBuilder.Create()
    //        .UseUrls(url)
    //        .UseKestrel()
    //        .UseContentRoot(Directory.GetCurrentDirectory())
    //        .UseIISIntegration()
    //        .UseUrls(url)
    //        .ConfigureServices(services =>
    //        {
    //            services.AddLogging();
    //            services.AddIdentityServer()
    //                .AddDeveloperSigningCredential()
    //                .AddInMemoryApiScopes(new List<ApiScope>
    //                {
    //                    new(apiName, "test"),
    //                })
    //                .AddInMemoryApiResources(new List<ApiResource>
    //                {
    //                    new()
    //                    {
    //                        Name = apiName,
    //                        Description = "My API",
    //                        Enabled = true,
    //                        DisplayName = "test",
    //                        Scopes = new List<string>
    //                        {
    //                            "api",
    //                            "api.readOnly",
    //                            "openid",
    //                            "offline_access",
    //                        },
    //                        ApiSecrets = new List<Secret>
    //                        {
    //                            new()
    //                            {
    //                                Value = "secret".Sha256(),
    //                            },
    //                        },
    //                        UserClaims = new List<string>
    //                        {
    //                            "CustomerId", "LocationId", "UserType", "UserId", "Role",
    //                        },
    //                    },
    //                })
    //                .AddInMemoryClients(new List<Client>
    //                {
    //                    new()
    //                    {
    //                        ClientId = "client",
    //                        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
    //                        ClientSecrets = new List<Secret> {new("secret".Sha256())},
    //                        AllowedScopes = new List<string> { apiName, "api.readOnly", "openid", "offline_access" },
    //                        AccessTokenType = tokenType,
    //                        Enabled = true,
    //                        RequireClientSecret = false,
    //                    },
    //                })
    //                .AddTestUsers(users);
    //        })
    //        .Configure(app =>
    //        {
    //            app.UseIdentityServer();
    //        })
    //        .Build();

    //    await _identityServerBuilder.StartAsync();

    //    await Steps.VerifyIdentityServerStarted(url);
    //}
    private async Task GivenIHaveATokenWithScope(string scope, [CallerMemberName] string testName = "")
        => await GivenIHaveAToken(scope: scope, testName);

    public override void Dispose()
    {
        //_identityServerBuilder?.Dispose();
        base.Dispose();
    }
}
