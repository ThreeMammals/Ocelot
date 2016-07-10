using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.BaseUrlRepository
{
    public class BaseUrlMapKeyAlreadyExists : Error
    {
        public BaseUrlMapKeyAlreadyExists() 
            : base("This key has already been used")
        {
        }
    }
}