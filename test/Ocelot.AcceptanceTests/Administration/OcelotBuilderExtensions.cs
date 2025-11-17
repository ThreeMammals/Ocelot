using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Ocelot.Administration;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Middleware;
using OpenTracing;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.AcceptanceTests.Administration;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddAdministration(this IOcelotBuilder builder, string path, string apiSecret,
        Action<JwtBearerOptions> configureOptions = null, Uri externalJwtServer = null)
    {
        var administrationPath = new AdministrationPath(path, apiSecret, externalJwtServer);
        builder.Services
            .AddSingleton<IAdministrationPath>(administrationPath)
            .AddSingleton<OcelotMiddlewareConfigurationDelegate>(GetOcelotMiddlewareConfiguration);

        //var jwtServerConfiguration = GetIdentityServerConfiguration(secret);
        //AddIdentityServer(identityServerConfiguration, administrationPath, builder, builder.Configuration);
        var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        authBuilder = configureOptions is not null
            ? authBuilder.AddJwtBearer(configureOptions)
            : authBuilder.AddJwtBearer();
        return builder;
    }

    public static Task GetOcelotMiddlewareConfiguration(IApplicationBuilder builder)
    {
        var repo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
        var config = repo.Get();
        var administrationPath = config?.Data?.AdministrationPath;
        var administration = builder.ApplicationServices.GetService<IAdministrationPath>();
        if (administration.ExternalJwtSigningUrl != null)
        {
            builder.UseOcelotJwtServer(administration.ExternalJwtSigningUrl); // UseIdentityServer();
        }
        if (administrationPath.IsNotEmpty() && administration.Path.IsNotEmpty())
        {
            builder.Map(administrationPath, AddOcelotAdministrationControllers);
        }
        return Task.CompletedTask;
    }

    public static void AddOcelotAdministrationControllers(this IApplicationBuilder builder) => builder
        .UseAuthentication()
        .UseRouting()
        .UseAuthorization()
        .UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapControllers();
        });

    public static IApplicationBuilder UseOcelotJwtServer(this IApplicationBuilder app, Uri externalJwtSigningUrl, bool requireInstance = false)
    {
        ArgumentNullException.ThrowIfNull(app);
        //app.Properties[AuthenticationMiddlewareSetKey] = true;
        //return app.UseMiddleware<AuthenticationMiddleware>();
        return app;
    }

    public static IdentityServerConfiguration GetIdentityServerConfiguration(string secret)
    {
        var credentialsSigningCertificateLocation = Environment.GetEnvironmentVariable("OCELOT_CERTIFICATE");
        var credentialsSigningCertificatePassword = Environment.GetEnvironmentVariable("OCELOT_CERTIFICATE_PASSWORD");

        return new IdentityServerConfiguration(
            "admin",
            false,
            secret,
            new List<string> { "admin", "openid", "offline_access" },
            credentialsSigningCertificateLocation,
            credentialsSigningCertificatePassword
        );
    }

    public static IOcelotBuilder AddAdministration(this IOcelotBuilder builder, string path, Action<JwtBearerOptions> configureOptions)
    {
        //var administrationPath = new AdministrationPath(path);
        //builder.Services.AddSingleton(IdentityServerMiddlewareConfigurationProvider.Get);
        //if (configureOptions != null)
        //{
        //    AddIdentityServer(configureOptions, builder);
        //}

        //builder.Services.AddSingleton<IAdministrationPath>(administrationPath);
        return builder;
    }
    /*
    private static void AddIdentityServer(Action<JwtBearerOptions> configOptions, IOcelotBuilder builder)
    {
        builder.Services
            .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
            .AddJwtBearer("Bearer", configOptions);
    }

    private static void AddIdentityServer(IIdentityServerConfiguration identityServerConfiguration, IAdministrationPath adminPath, IOcelotBuilder builder, IConfiguration configuration)
    {
        builder.Services.TryAddSingleton(identityServerConfiguration);
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
            var cert = new X509Certificate2(identityServerConfiguration.CredentialsSigningCertificateLocation, identityServerConfiguration.CredentialsSigningCertificatePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            identityServerBuilder.AddSigningCredential(cert);
        }
    }*/
}

public class IdentityServerConfiguration
{
    public IdentityServerConfiguration(
        string apiName,
        bool requireHttps,
        string apiSecret,
        List<string> allowedScopes,
        string credentialsSigningCertificateLocation,
        string credentialsSigningCertificatePassword)
    {
        ApiName = apiName;
        RequireHttps = requireHttps;
        ApiSecret = apiSecret;
        AllowedScopes = allowedScopes;
        CredentialsSigningCertificateLocation = credentialsSigningCertificateLocation;
        CredentialsSigningCertificatePassword = credentialsSigningCertificatePassword;
    }

    public string ApiName { get; }
    public bool RequireHttps { get; }
    public List<string> AllowedScopes { get; }
    public string ApiSecret { get; }
    public string CredentialsSigningCertificateLocation { get; }
    public string CredentialsSigningCertificatePassword { get; }
}
