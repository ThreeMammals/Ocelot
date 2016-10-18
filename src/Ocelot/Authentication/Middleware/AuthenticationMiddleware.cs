using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Authentication.Handler.Factory;
using Ocelot.Library.Configuration;
using Ocelot.Library.DownstreamRouteFinder;
using Ocelot.Library.Errors;
using Ocelot.Library.Middleware;
using Ocelot.Library.ScopedData;

namespace Ocelot.Library.Authentication.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IApplicationBuilder _app;
        private readonly IAuthenticationHandlerFactory _authHandlerFactory;

        public AuthenticationMiddleware(RequestDelegate next, IApplicationBuilder app,
            IScopedRequestDataRepository scopedRequestDataRepository, IAuthenticationHandlerFactory authHandlerFactory) 
            : base(scopedRequestDataRepository)
        {
            _next = next;
            _scopedRequestDataRepository = scopedRequestDataRepository;
            _authHandlerFactory = authHandlerFactory;
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
                var authenticationNext = _authHandlerFactory.Get(_app, downstreamRoute.Data.ReRoute.AuthenticationOptions);

                if (!authenticationNext.IsError)
                {
                    await authenticationNext.Data.Handler.Invoke(context);
                }
                else
                {
                    SetPipelineError(authenticationNext.Errors);
                }

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
