using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Authorization;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using System.Buffers;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ocelot.AcceptanceTests.Authentication;

public class AuthenticationSteps : Steps
{
    public const string DefaultScope = "api";
    public const string DefaultScope2 = "api2";

    protected BearerToken token;
    private /*IWebHost*/WebApplication _identityServer;
    private readonly string _identityServerUrl;

    public AuthenticationSteps() : base()
    {
        var port = PortFinder.GetRandomPort();
        _identityServerUrl = DownstreamUrl(port);
    }

    public override void Dispose()
    {
        //_identityServer?.Dispose();
        _identityServer.DisposeAsync().AsTask().Wait();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    private void WithOptions(JwtBearerOptions o)
    {
        o.Audience = _authToken.Audience; // "threemammals.com";
        o.Authority = new Uri(_identityServerUrl).Authority;
        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = new Uri(_identityServerUrl).Authority,
            ValidateAudience = true,
            ValidAudience = ocelotClient.BaseAddress.Authority,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _authToken.IssuerSigningKey,
        };
    }

    protected void WithAspNetIdentityAuthentication(IServiceCollection services)
    {
        services.AddOcelot();
        services.AddAuthentication().AddJwtBearer(WithOptions);
    }

    public static /*IWebHost*/WebApplication CreateAspNetIdentityServer(string url, string[] apiScopes)
    {
        apiScopes ??= [ DefaultScope ];
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
                    ValidIssuer = "threemammals.com", // see mycert.pfx
                    ValidAudience = "threemammals.com",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Ocelot.AcceptanceTests.Authentication")),
                };
            });
        var app = builder.Build();

        //app.UseMiddleware()
        app.MapGet("/connect", context =>
        {
            return context.Response.WriteAsync("Hello! Connected!");
        });
        app.Map("/connect/token", configuration =>
        {
            configuration.Run(async context =>
            {
                var response = context.Response;
                response.ContentType = "application/json";
                response.Headers.CacheControl = "no-cache";
                if (!HttpMethods.IsPost(context.Request.Method))
                {
                    await response.WriteAsync("No token!");
                }

                var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();

                var auth = JsonSerializer.Deserialize<AuthTokenRequest>(body, JsonSerializerOptions.Web);
                /*IdentityUser user = new(auth.UserName)
                {
                    Id = auth.UserId,
                };
                var token = await GenerateTokenAsync(user,
                    issuer: url,
                    audience: auth.Audience,
                    secretKey: auth.Password);*/
                var token = GenerateToken(url, auth);
                var json = JsonSerializer.Serialize(token, JsonSerializerOptions.Web);
                await response.WriteAsync(json);
            });
        });
        return app;
    }

    protected static async Task VerifyIdentityServerStarted(string url, HttpClient client = null)
    {
        client ??= new HttpClient();
        var response = await client.GetAsync($"{url}/connect");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldNotBeNullOrEmpty();
    }

    protected async Task GivenThereIsAnIdentityServer()
    {
        var scopes = new string[] { DefaultScope, DefaultScope2 };
        _identityServer = CreateAspNetIdentityServer(_identityServerUrl, scopes);
        await _identityServer.StartAsync();
        await VerifyIdentityServerStarted(_identityServerUrl);
    }

    protected void GivenIHaveAddedATokenToMyRequest() => GivenIHaveAddedATokenToMyRequest(token);
    protected void GivenIHaveAddedATokenToMyRequest(BearerToken token) => GivenIHaveAddedATokenToMyRequest(token.AccessToken, JwtBearerDefaults.AuthenticationScheme);

    public static AuthTokenRequest GivenAuthTokenRequest([CallerMemberName] string testName = "") => new()
    {
        UserId = testName,
        UserName = testName,
        Scope = DefaultScope,
        Password = testName, // "secret",
    };

    public class AuthTokenRequest
    {
        [JsonInclude]
        public string Audience { get; set; }
        [JsonInclude]
        public string UserId { get; set; }
        [JsonInclude]
        public string UserName { get; set; }
        [JsonInclude]
        public string Scope { get; set; }
        [JsonInclude]
        public string Password { get; set; }
        public SymmetricSecurityKey IssuerSigningKey { get; set; } // not serialized
    }

    public Task<BearerToken> GivenIHaveAToken([CallerMemberName] string testName = "")
        => GivenIHaveAToken(DefaultScope, testName);

    public async Task<BearerToken> GivenIHaveATokenWithUrlPath(string path, [CallerMemberName] string testName = "")
    {
        var auth = GivenAuthTokenRequest(testName);
        return token = await GivenToken(auth, path);
    }

    private AuthTokenRequest _authToken;
    public async Task<BearerToken> GivenToken(AuthTokenRequest auth, string path = "")
    {
        _authToken = auth;
        using var http = new HttpClient();
        var tokenUrl = $"{_identityServerUrl + path}/connect/token";
        var content = JsonContent.Create(auth);
        var response = await http.PostAsync(tokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<BearerToken>(responseContent, JsonSerializerOptions.Web);
    }

    protected async Task<BearerToken> GivenIHaveAToken(string scope, [CallerMemberName] string testName = "")
    {
        var auth = GivenAuthTokenRequest(testName);
        auth.Audience = ocelotClient.BaseAddress.Authority; // Ocelot DNS is token audience
        auth.Scope = scope;

        // System.ArgumentOutOfRangeException: 'IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'HS256', the key size must be greater than: '256' bits, key has '160' bits. (Parameter 'keyBytes')'
        // Make sure the security key is at least 32 characters long
        const int Size = 256 / 8;
        var securityKey = auth.Password.Length >= Size
            ? auth.Password
            : string.Join("", Enumerable.Repeat(auth.Password, Size / auth.Password.Length))
                + auth.Password[..(Size % auth.Password.Length)]; // total length should be 32 chars
        auth.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));

        return token = await GivenToken(auth);
    }

    public static FileRoute GivenAuthRoute(int port, string scheme = JwtBearerDefaults.AuthenticationScheme,
        bool allowAnonymous = false, string method = null)
    {
        var r = GivenDefaultRoute(port).WithMethods(method ?? HttpMethods.Get);
        r.AuthenticationOptions.AuthenticationProviderKeys = [scheme];
        r.AuthenticationOptions.AllowAnonymous = allowAnonymous;
        return r;
    }

    public static FileGlobalConfiguration GivenGlobalAuthConfiguration(
        string scheme = JwtBearerDefaults.AuthenticationScheme,
        string[] allowedScopes = null)
    {
        FileGlobalConfiguration c = new();
        c.AuthenticationOptions.AuthenticationProviderKeys = [scheme];
        c.AuthenticationOptions.AllowedScopes.AddRange(allowedScopes ?? []);
        return c;
    }

    //private IConfiguration _config;
    private readonly UserManager<IdentityUser> _userManager = default;
    public async Task<BearerToken> GenerateTokenAsync(IdentityUser user, string issuer, string audience, string secretKey)
    {
        var userClaims = await _userManager.GetClaimsAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var roleClaims = roles
            .Select(role => new Claim(ClaimTypes.Role, role));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
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

    public static BearerToken GenerateToken(string issuerUrl, AuthTokenRequest auth)
    {
        var userClaims = Array.Empty<Claim>(); // new Claim[] { new(ClaimTypes.Role, auth.Scope) }; // await _userManager.GetClaimsAsync(user);
        var roles = new List<string> { auth.Scope }; // await _userManager.GetRolesAsync(user);
        var roleClaims = roles
            .Select(role => new Claim(ClaimTypes.Role, role));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, auth.UserId),
            new(JwtRegisteredClaimNames.Email, auth.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ScopesAuthorizer.Scope, auth.Scope),
        }
        .Union(userClaims)
        .Union(roleClaims);

        var credentials = new SigningCredentials(auth.IssuerSigningKey, SecurityAlgorithms.HmacSha256);
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
}
