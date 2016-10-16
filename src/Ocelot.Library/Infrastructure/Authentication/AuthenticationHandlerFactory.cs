using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Authentication
{
    public class AuthenticationHandlerFactory : IAuthenticationHandlerFactory
    {
        private readonly IAuthenticationHandlerCreator _creator;

        public AuthenticationHandlerFactory(IAuthenticationHandlerCreator creator)
        {
            _creator = creator;
        }

        public Response<AuthenticationHandler> Get(string provider, IApplicationBuilder app)
        {
            var handler = _creator.CreateIdentityServerAuthenticationHandler(app);

            if (!handler.IsError)
            {
                return new OkResponse<AuthenticationHandler>(new AuthenticationHandler(provider, handler.Data));
            }

            return new ErrorResponse<AuthenticationHandler>(new List<Error>
            {
                new UnableToCreateAuthenticationHandlerError($"Unable to create authentication handler for {provider}")
            });
        }
    }
}