using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.Configuration.Authentication;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Provider;
using Ocelot.Middleware;

namespace Ocelot.DependencyInjection
{
    public static class OcelotBuilderExtensionsIdentityServer
    {
        public static void AddIdentityServer(this IOcelotBuilder builder)
        {
            var identityServerConfiguration = IdentityServerConfigurationCreator.GetIdentityServerConfiguration();

            if (identityServerConfiguration != null)
            {
                var services = builder.Services;

                services.TryAddSingleton<IIdentityServerConfiguration>(identityServerConfiguration);
                services.TryAddSingleton<IHashMatcher, HashMatcher>();
                var identityServerBuilder = services
                    .AddIdentityServer(o =>
                    {
                        o.IssuerUri = "Ocelot";
                    })
                    .AddInMemoryApiResources(Resources(identityServerConfiguration))
                    .AddInMemoryClients(Client(identityServerConfiguration))
                    .AddResourceOwnerValidator<OcelotResourceOwnerPasswordValidator>();

                //todo - refactor a method so we know why this is happening
                var whb = services.First(x => x.ServiceType == typeof(IWebHostBuilder));
                var urlFinder = new BaseUrlFinder((IWebHostBuilder)whb.ImplementationInstance);
                var baseSchemeUrlAndPort = urlFinder.Find();
                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                    .AddIdentityServerAuthentication(o =>
                    {
                        var adminPath = builder.Configuration.GetValue("GlobalConfiguration:AdministrationPath", string.Empty);
                        o.Authority = baseSchemeUrlAndPort + adminPath;
                        o.ApiName = identityServerConfiguration.ApiName;
                        o.RequireHttpsMetadata = identityServerConfiguration.RequireHttps;
                        o.SupportedTokens = SupportedTokens.Both;
                        o.ApiSecret = identityServerConfiguration.ApiSecret;
                    });

                //todo - refactor naming..
                if (string.IsNullOrEmpty(identityServerConfiguration.CredentialsSigningCertificateLocation) || string.IsNullOrEmpty(identityServerConfiguration.CredentialsSigningCertificatePassword))
                {
                    identityServerBuilder.AddDeveloperSigningCredential();
                }
                else
                {
                    //todo - refactor so calls method?
                    var cert = new X509Certificate2(identityServerConfiguration.CredentialsSigningCertificateLocation, identityServerConfiguration.CredentialsSigningCertificatePassword);
                    identityServerBuilder.AddSigningCredential(cert);
                }
            }
        }

        private static List<ApiResource> Resources(IIdentityServerConfiguration identityServerConfiguration)
        {
            return new List<ApiResource>
            {
                new ApiResource(identityServerConfiguration.ApiName, identityServerConfiguration.ApiName)
                {
                    ApiSecrets = new List<Secret>
                    {
                        new Secret
                        {
                            Value = identityServerConfiguration.ApiSecret.Sha256()
                        }
                    }
                }
            };
        }

        private static List<Client> Client(IIdentityServerConfiguration identityServerConfiguration)
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId = identityServerConfiguration.ApiName,
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    ClientSecrets = new List<Secret> {new Secret(identityServerConfiguration.ApiSecret.Sha256())},
                    AllowedScopes = { identityServerConfiguration.ApiName }
                }
            };
        }
    }
}
