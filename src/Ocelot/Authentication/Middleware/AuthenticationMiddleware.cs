namespace Ocelot.Authentication.Middleware
{
    using Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Authentication;
    using Ocelot.Configuration;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Threading.Tasks;

    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository repo)
            : base(loggerFactory.CreateLogger<AuthenticationMiddleware>(), repo)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Method.ToUpper() != "OPTIONS" && IsAuthenticatedRoute(DownstreamContext.Data.DownstreamReRoute))
            {
                Logger.LogInformation($"{httpContext.Request.Path} is an authenticated route. {MiddlewareName} checking if client is authenticated");

                var result = await httpContext.AuthenticateAsync(DownstreamContext.Data.DownstreamReRoute.AuthenticationOptions.AuthenticationProviderKey);

                httpContext.User = result.Principal;

                if (httpContext.User.Identity.IsAuthenticated)
                {
                    Logger.LogInformation($"Client has been authenticated for {httpContext.Request.Path}");
                    await _next.Invoke(httpContext);
                }
                else
                {
                    var error = new UnauthenticatedError(
                        $"Request for authenticated route {httpContext.Request.Path} by {httpContext.User.Identity.Name} was unauthenticated");

                    Logger.LogWarning($"Client has NOT been authenticated for {httpContext.Request.Path} and pipeline error set. {error}");

                    SetPipelineError(httpContext, error);
                }
            }
            else
            {
                Logger.LogInformation($"No authentication needed for {httpContext.Request.Path}");

                await _next.Invoke(httpContext);
            }
        }

        private static bool IsAuthenticatedRoute(DownstreamReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }
}
