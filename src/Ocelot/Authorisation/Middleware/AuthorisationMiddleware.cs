namespace Ocelot.Authorisation.Middleware
{
    using Configuration;
    using Logging;
    using Ocelot.Middleware;
    using Responses;
    using System.Threading.Tasks;

    public class AuthorisationMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IClaimsAuthoriser _claimsAuthoriser;
        private readonly IScopesAuthoriser _scopesAuthoriser;

        public AuthorisationMiddleware(OcelotRequestDelegate next,
            IClaimsAuthoriser claimsAuthoriser,
            IScopesAuthoriser scopesAuthoriser,
            IOcelotLoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<AuthorisationMiddleware>())
        {
            _next = next;
            _claimsAuthoriser = claimsAuthoriser;
            _scopesAuthoriser = scopesAuthoriser;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (!IsOptionsHttpMethod(context) && IsAuthenticatedRoute(context.DownstreamReRoute))
            {
                Logger.LogInformation("route is authenticated scopes must be checked");

                var authorised = _scopesAuthoriser.Authorise(context.HttpContext.User, context.DownstreamReRoute.AuthenticationOptions.AllowedScopes);

                if (authorised.IsError)
                {
                    Logger.LogWarning("error authorising user scopes");

                    SetPipelineError(context, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    Logger.LogInformation("user scopes is authorised calling next authorisation checks");
                }
                else
                {
                    Logger.LogWarning("user scopes is not authorised setting pipeline error");

                    SetPipelineError(context, new UnauthorisedError(
                            $"{context.HttpContext.User.Identity.Name} unable to access {context.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}"));
                }
            }

            if (!IsOptionsHttpMethod(context) && IsAuthorisedRoute(context.DownstreamReRoute))
            {
                Logger.LogInformation("route is authorised");

                var authorised = _claimsAuthoriser.Authorise(context.HttpContext.User, context.DownstreamReRoute.RouteClaimsRequirement, context.TemplatePlaceholderNameAndValues);

                if (authorised.IsError)
                {
                    Logger.LogWarning($"Error whilst authorising {context.HttpContext.User.Identity.Name}. Setting pipeline error");

                    SetPipelineError(context, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    Logger.LogInformation($"{context.HttpContext.User.Identity.Name} has succesfully been authorised for {context.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}.");
                    await _next.Invoke(context);
                }
                else
                {
                    Logger.LogWarning($"{context.HttpContext.User.Identity.Name} is not authorised to access {context.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}. Setting pipeline error");

                    SetPipelineError(context, new UnauthorisedError($"{context.HttpContext.User.Identity.Name} is not authorised to access {context.DownstreamReRoute.UpstreamPathTemplate.OriginalValue}"));
                }
            }
            else
            {
                Logger.LogInformation($"{context.DownstreamReRoute.DownstreamPathTemplate.Value} route does not require user to be authorised");
                await _next.Invoke(context);
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

        private static bool IsOptionsHttpMethod(DownstreamContext context)
        {
            return context.HttpContext.Request.Method.ToUpper() == "OPTIONS";
        }
    }
}
