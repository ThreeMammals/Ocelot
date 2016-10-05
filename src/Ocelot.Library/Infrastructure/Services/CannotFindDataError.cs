using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Services
{
    public class CannotFindDataError : Error
    {
        public CannotFindDataError(string message) : base(message)
        {
        }
    }
}
