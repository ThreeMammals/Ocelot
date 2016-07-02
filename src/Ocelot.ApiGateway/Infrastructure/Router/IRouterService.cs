using Ocelot.ApiGateway.Infrastructure.Responses;

namespace Ocelot.ApiGateway.Infrastructure.Router
{
    public interface IRouterService
    {
        Response AddRoute(string apiKey, string upstreamApiBaseUrl);
        Response<Route> GetRoute(string apiKey);
    }
}