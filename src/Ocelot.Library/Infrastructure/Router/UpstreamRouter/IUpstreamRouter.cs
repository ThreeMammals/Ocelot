using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router.UpstreamRouter
{
    public interface IUpstreamRouter
    {
        Response AddRoute(string downstreamUrl, string upstreamUrl);
        Response<Route> GetRoute(string downstreamUrl);
    }
} 