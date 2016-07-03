using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router
{
    public class RouteKeyAlreadyExists : Error
    {
        public RouteKeyAlreadyExists(string message) 
            : base(message)
        {
        }
    }
}