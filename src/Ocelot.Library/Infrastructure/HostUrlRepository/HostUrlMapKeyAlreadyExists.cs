using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.HostUrlRepository
{
    public class HostUrlMapKeyAlreadyExists : Error
    {
        public HostUrlMapKeyAlreadyExists() 
            : base("This key has already been used")
        {
        }
    }
}