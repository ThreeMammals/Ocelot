using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlTemplateRepository
{
    public class DownstreamUrlTemplateAlreadyExists : Error
    {
        public DownstreamUrlTemplateAlreadyExists() 
            : base("This key has already been used")
        {
        }
    }
}