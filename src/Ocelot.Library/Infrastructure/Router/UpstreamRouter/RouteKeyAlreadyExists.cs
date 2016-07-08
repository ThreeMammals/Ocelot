using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router.UpstreamRouter
{
    public class RouteKeyAlreadyExists : Error
    {
        public RouteKeyAlreadyExists() 
            : base("This key has already been used")
        {
        }
    }
}