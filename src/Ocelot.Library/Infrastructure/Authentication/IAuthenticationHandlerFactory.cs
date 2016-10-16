using Microsoft.AspNetCore.Builder;
using Ocelot.Library.Infrastructure.Responses;
using AuthenticationOptions = Ocelot.Library.Infrastructure.Configuration.AuthenticationOptions;

namespace Ocelot.Library.Infrastructure.Authentication
{
    public interface IAuthenticationHandlerFactory
    {
        Response<AuthenticationHandler> Get(IApplicationBuilder app, AuthenticationOptions authOptions);
    }
}
