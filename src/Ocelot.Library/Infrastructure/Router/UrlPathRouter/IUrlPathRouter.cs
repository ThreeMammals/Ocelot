using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router.UrlPathRouter
{
    public interface IUrlPathRouter
    {
        Response AddRoute(string downstreamUrlPathTemplate, string upstreamUrlPathTemplate);
        Response<UrlPath> GetRoute(string downstreamUrlPathTemplate);
    }
}  