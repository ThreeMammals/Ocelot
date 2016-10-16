using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Responses;
using AuthenticationOptions = Ocelot.Library.Infrastructure.Configuration.AuthenticationOptions;

namespace Ocelot.Library.Infrastructure.Authentication
{
    public interface IAuthenticationHandlerCreator
    {
        Response<RequestDelegate> CreateIdentityServerAuthenticationHandler(IApplicationBuilder app, AuthenticationOptions authOptions);
    }
}
