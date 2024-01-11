using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Ocelot.AcceptanceTests.Authentication;

public class AuthenticationSteps : Steps
{
    protected static ApiResource CreateApiResource(
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

    protected static IWebHostBuilder CreateIdentityServer(string url, AccessTokenType tokenType, params string[] apiScopes)
    {
        apiScopes ??= Array.Empty<string>();
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
                        .Select(apiname => new ApiScope(apiname, "test")))
                    .AddInMemoryApiResources(apiScopes
                        .Select(x => new { i = Array.IndexOf(apiScopes, x), scope = x })
                        .Select(x => CreateApiResource(x.scope,
                            x.i % 2 == 0 ?["openid", "offline_access"] :[])))
                    .AddInMemoryClients(new List<Client>
                    {
                        new()
                        {
                            ClientId = "client",
                            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                            ClientSecrets = new List<Secret> { new("secret".Sha256()) },
                            AllowedScopes = apiScopes
                                .Union(apiScopes.Select(x => $"{x}.readOnly"))
                                .Union(["openid", "offline_access"])
                                .ToList(),
                            AccessTokenType = tokenType,
                            Enabled = true,
                            RequireClientSecret = false,
                        },
                    })
                    .AddTestUsers(
                    [
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
                    ]);
            })
            .Configure(app =>
            {
                app.UseIdentityServer();
            });
        return builder;
    }

    public async Task GivenIHaveATokenWithScope(string url, string apiScope = "api")
    {
        var form = GivenDefaultAuthTokenForm();
        form.RemoveAt(form.FindIndex(x => x.Key == "scope"));
        form.Add(new("scope", apiScope));

        await GivenIHaveATokenWithForm(url, form);
    }
}
