using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Repository
{
    public class CannotAddDataError : Error
    {
        public CannotAddDataError(string message) : base(message)
        {
        }
    }
}
