namespace Ocelot.Authorisation.Middleware
{
    using Configuration;
    using Logging;
    using Ocelot.Middleware;
    using Responses;
    using System.Threading.Tasks;
    using Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;

    public class AuthorisationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IClaimsAuthoriser _claimsAuthoriser;
        private readonly IScopesAuthoriser _scopesAuthoriser;

        public AuthorisationMiddleware(RequestDelegate next,
            IClaimsAuthoriser claimsAuthoriser,
            IScopesAuthoriser scopesAuthoriser,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository repo)
            : base(loggerFactory.CreateLogger<AuthorisationMiddleware>(), repo)
        {
            _next = next;
            _claimsAuthoriser = claimsAuthoriser;
            _scopesAuthoriser = scopesAuthoriser;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!IsOptionsHttpMethod(httpContext) && IsAuthenticatedRoute(DownstreamContext.Data.DownstreamReRoute))
            {
                Logger.LogInformation("route is authenticated scopes must be checked");

                var authorised = _scopesAuthoriser.Authorise(httpContext.User, DownstreamContext.Data.DownstreamReRoute.AuthenticationOptions.AllowedScopes);

                if (authorised.IsError)
                {
                    Logger.LogWarning("error authorising user scopes");

                    SetPipelineError(httpContext, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    Logger.LogInformation("user scopes is authorised calling next authorisation checks");
                }
                else
                {
                    Logger.LogWarning("user scopes is not authorised setting pipeline error");

                    SetPipelineError(httpContext, new UnauthorisedError(
                            $"{httpContext.User.Identity.Name} unable to access {DownstreamContext.Data.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}"));
                }
            }

            if (!IsOptionsHttpMethod(httpContext) && IsAuthorisedRoute(DownstreamContext.Data.DownstreamReRoute))
            {
                Logger.LogInformation("route is authorised");

                var authorised = _claimsAuthoriser.Authorise(httpContext.User, DownstreamContext.Data.DownstreamReRoute.RouteClaimsRequirement, DownstreamContext.Data.TemplatePlaceholderNameAndValues);

                if (authorised.IsError)
                {
                    Logger.LogWarning($"Error whilst authorising {httpContext.User.Identity.Name}. Setting pipeline error");

                    SetPipelineError(httpContext, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    Logger.LogInformation($"{httpContext.User.Identity.Name} has succesfully been authorised for {DownstreamContext.Data.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}.");
                    await _next.Invoke(httpContext);
                }
                else
                {
                    Logger.LogWarning($"{httpContext.User.Identity.Name} is not authorised to access {DownstreamContext.Data.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}. Setting pipeline error");

                    SetPipelineError(httpContext, new UnauthorisedError($"{httpContext.User.Identity.Name} is not authorised to access {DownstreamContext.Data.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}"));
                }
            }
            else
            {
                Logger.LogInformation($"{DownstreamContext.Data.DownstreamReRoute.DownstreamPathTemplate.Value} route does not require user to be authorised");
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
