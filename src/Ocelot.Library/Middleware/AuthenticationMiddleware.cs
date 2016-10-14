namespace Ocelot.Library.Middleware
{
    using System.Threading.Tasks;
    using Infrastructure.Configuration;
    using Infrastructure.DownstreamRouteFinder;
    using Infrastructure.Repository;
    using Microsoft.AspNetCore.Http;

    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        public AuthenticationMiddleware(RequestDelegate next, 
            IScopedRequestDataRepository scopedRequestDataRepository) 
            : base(scopedRequestDataRepository)
        {
            _next = next;
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _scopedRequestDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            if (IsAuthenticatedRoute(downstreamRoute.Data.ReRoute))
            {
                //todo - build auth pipeline and then call normal pipeline if all good?
                await _next.Invoke(context);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private static bool IsAuthenticatedRoute(ReRoute reRoute)
        {
            return reRoute.IsAuthenticated;
        }
    }
}
