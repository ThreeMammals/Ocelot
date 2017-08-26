using System;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.Authentication.Handler.Creator
{
    using Ocelot.Configuration;

    using AuthenticationOptions = Configuration.AuthenticationOptions;

    /// <summary>
    /// Cannot unit test things in this class due to use of extension methods
    /// </summary>
    public class AuthenticationHandlerCreator : IAuthenticationHandlerCreator
    {
        public Response<RequestDelegate> Create(IApplicationBuilder app, AuthenticationOptions authOptions)
        {
            var builder = app.New();

            if (authOptions.Provider.ToLower() == "jwt")
            {
                var authenticationConfig = authOptions.Config as JwtConfig;

                builder.UseJwtBearerAuthentication(
                    new JwtBearerOptions()
                        {
                            Authority = authenticationConfig.Authority,
                            Audience = authenticationConfig.Audience
                        });
            }
            else
            {
                var authenticationConfig = authOptions.Config as IdentityServerConfig;

                builder.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
                {
                    Authority = authenticationConfig.ProviderRootUrl,
                    ApiName = authenticationConfig.ApiName,
                    RequireHttpsMetadata = authenticationConfig.RequireHttps,
                    AllowedScopes = authOptions.AllowedScopes,
                    SupportedTokens = SupportedTokens.Both,
                    ApiSecret = authenticationConfig.ApiSecret
                });
            }

            var authenticationNext = builder.Build();

            return new OkResponse<RequestDelegate>(authenticationNext);
        }
    }
}