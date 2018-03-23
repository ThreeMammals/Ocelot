namespace Ocelot.Authorisation.Middleware
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Errors;
    using Ocelot.Middleware;
    using Logging;
    using Responses;
    using Configuration;

    public class AuthorisationMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IClaimsAuthoriser _claimsAuthoriser;
        private readonly IScopesAuthoriser _scopesAuthoriser;
        private readonly IOcelotLogger _logger;

        public AuthorisationMiddleware(OcelotRequestDelegate next,
            IClaimsAuthoriser claimsAuthoriser,
            IScopesAuthoriser scopesAuthoriser,
            IOcelotLoggerFactory loggerFactory)
        {
            _next = next;
            _claimsAuthoriser = claimsAuthoriser;
            _scopesAuthoriser = scopesAuthoriser;
            _logger = loggerFactory.CreateLogger<AuthorisationMiddleware>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (IsAuthenticatedRoute(context.DownstreamReRoute))
            {
                _logger.LogInformation("route is authenticated scopes must be checked");

                var authorised = _scopesAuthoriser.Authorise(context.HttpContext.User, context.DownstreamReRoute.AuthenticationOptions.AllowedScopes);

                if (authorised.IsError)
                {
                    _logger.LogWarning("error authorising user scopes");

                    SetPipelineError(context, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    _logger.LogInformation("user scopes is authorised calling next authorisation checks");
                }
                else
                {
                    _logger.LogWarning("user scopes is not authorised setting pipeline error");

                    SetPipelineError(context, new List<Error>
                    {
                        new UnauthorisedError(
                            $"{context.HttpContext.User.Identity.Name} unable to access {context.DownstreamReRoute.UpstreamPathTemplate.Value}")
                    });
                }
            }

            if (IsAuthorisedRoute(context.DownstreamReRoute))
            {
                _logger.LogInformation("route is authorised");

                var authorised = _claimsAuthoriser.Authorise(context.HttpContext.User, context.DownstreamReRoute.RouteClaimsRequirement);

                if (authorised.IsError)
                {
                    _logger.LogWarning("Error whilst authorising {Name}. Setting pipeline error", context.HttpContext.User.Identity.Name);

                    SetPipelineError(context, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    _logger.LogInformation("{Name} has succesfully been authorised for {Value}. Calling next middleware", context.HttpContext.User.Identity.Name, context.DownstreamReRoute.UpstreamPathTemplate.Value);
                    await _next.Invoke(context);
                }
                else
                {
                    _logger.LogWarning("{Name} is not authorised to access {Value}. Setting pipeline error", context.HttpContext.User.Identity.Name, context.DownstreamReRoute.UpstreamPathTemplate.Value);

                    SetPipelineError(context, new List<Error>
                    {
                        new UnauthorisedError($"{context.HttpContext.User.Identity.Name} is not authorised to access {context.DownstreamReRoute.UpstreamPathTemplate.Value}")
                    });
                }
            }
            else
            {
                _logger.LogInformation("{Value} route does not require user to be authorised", context.DownstreamReRoute.DownstreamPathTemplate.Value);
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
    }
}
