using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Authentication.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _app;
        private readonly IAuthenticationHandlerFactory _authHandlerFactory;
        private readonly IOcelotLogger _logger;

        public AuthenticationMiddleware(RequestDelegate next,
            IApplicationBuilder app,
            IRequestScopedDataRepository requestScopedDataRepository,
            IAuthenticationHandlerFactory authHandlerFactory,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _authHandlerFactory = authHandlerFactory;
            _app = app;
            _logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started authentication");

            if (IsAuthenticatedRoute(DownstreamRoute.ReRoute))
            {
                var authenticationHandler = _authHandlerFactory.Get(_app, DownstreamRoute.ReRoute.AuthenticationOptions);

                if (!authenticationHandler.IsError)
                {
                    _logger.LogDebug("calling authentication handler for ReRoute");

                    await authenticationHandler.Data.Handler.Handle(context);
                }
                else
                {
                    _logger.LogDebug("there was an error getting authentication handler for ReRoute");

                    SetPipelineError(authenticationHandler.Errors);
                }

                if (context.User.Identity.IsAuthenticated)
                {
                    _logger.LogDebug("the user was authenticated");

                    await _next.Invoke(context);

                    _logger.LogDebug("succesfully called next middleware");
                }
                else
                {
                    _logger.LogDebug("the user was not authenticated");

                    SetPipelineError(new List<Error> { new UnauthenticatedError($"Request for authenticated route {context.Request.Path} by {context.User.Identity.Name} was unauthenticated") });
                }
            }
            else
            {
                _logger.LogDebug("calling next middleware");

                await _next.Invoke(context);

                _logger.LogDebug("succesfully called next middleware");
            }
        }

        private static bool IsAuthenticatedRoute(ReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }
}
