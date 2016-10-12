namespace Ocelot.Library.Infrastructure.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using DownstreamRouteFinder;
    using Responses;

    public class RouteRequiresAuthentication : IRouteRequiresAuthentication
    {
        private readonly IOcelotConfiguration _configuration;

        public RouteRequiresAuthentication(IOcelotConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Response<bool> IsAuthenticated(DownstreamRoute downstreamRoute, string httpMethod)
        {
            var reRoute =
                _configuration.ReRoutes.FirstOrDefault(
                    x =>
                        x.DownstreamTemplate == downstreamRoute.DownstreamUrlTemplate &&
                        string.Equals(x.UpstreamHttpMethod, httpMethod, StringComparison.CurrentCultureIgnoreCase));

            if (reRoute == null)
            {
                return new ErrorResponse<bool>(new List<Error> {new CouldNotFindConfigurationError($"Could not find configuration for {downstreamRoute.DownstreamUrlTemplate} using method {httpMethod}")});
            }

            return new OkResponse<bool>(reRoute.IsAuthenticated);
        }
    }
}
