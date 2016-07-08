using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router.UpstreamRouter
{
    public class RouteKeyDoesNotExist : Error
    {
        public RouteKeyDoesNotExist() 
            : base("This key does not exist")
        {
        }
    }
}