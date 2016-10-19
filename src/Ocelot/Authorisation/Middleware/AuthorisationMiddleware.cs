namespace Ocelot.Authorisation.Middleware
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DownstreamRouteFinder;
    using Errors;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Middleware;
    using ScopedData;

    public class AuthorisationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IAuthoriser _authoriser;

        public AuthorisationMiddleware(RequestDelegate next,
            IScopedRequestDataRepository scopedRequestDataRepository,
            IAuthoriser authoriser)
            : base(scopedRequestDataRepository)
        {
            _next = next;
            _scopedRequestDataRepository = scopedRequestDataRepository;
            _authoriser = authoriser;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _scopedRequestDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            if (downstreamRoute.Data.ReRoute.IsAuthorised)
            {
                var authorised = _authoriser.Authorise(context.User, downstreamRoute.Data.ReRoute.RouteClaimsRequirement);

                if (authorised.IsError)
                {
                    SetPipelineError(authorised.Errors);
                    return;
                }

                if (authorised.Data)
                {
                    await _next.Invoke(context);
                }
                else
                {
                    SetPipelineError(new List<Error>
                    {
                        new UnauthorisedError(
                            $"{context.User.Identity.Name} unable to access {downstreamRoute.Data.ReRoute.UpstreamTemplate}")
                    });
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}
