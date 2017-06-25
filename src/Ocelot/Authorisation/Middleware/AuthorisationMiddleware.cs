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
    using Ocelot.Middleware;

    public class AuthorisationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IClaimsAuthoriser _claimsAuthoriser;
        private readonly IScopesAuthoriser _scopesAuthoriser;
        private readonly IOcelotLogger _logger;

        public AuthorisationMiddleware(RequestDelegate next,
            IRequestScopedDataRepository requestScopedDataRepository,
            IClaimsAuthoriser claimsAuthoriser,
            IScopesAuthoriser scopesAuthoriser,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _claimsAuthoriser = claimsAuthoriser;
            _scopesAuthoriser = scopesAuthoriser;
            _logger = loggerFactory.CreateLogger<AuthorisationMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsAuthenticatedRoute(DownstreamRoute.ReRoute))
            {
                _logger.LogDebug("route is authenticated scopes must be checked");

                var authorised = _scopesAuthoriser.Authorise(context.User, DownstreamRoute.ReRoute.AuthenticationOptions.AllowedScopes);

                if (authorised.IsError)
                {
                    _logger.LogDebug("error authorising user scopes");

                    SetPipelineError(authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    _logger.LogDebug("user scopes is authorised calling next authorisation checks");
                }
                else
                {
                    _logger.LogDebug("user scopes is not authorised setting pipeline error");

                    SetPipelineError(new List<Error>
                    {
                        new UnauthorisedError(
                            $"{context.User.Identity.Name} unable to access {DownstreamRoute.ReRoute.UpstreamPathTemplate.Value}")
                    });
                }
            }

            if (IsAuthorisedRoute(DownstreamRoute.ReRoute))
            {
                _logger.LogDebug("route is authorised");

                var authorised = _claimsAuthoriser.Authorise(context.User, DownstreamRoute.ReRoute.RouteClaimsRequirement);

                if (authorised.IsError)
                {
                    _logger.LogDebug($"Error whilst authorising {context.User.Identity.Name} for {context.User.Identity.Name}. Setting pipeline error");

                    SetPipelineError(authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    _logger.LogDebug($"{context.User.Identity.Name} has succesfully been authorised for {DownstreamRoute.ReRoute.UpstreamPathTemplate.Value}. Calling next middleware");
                    await _next.Invoke(context);
                }
                else
                {
                    _logger.LogDebug($"{context.User.Identity.Name} is not authorised to access {DownstreamRoute.ReRoute.UpstreamPathTemplate.Value}. Setting pipeline error");

                    SetPipelineError(new List<Error>
                    {
                        new UnauthorisedError($"{context.User.Identity.Name} is not authorised to access {DownstreamRoute.ReRoute.UpstreamPathTemplate.Value}")
                    });
                }
            }
            else
            {
                _logger.LogDebug($"{DownstreamRoute.ReRoute.DownstreamPathTemplate.Value} route does not require user to be authorised");
                await _next.Invoke(context);
            }
        }

        private static bool IsAuthorised(Response<bool> authorised)
        {
            return authorised.Data;
        }

        private static bool IsAuthenticatedRoute(ReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }

        private static bool IsAuthorisedRoute(ReRoute reRoute)
        {
            return reRoute.IsAuthorised;
        }
    }
}
