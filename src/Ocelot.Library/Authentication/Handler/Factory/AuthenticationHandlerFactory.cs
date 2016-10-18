using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Ocelot.Library.Authentication.Handler.Creator;
using Ocelot.Library.Errors;
using Ocelot.Library.Responses;

namespace Ocelot.Library.Authentication.Handler.Factory
{
    using AuthenticationOptions = Configuration.AuthenticationOptions;

    public class AuthenticationHandlerFactory : IAuthenticationHandlerFactory
    {
        private readonly IAuthenticationHandlerCreator _creator;

        public AuthenticationHandlerFactory(IAuthenticationHandlerCreator creator)
        {
            _creator = creator;
        }

        public Response<AuthenticationHandler> Get(IApplicationBuilder app, AuthenticationOptions authOptions)
        {
            var handler = _creator.CreateIdentityServerAuthenticationHandler(app, authOptions);

            if (!handler.IsError)
            {
                return new OkResponse<AuthenticationHandler>(new AuthenticationHandler(authOptions.Provider, handler.Data));
            }

            return new ErrorResponse<AuthenticationHandler>(new List<Error>
            {
                new UnableToCreateAuthenticationHandlerError($"Unable to create authentication handler for {authOptions.Provider}")
            });
        }
    }
}