using Ocelot.ApiGateway.Infrastructure.Responses;

namespace Ocelot.ApiGateway.Infrastructure.Router
{
    public class RouteKeyDoesNotExist : Error
    {
        public RouteKeyDoesNotExist(string message) 
            : base(message)
        {
        }
    }
}