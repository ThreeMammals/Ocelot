namespace Ocelot.Library.Authentication
{
    using Microsoft.AspNetCore.Builder;
    using Responses;
    using AuthenticationOptions = Configuration.AuthenticationOptions;

    public interface IAuthenticationHandlerFactory
    {
        Response<AuthenticationHandler> Get(IApplicationBuilder app, AuthenticationOptions authOptions);
    }
}
