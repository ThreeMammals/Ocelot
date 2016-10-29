using Ocelot.Infrastructure.RequestData;
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

        public AuthorisationMiddleware(RequestDelegate next,
            IRequestScopedDataRepository requestScopedDataRepository,
            IAuthoriser authoriser)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _authoriser = authoriser;
        }

        public async Task Invoke(HttpContext context)
        {
            if (DownstreamRoute.ReRoute.IsAuthorised)
            {
                var authorised = _authoriser.Authorise(context.User, DownstreamRoute.ReRoute.RouteClaimsRequirement);

                if (authorised.IsError)
                {
                    SetPipelineError(authorised.Errors);
                    return;
                }

                if (IsAuthorised(authorised))
                {
                    await _next.Invoke(context);
                }
                else
                {
                    SetPipelineError(new List<Error>
                    {
                        new UnauthorisedError(
                            $"{context.User.Identity.Name} unable to access {DownstreamRoute.ReRoute.UpstreamTemplate}")
                    });
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private static bool IsAuthorised(Response<bool> authorised)
        {
            return authorised.Data;
        }
    }
}
