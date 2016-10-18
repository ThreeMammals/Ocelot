using Ocelot.Errors;

namespace Ocelot.ScopedData
{
    public class CannotAddDataError : Error
    {
        public CannotAddDataError(string message) : base(message, OcelotErrorCode.CannotAddDataError)
        {
        }
    }
}
