using Microsoft.AspNetCore.Builder;
using Ocelot.Library.Responses;

namespace Ocelot.Library.Authentication.Handler.Factory
{
    using AuthenticationOptions = Configuration.AuthenticationOptions;

    public interface IAuthenticationHandlerFactory
    {
        Response<AuthenticationHandler> Get(IApplicationBuilder app, AuthenticationOptions authOptions);
    }
}
