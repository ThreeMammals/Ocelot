using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.BaseUrlRepository
{
    public class BaseUrlMapKeyDoesNotExist : Error
    {
        public BaseUrlMapKeyDoesNotExist() 
            : base("This key does not exist")
        {
        }
    }
}