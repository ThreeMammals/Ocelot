using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.UrlTemplateRepository
{
    public class DownstreamUrlTemplateDoesNotExist : Error
    {
        public DownstreamUrlTemplateDoesNotExist() 
            : base("This key does not exist")
        {
        }
    }
}