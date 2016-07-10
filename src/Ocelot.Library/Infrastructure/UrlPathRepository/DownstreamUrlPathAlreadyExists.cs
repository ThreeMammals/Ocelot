using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlPathRepository
{
    public class DownstreamUrlPathTemplateAlreadyExists : Error
    {
        public DownstreamUrlPathTemplateAlreadyExists() 
            : base("This key has already been used")
        {
        }
    }
}