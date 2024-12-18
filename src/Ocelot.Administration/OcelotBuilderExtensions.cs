using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;

namespace Ocelot.Administration;

public static class OcelotBuilderExtensions
{
    public static IOcelotAdministrationBuilder AddAdministration(this IOcelotBuilder builder, string path, string secret)
    {
        var administrationPath = new AdministrationPath(path);

        builder.Services.AddSingleton(IdentityServerMiddlewareConfigurationProvider.Get);

        //add identity server for admin area
        var identityServerConfiguration = IdentityServerConfigurationCreator.GetIdentityServerConfiguration(secret);

        if (identityServerConfiguration != null)
        {
            AddIdentityServer(identityServerConfiguration, administrationPath, builder, builder.Configuration);
        }

        builder.Services.AddSingleton<IAdministrationPath>(administrationPath);
        return new OcelotAdministrationBuilder(builder.Services, builder.Configuration);
    }

    public static IOcelotAdministrationBuilder AddAdministration(this IOcelotBuilder builder, string path, Action<JwtBearerOptions> configureOptions)
    {
        var administrationPath = new AdministrationPath(path);
        builder.Services.AddSingleton(IdentityServerMiddlewareConfigurationProvider.Get);

        if (configureOptions != null)
        {
            AddIdentityServer(configureOptions, builder);
        }

        builder.Services.AddSingleton<IAdministrationPath>(administrationPath);
        return new OcelotAdministrationBuilder(builder.Services, builder.Configuration);
    }

    private static void AddIdentityServer(Action<JwtBearerOptions> configOptions, IOcelotBuilder builder)
    {
        builder.Services
            .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
            .AddJwtBearer("Bearer", configOptions);
    }

    private static void AddIdentityServer(IdentityServerConfiguration identityServerConfiguration, AdministrationPath adminPath, IOcelotBuilder builder, IConfiguration configuration)
    {
        builder.Services.TryAddSingleton<IIdentityServerConfiguration>(identityServerConfiguration);
        var identityServerBuilder = builder.Services
            .AddIdentityServer(o =>
            {
                o.IssuerUri = "Ocelot";
                o.EmitStaticAudienceClaim = true;
            })
            .AddInMemoryApiScopes(ApiScopes(identityServerConfiguration))
            .AddInMemoryApiResources(Resources(identityServerConfiguration))
            .AddInMemoryClients(Client(identityServerConfiguration));

        var urlFinder = new BaseUrlFinder(configuration);
        var baseSchemeUrlAndPort = urlFinder.Find();
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        builder.Services
            .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = baseSchemeUrlAndPort + adminPath.Path;
                options.RequireHttpsMetadata = identityServerConfiguration.RequireHttps;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                };
            });

        //todo - refactor naming..
        if (string.IsNullOrEmpty(identityServerConfiguration.CredentialsSigningCertificateLocation) || string.IsNullOrEmpty(identityServerConfiguration.CredentialsSigningCertificatePassword))
        {
            identityServerBuilder.AddDeveloperSigningCredential();
        }
        else
        {
            //todo - refactor so calls method?
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SYSLIB0057 // Type or member is obsolete
            // TODO: Refactor the code to phase out IdentityServer4 in favor of its successor or replace with ASP.NET Identity framework
            var cert = new X509Certificate2(identityServerConfiguration.CredentialsSigningCertificateLocation, identityServerConfiguration.CredentialsSigningCertificatePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
#pragma warning restore IDE0079 // Remove unnecessary suppression
            identityServerBuilder.AddSigningCredential(cert);
        }
    }

    private static IEnumerable<ApiScope> ApiScopes(IdentityServerConfiguration configuration)
        => configuration.AllowedScopes.Select(s => new ApiScope(s));

    private static List<ApiResource> Resources(IdentityServerConfiguration configuration) => new()
    {
        new(configuration.ApiName, configuration.ApiName)
        {
            ApiSecrets = new List<Secret>
            {
                new()
                {
                    Value = configuration.ApiSecret.Sha256(),
                },
            },
        },
    };

    private static List<Client> Client(IdentityServerConfiguration configuration) => new()
    {
        new()
        {
            ClientId = configuration.ApiName,
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = new List<Secret> {new(configuration.ApiSecret.Sha256())},
            AllowedScopes = configuration.AllowedScopes,
        },
    };
}
