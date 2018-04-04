using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Authentication.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IOcelotLogger _logger;

        public AuthenticationMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<AuthenticationMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (IsAuthenticatedRoute(context.DownstreamReRoute))
            {
                _logger.LogInformation("{context.HttpContext.Request.Path} is an authenticated route. {MiddlewareName} checking if client is authenticated", context.HttpContext.Request.Path, MiddlewareName);
                
                var result = await context.HttpContext.AuthenticateAsync(context.DownstreamReRoute.AuthenticationOptions.AuthenticationProviderKey);
                
                context.HttpContext.User = result.Principal;

                if (context.HttpContext.User.Identity.IsAuthenticated)
                {
                    _logger.LogInformation("Client has been authenticated for {context.HttpContext.Request.Path}", context.HttpContext.Request.Path);
                    await _next.Invoke(context);
                }
                else
                {
                    var error = new List<Error>
                    {
                        new UnauthenticatedError(
                            $"Request for authenticated route {context.HttpContext.Request.Path} by {context.HttpContext.User.Identity.Name} was unauthenticated")
                    };

                    _logger.LogWarning("Client has NOT been authenticated for {context.HttpContext.Request.Path} and pipeline error set. {error}", context.HttpContext.Request.Path, error.ToErrorString());
                    
                    SetPipelineError(context, error);
                }
            }
            else
            {
                _logger.LogInformation($"No authentication needed for {context.HttpContext.Request.Path}");

                await _next.Invoke(context);
            }
        }

        private static bool IsAuthenticatedRoute(DownstreamReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }
}
