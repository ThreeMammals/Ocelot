using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Infrastructure.Configuration;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Errors;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private RequestDelegate _authenticationNext;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IApplicationBuilder _app;

        public AuthenticationMiddleware(RequestDelegate next, IApplicationBuilder app,
            IScopedRequestDataRepository scopedRequestDataRepository) 
            : base(scopedRequestDataRepository)
        {
            _next = next;
            _scopedRequestDataRepository = scopedRequestDataRepository;
            _app = app;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _scopedRequestDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            if (IsAuthenticatedRoute(downstreamRoute.Data.ReRoute))
            {
                //todo - build auth pipeline and then call normal pipeline if all good?
                //create new app builder
                var builder = _app.New();
                //set up any options for the authentication
                var jwtBearerOptions = new JwtBearerOptions
                {
                    AutomaticAuthenticate = true,
                    AutomaticChallenge = true,
                    RequireHttpsMetadata = false,
                };
                //set the authentication middleware
                builder.UseJwtBearerAuthentication(jwtBearerOptions);
                //use mvc so we hit the catch all authorised controller
                builder.UseMvc();
                //then build it
                _authenticationNext = builder.Build();
                //then call it
                await _authenticationNext(context);
                //check if the user is authenticated
                if (context.User.Identity.IsAuthenticated)
                {
                    await _next.Invoke(context);
                }
                else
                {   
                    SetPipelineError(new List<Error> {new UnauthenticatedError($"Request for authenticated route {context.Request.Path} by {context.User.Identity.Name} was unauthenticated")});
                }      
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private static bool IsAuthenticatedRoute(ReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }
}
