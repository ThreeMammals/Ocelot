using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Responses;

namespace Ocelot.Library.Authentication.Handler.Creator
{
    using AuthenticationOptions = Configuration.AuthenticationOptions;

    public interface IAuthenticationHandlerCreator
    {
        Response<RequestDelegate> CreateIdentityServerAuthenticationHandler(IApplicationBuilder app, AuthenticationOptions authOptions);
    }
}
