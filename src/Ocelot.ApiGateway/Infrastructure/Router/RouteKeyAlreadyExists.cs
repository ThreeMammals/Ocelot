using Ocelot.ApiGateway.Infrastructure.Responses;

namespace Ocelot.ApiGateway.Infrastructure.Router
{
    public class RouteKeyAlreadyExists : Error
    {
        public RouteKeyAlreadyExists(string message) 
            : base(message)
        {
        }
    }
}