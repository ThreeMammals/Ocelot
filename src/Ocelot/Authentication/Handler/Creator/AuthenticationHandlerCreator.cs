using System;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;

namespace Ocelot.Authentication.Handler.Creator
{
    using AuthenticationOptions = Configuration.AuthenticationOptions;

    /// <summary>
    /// Cannot unit test things in this class due to use of extension methods
    /// </summary>
    public class AuthenticationHandlerCreator : IAuthenticationHandlerCreator
    {
        public Response<RequestDelegate> Create(IApplicationBuilder app, AuthenticationOptions authOptions)
        {
            var builder = app.New();

            builder.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = authOptions.ProviderRootUrl,
                ApiName = authOptions.ApiName,
                RequireHttpsMetadata = authOptions.RequireHttps,
                AllowedScopes = authOptions.AllowedScopes,
                SupportedTokens = SupportedTokens.Both,
                ApiSecret = authOptions.ApiSecret
            });

            var authenticationNext = builder.Build();

            return new OkResponse<RequestDelegate>(authenticationNext);
        }
    }
}