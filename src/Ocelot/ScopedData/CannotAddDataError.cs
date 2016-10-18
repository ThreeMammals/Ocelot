using Ocelot.Library.Errors;

namespace Ocelot.Library.ScopedData
{
    public class CannotAddDataError : Error
    {
        public CannotAddDataError(string message) : base(message, OcelotErrorCode.CannotAddDataError)
        {
        }
    }
}
