using Microsoft.AspNetCore.Builder;
using Ocelot.Responses;

namespace Ocelot.Authentication.Handler.Factory
{
    using AuthenticationOptions = Configuration.AuthenticationOptions;

    public interface IAuthenticationHandlerFactory
    {
        Response<AuthenticationHandler> Get(IApplicationBuilder app, AuthenticationOptions authOptions);
    }
}
