namespace Ocelot.Library.Middleware
{
    using System.Threading.Tasks;
    using DownstreamRouteFinder;
    using Microsoft.AspNetCore.Http;
    using Repository;

    public class DownstreamRouteFinderMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDownstreamRouteFinder _downstreamRouteFinder;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        public DownstreamRouteFinderMiddleware(RequestDelegate next, 
            IDownstreamRouteFinder downstreamRouteFinder, 
            IScopedRequestDataRepository scopedRequestDataRepository)
            :base(scopedRequestDataRepository)
        {
            _next = next;
            _downstreamRouteFinder = downstreamRouteFinder;
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public async Task Invoke(HttpContext context)
        {   
            var upstreamUrlPath = context.Request.Path.ToString();

            var downstreamRoute = _downstreamRouteFinder.FindDownstreamRoute(upstreamUrlPath, context.Request.Method);

            if (downstreamRoute.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            _scopedRequestDataRepository.Add("DownstreamRoute", downstreamRoute.Data);

            await _next.Invoke(context);
        }
    }
}