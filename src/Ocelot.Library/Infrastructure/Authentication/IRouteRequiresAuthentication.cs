namespace Ocelot.Library.Infrastructure.Authentication
{
    using DownstreamRouteFinder;
    using Responses;

    public interface IRouteRequiresAuthentication
    {
        Response<bool> IsAuthenticated(DownstreamRoute downstreamRoute, string httpMethod);
    }
}