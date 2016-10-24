using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Errors;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Middleware;

namespace Ocelot.Authentication.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;
        private readonly IApplicationBuilder _app;
        private readonly IAuthenticationHandlerFactory _authHandlerFactory;

        public AuthenticationMiddleware(RequestDelegate next, 
            IApplicationBuilder app,
            IRequestScopedDataRepository requestScopedDataRepository, 
            IAuthenticationHandlerFactory authHandlerFactory) 
            : base(requestScopedDataRepository)
        {
            _next = next;
            _requestScopedDataRepository = requestScopedDataRepository;
            _authHandlerFactory = authHandlerFactory;
            _app = app;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _requestScopedDataRepository.Get<DownstreamRoute>("DownstreamRoute");

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
