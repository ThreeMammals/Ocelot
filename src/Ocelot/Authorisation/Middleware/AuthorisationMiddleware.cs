using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.Configuration;

namespace Ocelot.Authorisation.Middleware
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Errors;
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.Middleware;

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
                _logger.LogDebug("route is authenticated scopes must be checked");

                var authorised = _scopesAuthoriser.Authorise(context.HttpContext.User, context.DownstreamReRoute.AuthenticationOptions.AllowedScopes);

                if (authorised.IsError)
                {
                    _logger.LogDebug("error authorising user scopes");

                    SetPipelineError(context, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    _logger.LogDebug("user scopes is authorised calling next authorisation checks");
                }
                else
                {
                    _logger.LogDebug("user scopes is not authorised setting pipeline error");

                    SetPipelineError(context, new List<Error>
                    {
                        new UnauthorisedError(
                            $"{context.HttpContext.User.Identity.Name} unable to access {context.DownstreamReRoute.UpstreamPathTemplate.Value}")
                    });
                }
            }

            if (IsAuthorisedRoute(context.DownstreamReRoute))
            {
                _logger.LogDebug("route is authorised");

                var authorised = _claimsAuthoriser.Authorise(context.HttpContext.User, context.DownstreamReRoute.RouteClaimsRequirement);

                if (authorised.IsError)
                {
                    _logger.LogDebug($"Error whilst authorising {context.HttpContext.User.Identity.Name} for {context.HttpContext.User.Identity.Name}. Setting pipeline error");

                    SetPipelineError(context, authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    _logger.LogDebug($"{context.HttpContext.User.Identity.Name} has succesfully been authorised for {context.DownstreamReRoute.UpstreamPathTemplate.Value}. Calling next middleware");
                    await _next.Invoke(context);
                }
                else
                {
                    _logger.LogDebug($"{context.HttpContext.User.Identity.Name} is not authorised to access {context.DownstreamReRoute.UpstreamPathTemplate.Value}. Setting pipeline error");

                    SetPipelineError(context, new List<Error>
                    {
                        new UnauthorisedError($"{context.HttpContext.User.Identity.Name} is not authorised to access {context.DownstreamReRoute.UpstreamPathTemplate.Value}")
                    });
                }
            }
            else
            {
                _logger.LogDebug($"{context.DownstreamReRoute.DownstreamPathTemplate.Value} route does not require user to be authorised");
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
