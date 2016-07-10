using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.HostUrlRepository
{
    public class HostUrlMapKeyDoesNotExist : Error
    {
        public HostUrlMapKeyDoesNotExist() 
            : base("This key does not exist")
        {
        }
    }
}