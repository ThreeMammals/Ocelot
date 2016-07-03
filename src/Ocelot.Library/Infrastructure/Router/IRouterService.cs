using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router
{
    public interface IRouterService
    {
        Response AddRoute(string apiKey, string upstreamApiBaseUrl);
        Response<Route> GetRoute(string apiKey);
    }
}