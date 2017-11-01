using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Authentication.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _app;
        private readonly IAuthenticationSchemeProvider _authSchemeProvider;
        private readonly IOcelotLogger _logger;

        public AuthenticationMiddleware(RequestDelegate next,
            IApplicationBuilder app,
            IRequestScopedDataRepository requestScopedDataRepository,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _app = app;
            _logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsAuthenticatedRoute(DownstreamRoute.ReRoute))
            {
                _logger.LogDebug($"{context.Request.Path} is an authenticated route. {MiddlewareName} checking if client is authenticated");
                
                var result = await context.AuthenticateAsync(DownstreamRoute.ReRoute.AuthenticationOptions.AuthenticationProviderKey);
                
                context.User = result.Principal;

                if (context.User.Identity.IsAuthenticated)
                {
                    _logger.LogDebug($"Client has been authenticated for {context.Request.Path}");
                    await _next.Invoke(context);
                }
                else
                {
                    var error = new List<Error>
                    {
                        new UnauthenticatedError(
                            $"Request for authenticated route {context.Request.Path} by {context.User.Identity.Name} was unauthenticated")
                    };

                    _logger.LogError($"Client has NOT been authenticated for {context.Request.Path} and pipeline error set. {error.ToErrorString()}");
                    SetPipelineError(error);
                }
            }
            else
            {
                _logger.LogTrace($"No authentication needed for {context.Request.Path}");

                await _next.Invoke(context);
            }
        }

        private static bool IsAuthenticatedRoute(ReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }
}

