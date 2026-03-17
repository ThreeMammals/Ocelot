using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Authorization;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Shouldly;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Ocelot.Testing.Authentication;

public class AuthenticationSteps : AcceptanceSteps
{
    protected BearerToken? token;
    private readonly Dictionary<string, WebApplication> _jwtSigningServers;
    public string JwtSigningServerUrl { get => _jwtSigningServers.First().Key; }

    public AuthenticationSteps() : base()
    {
        _jwtSigningServers = [];
    }

    public override void Dispose()
    {
        foreach (var kv in _jwtSigningServers)
        {
            IDisposable server = _jwtSigningServers[kv.Key];
            server?.Dispose();
        }
        _jwtSigningServers.Clear();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void WithThreemammalsOptions(JwtBearerOptions o)
    {
        o.Audience = AuthToken.Audience; // "threemammals.com";
        o.Authority = new Uri(JwtSigningServerUrl).Authority;
        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = new Uri(JwtSigningServerUrl).Authority,
            ValidateAudience = true,
            ValidAudience = ocelotClient?.BaseAddress?.Authority,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = AuthToken.IssuerSigningKey(),
        };
    }

    public void WithJwtBearerAuthentication(IServiceCollection services)
        => WithJwtBearerAuthentication(services, true);
    public void WithJwtBearerAuthentication(IServiceCollection services, bool addOcelot)
    {
        if (addOcelot) services.AddOcelot();
        services.AddAuthentication().AddJwtBearer(WithThreemammalsOptions);
    }

    public static /*IHost*/ WebApplication CreateJwtSigningServer(string url, string[] apiScopes)
    {
        apiScopes ??= [OcelotScopes.Api];
        var builder = TestWebBuilder.CreateSlimBuilder();
        builder.WebHost.UseUrls(url);
        builder.Services
            .AddLogging()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "threemammals.com", // see mycert2.pfx
                    ValidAudience = "threemammals.com",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Ocelot.AcceptanceTests.Authentication")),
                };
            });
        var app = builder.Build();
        app.MapGet("/connect", () => "Hello! Connected!");
        app.MapPost("/token", (AuthenticationTokenRequest model) =>
        {
            // The signing server should be eligible to sign predefined claims as specified in its configuration.
            // If an unknown scope or claim is requested for inclusion in a JWT, the server should reject the request.
            // Therefore, the server configuration should be well-known to the client; otherwise, it poses a security risk.
            if (!apiScopes.Intersect(model.Scopes?.Split(' ') ?? []).Any())
            {
                return Results.BadRequest();
            }
            var token = GenerateToken(url, model);
            return Results.Json(token);
        });
        return app;
    }

    protected static async Task VerifyJwtSigningServerStarted(string url, CancellationToken token, HttpClient? client = null)
    {
        client ??= new HttpClient();
        var response = await client.GetAsync($"{url}/connect", token);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(token);
        json.ShouldNotBeNullOrEmpty();
    }

    public Task<string> GivenThereIsExternalJwtSigningService(string[] extraScopes, CancellationToken token)
    {
        List<string> scopes = [OcelotScopes.Api, OcelotScopes.Api2];
        scopes.AddRange(extraScopes);
        var url = DownstreamUrl(PortFinder.GetRandomPort());
        var server = CreateJwtSigningServer(url, [.. scopes]);
        _jwtSigningServers.Add(url, server);
        return server.StartAsync(token)
            .ContinueWith(t => VerifyJwtSigningServerStarted(url, token), token)
            .ContinueWith(t => url, token);
    }

    public void GivenIHaveAddedATokenToMyRequest() => GivenIHaveAddedATokenToMyRequest(token);
    public void GivenIHaveAddedATokenToMyRequest(BearerToken? token)
        => GivenIHaveAddedATokenToMyRequest(token?.AccessToken ?? string.Empty, JwtBearerDefaults.AuthenticationScheme);

    public AuthenticationTokenRequest GivenAuthTokenRequest(string scope,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        [CallerMemberName] string testName = "")
    {
        var auth = new AuthenticationTokenRequest()
        {
            Audience = ocelotClient?.BaseAddress?.Authority ?? string.Empty, // Ocelot DNS is token audience
            ApiSecret = testName, // "secret",
            Scopes = scope ?? OcelotScopes.Api,
            Claims = claims is null ? new() : new(claims),
            UserId = testName,
            UserName = testName,
        };
        return auth;
    }

    public Task<BearerToken?> GivenIHaveAToken([CallerMemberName] string testName = "")
        => GivenIHaveAToken(OcelotScopes.Api, null, JwtSigningServerUrl, null, testName);

    public async Task<BearerToken?> GivenIHaveAToken(string scope,
        IEnumerable<KeyValuePair<string, string>>? claims = null,
        string? issuerUrl = null,
        string? audience = null,
        [CallerMemberName] string testName = "")
    {
        var auth = GivenAuthTokenRequest(scope, claims, testName);
        auth.Audience = audience ?? ocelotClient?.BaseAddress?.Authority ?? string.Empty;
        return token = await GivenToken(auth, string.Empty, issuerUrl);
    }
    public async Task<BearerToken?> GivenIHaveATokenWithUrlPath(string path, string scope, [CallerMemberName] string testName = "")
    {
        var auth = GivenAuthTokenRequest(scope, null!, testName);
        return token = await GivenToken(auth, path);
    }

    public readonly Dictionary<string, AuthenticationTokenRequest> AuthTokens = [];
    public AuthenticationTokenRequest AuthToken => AuthTokens.Count > 0 ? AuthTokens.First().Value : new();
    public event EventHandler<AuthenticationTokenRequestEventArgs>? AuthTokenRequesting;
    protected virtual void OnAuthenticationTokenRequest(AuthenticationTokenRequestEventArgs e)
        => AuthTokenRequesting?.Invoke(this, e);

    protected async Task<BearerToken?> GivenToken(AuthenticationTokenRequest auth, string path = "", string? issuerUrl = null)
    {
        using var http = new HttpClient();
        issuerUrl ??= JwtSigningServerUrl;

        AuthTokens[issuerUrl] = auth;
        OnAuthenticationTokenRequest(new(auth));

        var tokenUrl = $"{issuerUrl + path}/token";
        var content = JsonContent.Create(auth);
        var response = await http.PostAsync(tokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<BearerToken>(responseContent, JsonSerializerOptions.Web);
    }

    protected FileRoute GivenAuthRoute(int port, string path, FileAuthenticationOptions options)
    {
        FileRoute? r = GivenRoute(port, path, path) as FileRoute;
        r!.AuthenticationOptions = options;
        return r;
    }

    public FileRoute GivenAuthRoute(int port,
        string scheme = JwtBearerDefaults.AuthenticationScheme,
        bool allowAnonymous = false,
        string[]? scopes = null,
        string? method = null)
    {
        // FileRoute r = GivenDefaultRoute(port)?.WithMethods(method ?? HttpMethods.Get) as FileRoute;
        FileRoute r = GivenDefaultRoute(port);
        r.UpstreamHttpMethod.Add(method ?? HttpMethods.Get);
        r.AuthenticationOptions = new(scheme)
        {
            AllowAnonymous = allowAnonymous,
            AllowedScopes = scopes?.ToList(),
        };
        return r;
    }

    public static FileGlobalConfiguration GivenGlobalAuthConfiguration(
        string scheme = JwtBearerDefaults.AuthenticationScheme,
        string[]? allowedScopes = null)
        => new()
        {
            AuthenticationOptions = new()
            {
                AllowedScopes = [.. allowedScopes ?? []],
                AuthenticationProviderKeys = [scheme],
            },
        };

    //private IConfiguration _config;
    private readonly UserManager<IdentityUser>? _userManager = default;
    public async Task<BearerToken> GenerateTokenAsync(IdentityUser user, string issuer, string audience, string secretKey)
    {
        var userClaims = await _userManager?.GetClaimsAsync(user)!;
        var roles = await _userManager.GetRolesAsync(user);
        var roleClaims = roles
            .Select(role => new Claim(ClaimTypes.Role, role));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        }
        .Union(userClaims)
        .Union(roleClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(1);
        var token = new JwtSecurityToken(
            issuer: issuer, //_config["Jwt:Issuer"],
            audience: audience, // _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        BearerToken bt = new()
        {
            AccessToken = jwt,
            ExpiresIn = (int)(expiry - DateTime.UtcNow).TotalSeconds,
            TokenType = JwtBearerDefaults.AuthenticationScheme,
        };
        return bt;
    }

    private static bool IsRoleKey(KeyValuePair<string, string> kv)
        => nameof(ClaimTypes.Role).Equals(kv.Key, StringComparison.OrdinalIgnoreCase)
            || ClaimTypes.Role.Equals(kv.Key);
    private static bool IsNotRoleKey(KeyValuePair<string, string> kv)
        => !IsRoleKey(kv);

    public static BearerToken GenerateToken(string issuerUrl, AuthenticationTokenRequest auth)
    {
        if (auth is null)
            return new();

        var userClaims = auth.Claims // await _userManager.GetClaimsAsync(user);
            .Where(IsNotRoleKey)
            .Select(kv => new Claim(kv.Key, kv.Value))
            .ToList();
        var roleClaims = auth.Claims // await _userManager.GetRolesAsync(user);
            .Where(IsRoleKey)
            .Select(kv => new Claim(/*ClaimTypes.Role*/kv.Key, kv.Value)) // ClaimTypes.Role is not supported, see AuthorizationTests.Should_fix_issue_240
            .ToList();
        var claims = new List<Claim>(4 + auth.Claims.Count)
        {
            new(JwtRegisteredClaimNames.Sub, auth.UserId ?? string.Empty),
            new(OcelotClaims.OcSub, auth.UserId ?? string.Empty), // this is a handy lifehack to fix current authorization services like IScopesAuthorizer and IClaimsAuthorizer, which don't support JWT standard and claim types in URL form, aka the ':' delimiter issue with the JSON configuration provider
            new(JwtRegisteredClaimNames.Email, $"{auth.UserName}@ocelot.net"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ScopesAuthorizer.Scope, auth.Scopes ?? string.Empty),
        };
        claims.AddRange(roleClaims);
        claims.AddRange(userClaims);

        var credentials = new SigningCredentials(auth.IssuerSigningKey(), SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(1);
        var token = new JwtSecurityToken(
            issuer: new Uri(issuerUrl).Authority, // URL http://localhost:1234 -> DNS localhost:1234 //_config["Jwt:Issuer"],
            audience: auth.Audience, // _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );
        var jwt = string.Empty;
        try
        {
            jwt = new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            jwt = ex.Message;
        }
        BearerToken bt = new()
        {
            AccessToken = jwt,
            ExpiresIn = (int)(expiry - DateTime.UtcNow).TotalSeconds,
            TokenType = JwtBearerDefaults.AuthenticationScheme,
        };
        return bt;
    }

    public static FileAuthenticationOptions GivenOptions(bool? allowAnonymous = null,
        List<string>? allowedScopes = null, string[]? schemes = null)
        => new()
        {
            AllowAnonymous = allowAnonymous,
            AllowedScopes = allowedScopes,
            AuthenticationProviderKeys = schemes,
        };
}
