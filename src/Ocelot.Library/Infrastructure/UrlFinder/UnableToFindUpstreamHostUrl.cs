using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlFinder
{
    public class UnableToFindUpstreamHostUrl : Error
    {
        public UnableToFindUpstreamHostUrl() 
            : base("Unable to find upstream base url")
        {
        }
    }
}