namespace Ocelot.Library.Authentication
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Responses;
    using AuthenticationOptions = Configuration.AuthenticationOptions;

    public interface IAuthenticationHandlerCreator
    {
        Response<RequestDelegate> CreateIdentityServerAuthenticationHandler(IApplicationBuilder app, AuthenticationOptions authOptions);
    }
}
