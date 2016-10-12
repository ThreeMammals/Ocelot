namespace Ocelot.Library.Middleware
{
    using System.Threading.Tasks;
    using Infrastructure.Authentication;
    using Infrastructure.DownstreamRouteFinder;
    using Infrastructure.Repository;
    using Infrastructure.Responses;
    using Microsoft.AspNetCore.Http;

    public class AuthenticationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;
        private readonly IRouteRequiresAuthentication _requiresAuthentication;
        public AuthenticationMiddleware(RequestDelegate next, 
            IScopedRequestDataRepository scopedRequestDataRepository, 
            IRouteRequiresAuthentication requiresAuthentication) 
            : base(scopedRequestDataRepository)
        {
            _next = next;
            _scopedRequestDataRepository = scopedRequestDataRepository;
            _requiresAuthentication = requiresAuthentication;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRoute = _scopedRequestDataRepository.Get<DownstreamRoute>("DownstreamRoute");

            var isAuthenticated = _requiresAuthentication.IsAuthenticated(downstreamRoute.Data, context.Request.Method);

            if (isAuthenticated.IsError)
            {
                SetPipelineError(downstreamRoute.Errors);
                return;
            }

            if (IsAuthenticatedRoute(isAuthenticated))
            {
                //todo - build auth pipeline and then call normal pipeline if all good?
                await _next.Invoke(context);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private static bool IsAuthenticatedRoute(Response<bool> isAuthenticated)
        {
            return isAuthenticated.Data;
        }
    }
}
