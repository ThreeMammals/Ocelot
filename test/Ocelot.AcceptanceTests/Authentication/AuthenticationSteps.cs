using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using System.Security.Claims;

namespace Ocelot.AcceptanceTests.Authentication;

public class AuthenticationSteps : Steps, IDisposable
{
    private readonly ServiceHandler _serviceHandler;

    public AuthenticationSteps() : base()
    {
        _serviceHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    public static ApiResource CreateApiResource(
        string apiName,
        IEnumerable<string> extraScopes = null) => new()
    {
        Name = apiName,
        Description = $"My {apiName} API",
        Enabled = true,
        DisplayName = "test",
        Scopes = new List<string>(extraScopes ?? Enumerable.Empty<string>())
        {
            apiName,
            $"{apiName}.readOnly",
        },
        ApiSecrets = new List<Secret>
        {
            new ("secret".Sha256()),
        },
        UserClaims = new List<string>
        {
            "CustomerId", "LocationId",
        },
    };

    protected static Client CreateClientWithSecret(string clientId, Secret secret, AccessTokenType tokenType = AccessTokenType.Jwt, string[] apiScopes = null)
    {
        var client = DefaultClient(tokenType, apiScopes);
        client.ClientId = clientId ?? "client";
        client.ClientSecrets = new Secret[] { secret };
        return client;
    }

    protected static Client DefaultClient(AccessTokenType tokenType = AccessTokenType.Jwt, string[] apiScopes = null)
    {
        apiScopes ??= new string[] { "api" };
        return new()
        {
            ClientId = "client",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            ClientSecrets = new List<Secret> { new("secret".Sha256()) },
            AllowedScopes = apiScopes
                .Union(apiScopes.Select(x => $"{x}.readOnly"))
                .Union(new string[] { "openid", "offline_access" })
                .ToList(),
            AccessTokenType = tokenType,
            Enabled = true,
            RequireClientSecret = false,
            RefreshTokenExpiration = TokenExpiration.Absolute,
        };
    }

    public static IWebHostBuilder CreateIdentityServer(string url, AccessTokenType tokenType, string[] apiScopes, Client[] clients)
    {
        apiScopes ??= new string[] { "api" };
        clients ??= new Client[] { DefaultClient(tokenType, apiScopes) };
        var builder = new WebHostBuilder()
            .UseUrls(url)
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseUrls(url)
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddIdentityServer()
                    .AddDeveloperSigningCredential()
                    .AddInMemoryApiScopes(apiScopes
                        .Select(apiname => new ApiScope(apiname, apiname.ToUpper())))
                    .AddInMemoryApiResources(apiScopes
                        .Select(x => new { i = Array.IndexOf(apiScopes, x), scope = x })
                        .Select(x => CreateApiResource(x.scope, new string[] { "openid", "offline_access" })))
                    .AddInMemoryClients(clients)
                    .AddTestUsers(new()
                    {
                        new()
                        {
                            Username = "test",
                            Password = "test",
                            SubjectId = "registered|1231231",
                            Claims = new List<Claim>
                            {
                                   new("CustomerId", "123"),
                                   new("LocationId", "321"),
                            },
                        },
                    });
            })
            .Configure(app =>
            {
                app.UseIdentityServer();
            });
        return builder;
    }

    internal Task<BearerToken> GivenAuthToken(string url, string apiScope)
    {
        var form = GivenDefaultAuthTokenForm();
        form.RemoveAll(x => x.Key == "scope");
        form.Add(new("scope", apiScope));
        return GivenIHaveATokenWithForm(url, form);
    }

    internal Task<BearerToken> GivenAuthToken(string url, string apiScope, string client)
    {
        var form = GivenDefaultAuthTokenForm();

        form.RemoveAll(x => x.Key == "scope");
        form.Add(new("scope", apiScope));

        form.RemoveAll(x => x.Key == "client_id");
        form.Add(new("client_id", client));

        return GivenIHaveATokenWithForm(url, form);
    }

    public static FileRoute GivenDefaultAuthRoute(int port, string upstreamHttpMethod = null, string authProviderKey = null) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamHostAndPorts = new()
        {
            new("localhost", port),
        },
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = new() { upstreamHttpMethod ?? HttpMethods.Get },
        AuthenticationOptions = new()
        {
            AuthenticationProviderKeys = new string[] { authProviderKey ?? "Test" },
        },
    };

    protected void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, string responseBody)
    {
        var url = DownstreamServiceUrl(port);
        GivenThereIsAServiceRunningOn(url, statusCode, responseBody);
    }

    protected void GivenThereIsAServiceRunningOn(string url, HttpStatusCode statusCode, string responseBody)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
        {
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(responseBody);
        });
    }

    protected static string DownstreamServiceUrl(int port) => string.Concat("http://localhost:", port);
}
