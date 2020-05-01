namespace Ocelot.Authorisation.Middleware
{
    using Ocelot.Configuration;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Responses;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;

    public class AuthorisationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IClaimsAuthoriser _claimsAuthoriser;
        private readonly IScopesAuthoriser _scopesAuthoriser;

        public AuthorisationMiddleware(RequestDelegate next,
            IClaimsAuthoriser claimsAuthoriser,
            IScopesAuthoriser scopesAuthoriser,
            IOcelotLoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<AuthorisationMiddleware>())
        {
            _next = next;
            _claimsAuthoriser = claimsAuthoriser;
            _scopesAuthoriser = scopesAuthoriser;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var downstreamReRoute = httpContext.Items.DownstreamReRoute();

            if (!IsOptionsHttpMethod(httpContext) && IsAuthenticatedRoute(downstreamReRoute))
            {
                Logger.LogInformation("route is authenticated scopes must be checked");

                var authorised = _scopesAuthoriser.Authorise(httpContext.User, downstreamReRoute.AuthenticationOptions.AllowedScopes);

                if (authorised.IsError)
                {
                    Logger.LogWarning("error authorising user scopes");

                    httpContext.Items.UpsertErrors(authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    Logger.LogInformation("user scopes is authorised calling next authorisation checks");
                }
                else
                {
                    Logger.LogWarning("user scopes is not authorised setting pipeline error");

                    httpContext.Items.SetError(new UnauthorisedError(
                            $"{httpContext.User.Identity.Name} unable to access {downstreamReRoute.UpstreamPathTemplate.OriginalValue}"));
                }
            }

            if (!IsOptionsHttpMethod(httpContext) && IsAuthorisedRoute(downstreamReRoute))
            {
                Logger.LogInformation("route is authorised");

                var authorised = _claimsAuthoriser.Authorise(httpContext.User, downstreamReRoute.RouteClaimsRequirement, httpContext.Items.TemplatePlaceholderNameAndValues());

                if (authorised.IsError)
                {
                    Logger.LogWarning($"Error whilst authorising {httpContext.User.Identity.Name}. Setting pipeline error");

                    httpContext.Items.UpsertErrors(authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    Logger.LogInformation($"{httpContext.User.Identity.Name} has succesfully been authorised for {downstreamReRoute.UpstreamPathTemplate.OriginalValue}.");
                    await _next.Invoke(httpContext);
                }
                else
                {
                    Logger.LogWarning($"{httpContext.User.Identity.Name} is not authorised to access {downstreamReRoute.UpstreamPathTemplate.OriginalValue}. Setting pipeline error");

                    httpContext.Items.SetError(new UnauthorisedError($"{httpContext.User.Identity.Name} is not authorised to access {downstreamReRoute.UpstreamPathTemplate.OriginalValue}"));
                }
            }
            else
            {
                Logger.LogInformation($"{downstreamReRoute.DownstreamPathTemplate.Value} route does not require user to be authorised");
                await _next.Invoke(httpContext);
            }
        }

        private static bool IsAuthorised(Response<bool> authorised)
        {
            return authorised.Data;
        }

        private static bool IsAuthenticatedRoute(DownstreamReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }

        private static bool IsAuthorisedRoute(DownstreamReRoute reRoute)
        {
            return reRoute.IsAuthorised;
        }

        private static bool IsOptionsHttpMethod(HttpContext httpContext)
        {
            return httpContext.Request.Method.ToUpper() == "OPTIONS";
        }
    }
}
