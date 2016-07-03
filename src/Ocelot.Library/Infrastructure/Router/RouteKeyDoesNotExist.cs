using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router
{
    public class RouteKeyDoesNotExist : Error
    {
        public RouteKeyDoesNotExist(string message) 
            : base(message)
        {
        }
    }
}