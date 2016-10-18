using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Errors;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.ScopedData;

namespace Ocelot.Authorisation
{
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

            //todo - call authoriser
            var authorised = new OkResponse<bool>(true); //_authoriser.Authorise(context.User, new RouteClaimsRequirement(new Dictionary<string, string>()));

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
                    new UnauthorisedError($"{context.User.Identity.Name} unable to access {downstreamRoute.Data.ReRoute.UpstreamTemplate}")
                });
            }
        }
    }
}
