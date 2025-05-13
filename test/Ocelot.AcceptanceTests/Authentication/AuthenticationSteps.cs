//using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using System.Security.Claims;

namespace Ocelot.AcceptanceTests.Authentication;

public class AuthenticationSteps : Steps
{
    protected BearerToken token;

    public AuthenticationSteps() : base()
    {
    }

    //public static ApiResource CreateApiResource(
    //    string apiName,
    //    IEnumerable<string> extraScopes = null) => new()
    //{
    //    Name = apiName,
    //    Description = $"My {apiName} API",
    //    Enabled = true,
    //    DisplayName = "test",
    //    Scopes = new List<string>(extraScopes ?? Enumerable.Empty<string>())
    //    {
    //        apiName,
    //        $"{apiName}.readOnly",
    //    },
    //    ApiSecrets = new List<Secret>
    //    {
    //        new ("secret".Sha256()),
    //    },
    //    UserClaims = new List<string>
    //    {
    //        "CustomerId", "LocationId",
    //    },
    //};

    //protected static Client CreateClientWithSecret(string clientId, Secret secret, AccessTokenType tokenType = AccessTokenType.Jwt, string[] apiScopes = null)
    //{
    //    var client = DefaultClient(tokenType, apiScopes);
    //    client.ClientId = clientId ?? "client";
    //    client.ClientSecrets = new Secret[] { secret };
    //    return client;
    //}

    //protected static Client DefaultClient(AccessTokenType tokenType = AccessTokenType.Jwt, string[] apiScopes = null)
    //{
    //    apiScopes ??= new string[] { "api" };
    //    return new()
    //    {
    //        ClientId = "client",
    //        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
    //        ClientSecrets = new List<Secret> { new("secret".Sha256()) },
    //        AllowedScopes = apiScopes
    //            .Union(apiScopes.Select(x => $"{x}.readOnly"))
    //            .Union(new string[] { "openid", "offline_access" })
    //            .ToList(),
    //        AccessTokenType = tokenType,
    //        Enabled = true,
    //        RequireClientSecret = false,
    //        RefreshTokenExpiration = TokenExpiration.Absolute,
    //    };
    //}

    //public static IWebHostBuilder CreateIdentityServer(string url, AccessTokenType tokenType, string[] apiScopes, Client[] clients)
    //{
    //    apiScopes ??= new string[] { "api" };
    //    clients ??= new Client[] { DefaultClient(tokenType, apiScopes) };
    //    var builder = TestHostBuilder.Create()
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
    //                .AddInMemoryApiScopes(apiScopes
    //                    .Select(apiname => new ApiScope(apiname, apiname.ToUpper())))
    //                .AddInMemoryApiResources(apiScopes
    //                    .Select(x => new { i = Array.IndexOf(apiScopes, x), scope = x })
    //                    .Select(x => CreateApiResource(x.scope, new string[] { "openid", "offline_access" })))
    //                .AddInMemoryClients(clients)
    //                .AddTestUsers(new()
    //                {
    //                    new()
    //                    {
    //                        Username = "test",
    //                        Password = "test",
    //                        SubjectId = "registered|1231231",
    //                        Claims = new List<Claim>
    //                        {
    //                               new("CustomerId", "123"),
    //                               new("LocationId", "321"),
    //                        },
    //                    },
    //                });
    //        })
    //        .Configure(app =>
    //        {
    //            app.UseIdentityServer();
    //        });
    //    return builder;
    //}
    protected void GivenIHaveAddedATokenToMyRequest() => GivenIHaveAddedATokenToMyRequest(token);
    public void GivenIHaveAddedATokenToMyRequest(BearerToken token) => GivenIHaveAddedATokenToMyRequest(token.AccessToken, "Bearer");

    public static List<KeyValuePair<string, string>> GivenDefaultAuthTokenForm() => new()
    {
        new ("client_id", "client"),
        new ("client_secret", "secret"),
        new ("scope", "api"),
        new ("username", "test"),
        new ("password", "test"),
        new ("grant_type", "password"),
    };

    public async Task<BearerToken> GivenIHaveAToken(string url)
    {
        var form = GivenDefaultAuthTokenForm();
        return token = await GivenIHaveATokenWithForm(url, form);
    }

    public static async Task<BearerToken> GivenIHaveATokenWithForm(string url, IEnumerable<KeyValuePair<string, string>> form)
    {
        var tokenUrl = $"{url}/connect/token";
        var formData = form ?? Enumerable.Empty<KeyValuePair<string, string>>();
        var content = new FormUrlEncodedContent(formData);

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(tokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<BearerToken>(responseContent) ?? new();
    }

    internal async Task<BearerToken> GivenAuthToken(string url, string apiScope)
    {
        var form = GivenDefaultAuthTokenForm();
        form.RemoveAll(x => x.Key == "scope");
        form.Add(new("scope", apiScope));
        return token = await GivenIHaveATokenWithForm(url, form);
    }

    internal static Task<BearerToken> GivenAuthToken(string url, string apiScope, string client)
    {
        var form = GivenDefaultAuthTokenForm();

        form.RemoveAll(x => x.Key == "scope");
        form.Add(new("scope", apiScope));

        form.RemoveAll(x => x.Key == "client_id");
        form.Add(new("client_id", client));

        return GivenIHaveATokenWithForm(url, form);
    }

    public static FileRoute GivenAuthRoute(int port, string upstreamHttpMethod = null, string authProviderKey = null)
    {
        var r = GivenDefaultRoute(port).WithMethods(upstreamHttpMethod ?? HttpMethods.Get);
        r.AuthenticationOptions.AuthenticationProviderKeys = [authProviderKey ?? "Test"];
        return r;
    }

    protected void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, string responseBody)
    {
        Task MapStatus(HttpContext context)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(responseBody);
        }
        handler.GivenThereIsAServiceRunningOn(port, MapStatus);
    }
}
