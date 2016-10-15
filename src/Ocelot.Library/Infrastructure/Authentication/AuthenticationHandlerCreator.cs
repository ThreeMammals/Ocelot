using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Authentication
{
    /// <summary>
    /// Cannot unit test things in this class due to use of extension methods
    /// </summary>
    public class AuthenticationHandlerCreator : IAuthenticationHandlerCreator
    {
        public Response<RequestDelegate> CreateIdentityServerAuthenticationHandler(IApplicationBuilder app)
        {
            var builder = app.New();

            builder.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                //todo sort these options out
                Authority = "http://localhost:51888",
                ScopeName = "api",

                RequireHttpsMetadata = false
            });

            builder.UseMvc();

            var authenticationNext = builder.Build();

            return new OkResponse<RequestDelegate>(authenticationNext);
        }
    }
}