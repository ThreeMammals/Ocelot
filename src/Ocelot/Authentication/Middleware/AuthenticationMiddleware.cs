using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Threading.Tasks;

namespace Ocelot.Authentication.Middleware
{
    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;

        public AuthenticationMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<AuthenticationMiddleware>())
        {
            _next = next;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context.HttpContext.Request.Method.ToUpper() != "OPTIONS" && IsAuthenticatedRoute(context.DownstreamReRoute))
            {
                Logger.LogInformation($"{context.HttpContext.Request.Path} is an authenticated route. {MiddlewareName} checking if client is authenticated");

                var result = await context.HttpContext.AuthenticateAsync(context.DownstreamReRoute.AuthenticationOptions.AuthenticationProviderKey);

                context.HttpContext.User = result.Principal;

                if (context.HttpContext.User.Identity.IsAuthenticated)
                {
                    Logger.LogInformation($"Client has been authenticated for {context.HttpContext.Request.Path}");
                    await _next.Invoke(context);
                }
                else
                {
                    var error = new UnauthenticatedError(
                        $"Request for authenticated route {context.HttpContext.Request.Path} by {context.HttpContext.User.Identity.Name} was unauthenticated");

                    Logger.LogWarning($"Client has NOT been authenticated for {context.HttpContext.Request.Path} and pipeline error set. {error}");

                    SetPipelineError(context, error);
                }
            }
            else
            {
                Logger.LogInformation($"No authentication needed for {context.HttpContext.Request.Path}");

                await _next.Invoke(context);
            }
        }

        private static bool IsAuthenticatedRoute(DownstreamReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }
}
