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
            _logger.LogDebug("started authorisation");

            if (DownstreamRoute.ReRoute.IsAuthorised)
            {
                _logger.LogDebug("route is authorised");

                var authorised = _authoriser.Authorise(context.User, DownstreamRoute.ReRoute.RouteClaimsRequirement);

                if (authorised.IsError)
                {
                    _logger.LogDebug("error authorising user");

                    SetPipelineError(authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    _logger.LogDebug("user is authorised calling next middleware");

                    await _next.Invoke(context);

                    _logger.LogDebug("succesfully called next middleware");
                }
                else
                {
                    _logger.LogDebug("user is not authorised setting pipeline error");

                    SetPipelineError(new List<Error>
                    {
                        new UnauthorisedError(
                            $"{context.User.Identity.Name} unable to access {DownstreamRoute.ReRoute.UpstreamPathTemplate.Value}")
                    });
                }
            }
            else
            {
                _logger.LogDebug("AuthorisationMiddleware.Invoke route is not authorised calling next middleware");

                await _next.Invoke(context);

                _logger.LogDebug("succesfully called next middleware");
            }
        }

        private static bool IsAuthorised(Response<bool> authorised)
        {
            return authorised.Data;
        }
    }
}
