using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Services
{
    public class CannotAddDataError : Error
    {
        public CannotAddDataError(string message) : base(message)
        {
        }
    }
}
