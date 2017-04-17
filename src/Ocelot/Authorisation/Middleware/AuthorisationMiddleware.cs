using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;

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
        private readonly IAuthoriser _authoriser;
        private readonly IOcelotLogger _logger;

        public AuthorisationMiddleware(RequestDelegate next,
            IRequestScopedDataRepository requestScopedDataRepository,
            IAuthoriser authoriser,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _authoriser = authoriser;
            _logger = loggerFactory.CreateLogger<AuthorisationMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (DownstreamRoute.ReRoute.IsAuthorised)
            {
                _logger.LogDebug($"{DownstreamRoute.ReRoute.DownstreamPathTemplate.Value} route requires user to be authorised");

                var authorised = _authoriser.Authorise(context.User, DownstreamRoute.ReRoute.RouteClaimsRequirement);

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
    }
}
